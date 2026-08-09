using SushiDefense.Audio;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Effects;
using SushiDefense.Navigation;
using SushiDefense.Run;
using SushiDefense.Scoring;
using SushiDefense.Stages;
using SushiDefense.UI;
using UnityEngine;

namespace SushiDefense
{
    /// <summary>
    /// 씬 진입점. 순수 로직 객체를 조립해 뷰에 물려 주고, 매 프레임 시간을 흘린다.
    ///
    /// <para>
    /// 조립을 한곳에 모으는 이유: 뷰가 스스로 로직을 만들면 씬마다 다른 조합이 생기고,
    /// 어느 뷰가 어떤 로직을 쥐고 있는지 추적할 수 없게 된다.
    /// </para>
    /// <para>
    /// <b>시간을 흘리는 것도 여기다.</b> 뷰가 <c>Tick</c> 을 부르면 "그리는 일" 과
    /// "판정을 돌리는 일" 이 한 컴포넌트에 섞인다 (<c>CLAUDE.md</c> §3.2).
    /// </para>
    /// </summary>
    public sealed class StageBootstrap : MonoBehaviour, IStageRestarter
    {
        /// <summary>
        /// 이 런이 지나갈 스테이지 목록. 비어 있으면 <see cref="_stageConfig"/> 한 장짜리
        /// 런으로 본다 — <b>경로를 하나로 유지</b>하기 위해서다. "진행이 없는 모드" 라는
        /// 두 번째 분기를 만들면 테스트가 두 배가 된다.
        /// </summary>
        [SerializeField] private RunConfig _runConfig;

        /// <summary>
        /// 목록을 물리지 않았을 때 쓸 폴백 스테이지. <b>런타임에 여기 대입하지 않는다</b> —
        /// 직렬화 필드를 런타임에 쓰면 인스펙터에 물린 값이 진실인지 아닌지 알 수 없게 된다.
        /// 지금 도는 스테이지는 <see cref="ActiveStage"/> 다.
        /// </summary>
        [SerializeField] private StageConfig _stageConfig;

        [SerializeField] private SushiPoolBehaviour _viewPool;
        [SerializeField] private SushiBeltView _beltView;
        [SerializeField] private Transform _beltStart;
        [SerializeField] private Transform _beltEnd;
        [SerializeField] private CustomerPlacementController _placementController;
        [SerializeField] private TableSlotView[] _slots;
        [SerializeField] private CustomerData[] _startingCustomers;
        [SerializeField] private StageHudView _hud;
        [SerializeField] private RewardCatalog _rewardCatalog;
        [SerializeField] private RewardSelectionView _rewardView;
        [SerializeField] private StageTransitionView _transitionView;
        [SerializeField] private DeckPanelView _deckPanelView;
        [SerializeField] private StageMenuView _stageMenuView;
        [SerializeField] private SceneRouter _sceneRouter;
        [SerializeField] private AudioDirector _audioDirector;
        [SerializeField] private EffectDirector _effectDirector;
        [SerializeField] private CustomerHandView _hand;
        [SerializeField] private CustomerInspectorView _inspectorView;
        [SerializeField] private CustomerTapRouter _tapRouter;

        /// <summary>
        /// 지금 돌고 있는 스테이지의 정의. 스테이지가 넘어가면 이 값이 바뀐다.
        /// 진단·테스트용으로 읽기만 노출한다.
        /// </summary>
        public StageConfig ActiveStage { get; private set; }

        /// <summary>지금 돌고 있는 스테이지의 정의. <see cref="ActiveStage"/> 의 옛 이름이다.</summary>
        public StageConfig StageConfig => ActiveStage;

        /// <summary>이 스테이지의 진행 표시. 씬에 없으면 <c>null</c> 이다.</summary>
        public StageHudView Hud => _hud;

        /// <summary>이 스테이지의 벨트. 진단·테스트용으로 노출한다.</summary>
        public SushiBelt Belt { get; private set; }

        /// <summary>이 스테이지의 조율자.</summary>
        public ClaimCoordinator Coordinator { get; private set; }

        /// <summary>이 스테이지의 배치 서비스.</summary>
        public CustomerPlacementService Placement { get; private set; }

        /// <summary>이 스테이지의 영입 재화. 스테이지마다 초기 예산으로 새로 열린다.</summary>
        public RecruitWallet Wallet { get; private set; }

        /// <summary>이 스테이지의 매출. 스테이지마다 0 에서 시작한다.</summary>
        public RevenueLedger Revenue { get; private set; }

        /// <summary>
        /// 이 런의 기억 — 덱 · 손님 명부 · 진행 스테이지.
        ///
        /// <para>
        /// <b><see cref="Build"/> 바깥에 산다.</b> 원장·지갑·벨트·순차번호 발급기는 호출마다
        /// 새로 열리지만 이 객체는 살아남는다. 재시도에 새 코드가 거의 없는 이유다.
        /// </para>
        /// </summary>
        public RunState Run { get; private set; }

        /// <summary>이 스테이지의 진행. 시간을 흘리고 클리어/실패를 판정한다.</summary>
        public StageController Stage { get; private set; }

        /// <summary>
        /// 이 런의 진행 — 지금 몇 번째인가 · 다음이 있는가 · 끝났는가.
        /// <see cref="Run"/> 과 같은 수명이다.
        /// </summary>
        public RunProgression Progression { get; private set; }

        /// <summary>
        /// 보상 선택의 로직. 카탈로그나 화면이 씬에 없으면 <c>null</c> 이다.
        ///
        /// <para>
        /// <b>런 수명이다.</b> <see cref="Build"/> 마다 새로 만들면, 전환 화면의 확인 입력이
        /// 부른 <see cref="Build"/> 가 자기를 부른 프레젠터를 파괴하게 된다.
        /// </para>
        /// </summary>
        public RewardSelectionPresenter Rewards { get; private set; }

        /// <summary>전환 화면의 로직. 화면이 씬에 없으면 <c>null</c> 이다. 런 수명이다.</summary>
        public StageTransitionPresenter Transition { get; private set; }

        /// <summary>
        /// 덱 보기의 로직. 화면이 씬에 없으면 <c>null</c> 이다.
        ///
        /// <para>
        /// <b>런 수명이다.</b> 덱은 보상으로 자라고 스테이지를 넘어 유지되므로, 판마다
        /// 새로 만들면 열어 둔 화면이 조용히 옛 덱을 가리키게 된다.
        /// </para>
        /// </summary>
        public DeckPanelPresenter Deck { get; private set; }

        /// <summary>
        /// 인스테이지 메뉴의 로직. 화면이 씬에 없으면 <c>null</c> 이다. 런 수명이다.
        /// </summary>
        public StageMenuPresenter Menu { get; private set; }

        /// <summary>
        /// 앉아 있는 손님을 눌러 보는 정보 창의 로직. 화면이 씬에 없으면 <c>null</c> 이다.
        /// 런 수명이며, <b>판을 멈추지 않는다</b>.
        /// </summary>
        public CustomerInspectorPresenter Inspector { get; private set; }

        /// <summary>
        /// 이 판이 멈춰 있나. <b>시간을 흘릴지 말지의 유일한 진실</b>이다.
        ///
        /// <para>
        /// <b>런 수명이다.</b> 판마다 새로 만들면 메뉴가 들고 있는 게이트가 죽은 객체를
        /// 가리킨다. 다만 <b>새 판은 흐르는 상태로 시작</b>해야 하므로 <see cref="Build"/> 가
        /// 재개시킨다 — 멈춘 채로 열리면 고장으로 보인다.
        /// </para>
        /// </summary>
        public PauseState Pause { get; private set; }

        /// <summary>
        /// 스테이지 위의 창이 겹쳐 뜨지 않게 하는 조정자. <b>런 수명이다</b> — 창들 자신이
        /// 런 수명이므로 조정자만 판마다 새로 만들면 열려 있던 창을 잊는다.
        /// </summary>
        public StageWindowArbiter Windows { get; private set; }

        /// <summary>인스펙터 없이 참조를 물린다. 테스트용 진입점이다.</summary>
        public void Initialize(StageConfig stageConfig, SushiPoolBehaviour viewPool,
                               SushiBeltView beltView, Transform beltStart, Transform beltEnd,
                               CustomerPlacementController placementController,
                               TableSlotView[] slots, CustomerData[] startingCustomers)
        {
            _stageConfig = stageConfig;
            _viewPool = viewPool;
            _beltView = beltView;
            _beltStart = beltStart;
            _beltEnd = beltEnd;
            _placementController = placementController;
            _slots = slots;
            _startingCustomers = startingCustomers;
        }

        /// <summary>
        /// 인스펙터 없이 <b>런 수명</b> 참조를 물린다. 테스트용 진입점이며, 스테이지 수명
        /// 참조를 받는 <see cref="Initialize"/> 와 나눠 둔 이유는 두 수명이 다르기 때문이다.
        /// <c>null</c> 을 넘기면 그 기능만 꺼진다.
        /// </summary>
        public void InitializeRun(RunConfig runConfig, RewardCatalog rewardCatalog,
                                  RewardSelectionView rewardView,
                                  StageTransitionView transitionView)
        {
            _runConfig = runConfig;
            _rewardCatalog = rewardCatalog;
            _rewardView = rewardView;
            _transitionView = transitionView;
        }

        /// <summary>
        /// 로직을 조립하고 뷰에 물린다. <see cref="Awake"/> 가 자동으로 부르지만,
        /// 인스펙터 없이 세운 경우 테스트가 직접 부를 수 있다.
        /// </summary>
        public void Build()
        {
            Teardown();
            ResolveMissingReferences();
            EnsureRunScope();

            // 새 판은 흐르는 상태로 열린다. 게이트 자체는 런 수명이라 살아남지만, 멈춘
            // 채로 다음 판이 시작되면 고장으로 보인다.
            Pause.Resume();

            // 런이 끝난 뒤에도 마지막 판을 그대로 두려면 폴백이 필요하다.
            ActiveStage = Progression.CurrentStage ?? ActiveStage ?? _stageConfig;

            Belt = new SushiBelt(ActiveStage, Run.Sushi.Cards, new SequenceNumberIssuer(),
                                 new SushiPool<SushiItem>(new SushiItemFactory()));
            Revenue = new RevenueLedger();
            Wallet = new RecruitWallet(ActiveStage.InitialRecruitBudget);
            Coordinator = new ClaimCoordinator(Belt, ActiveStage, Revenue, Wallet);
            Placement = new CustomerPlacementService(Coordinator, ActiveStage,
                                                     new SequenceNumberIssuer(), Wallet, Pause);
            Stage = new StageController(Coordinator, Revenue, ActiveStage);
            Stage.OutcomeDecided += OnOutcomeDecided;

            _beltView.Initialize(_viewPool, ActiveStage, _beltStart, _beltEnd);
            _beltView.Bind(Belt);

            BindSlots();

            _placementController.Bind(Placement, Coordinator);

            if (_hud != null)
            {
                _hud.Bind(Revenue, Wallet, Placement, Coordinator, Stage, Progression, Pause);
            }

            // 디렉터 자체는 런 수명이다. 판이 바뀔 때마다 새 출처만 갈아 낀다 —
            // 새로 만들면 배경음이 끊기고 자동재생 잠금이 다시 걸린다.
            if (_audioDirector != null)
            {
                _audioDirector.Bind(Coordinator, Placement, Stage, Rewards, Transition);
            }

            if (_effectDirector != null)
            {
                _effectDirector.Bind(Coordinator, Stage, _slots);
            }

            // 손패는 명부를 카드로 늘어놓고, 카드를 자리에 떨어뜨리면 손님이 앉는다.
            // 카메라를 넘기지 않는 것은 씬 진입점이 들고 있지 않기 때문이며, 손패가
            // null 을 그대로 대입하지 않는다는 것이 그쪽의 계약이다.
            if (_hand != null)
            {
                _hand.Initialize(_placementController, null, _slots);

                // 지갑을 물리는 것이 명부보다 먼저다. Bind 가 끝나며 카드를 한 번 평가하고,
                // 그 뒤로는 잔액이 바뀔 때마다 지갑이 다시 평가시킨다.
                _hand.Watch(Wallet);
                _hand.Bind(Run.Customers.Members);
            }
        }

        /// <summary>
        /// 런에 딸린 것들을 <b>한 번만</b> 세운다 — 덱 · 명부 · 진행 · 보상 화면 · 전환 화면.
        ///
        /// <para>
        /// 이 다섯은 스테이지가 아니라 런의 것이다. 특히 두 프레젠터를 여기 둔 이유는
        /// 재진입 때문이다: 전환 화면의 확인 입력이 <see cref="Build"/> 를 부르는데,
        /// 그 <see cref="Build"/> 가 자기를 부른 프레젠터를 파괴하면 이벤트 발행 도중에
        /// 발밑이 무너진다.
        /// </para>
        /// <para>
        /// 덱은 <b>첫 스테이지</b>의 스폰 구성에서만 온다. 이후 스테이지의
        /// <c>SpawnTable</c> 은 읽히지 않는다 — 덱의 진실은 런에 있다 (M3).
        /// </para>
        /// </summary>
        private void EnsureRunScope()
        {
            if (Run != null)
            {
                return;
            }

            var stages = StageSequence();
            Run = new RunState(SushiDeck.FromSpawnTable(stages[0]),
                               new CustomerDeck(_startingCustomers), NewSeed());
            Progression = new RunProgression(stages, Run);
            Pause = new PauseState();

            Windows = new StageWindowArbiter();
            Windows.CloseRequested += OnWindowCloseRequested;
            Windows.CoverageChanged += OnWindowCoverageChanged;

            BuildRewards();
            BuildTransition();
            BuildDeckPanel();
            BuildStageMenu();
            BuildInspector();
        }

        /// <summary>
        /// 손님 정보 창을 세운다. 화면이 없으면 조용히 건너뛴다 — 덱·메뉴와 같은 판단이며,
        /// 정보 조회는 판이 돌아가는 데 필요한 것이 아니라 그 위에 얹히는 것이다.
        ///
        /// <para>
        /// <b>클릭 라우터는 자리 목록만 받는다.</b> 배치 서비스를 거치지 않는 이유는 자리가
        /// 이미 앉은 손님의 뷰를 들고 있기 때문이고, 둘 다 물으면 «누가 앉아 있나» 의 답이
        /// 두 곳에서 나온다.
        /// </para>
        /// </summary>
        private void BuildInspector()
        {
            if (_inspectorView == null)
            {
                return;
            }

            Inspector = new CustomerInspectorPresenter(_inspectorView, Windows);

            if (_tapRouter != null)
            {
                // 카메라를 넘기지 않는 것은 씬 진입점이 들고 있지 않기 때문이며,
                // 라우터가 null 을 그대로 대입하지 않는다는 것이 그쪽의 계약이다.
                _tapRouter.Bind(_slots, null, OpenInspectorAt, Inspector.Close);
            }
        }

        /// <summary>
        /// 창을 <b>먼저 세우고</b> 띄운다. 순서를 바꾸면 한 프레임 동안 직전 손님 자리에
        /// 창이 보인다.
        ///
        /// <para>
        /// 람다가 아니라 메서드 그룹으로 넘긴다 — 나중에 구독을 풀 일이 생겨도 같은 대상을
        /// 가리킬 수 있고, 매번 새 델리게이트를 만들지 않는다.
        /// </para>
        /// </summary>
        private void OpenInspectorAt(CustomerLogic customer, Vector2 worldPoint)
        {
            _inspectorView.AnchorTo(worldPoint);
            Inspector.Open(customer);
        }

        /// <summary>
        /// 이 런이 지나갈 스테이지 목록. 목록이 없으면 <b>한 장짜리 런</b>이 되며, 그것이
        /// 곧 M3 까지의 동작이다 — 첫 클리어가 곧 런 종료다.
        /// </summary>
        private StageConfig[] StageSequence()
        {
            if (_runConfig == null || _runConfig.StageCount == 0)
            {
                return new[] { _stageConfig };
            }

            var stages = new StageConfig[_runConfig.StageCount];
            for (var i = 0; i < stages.Length; i++)
            {
                stages[i] = _runConfig.Stages[i];
            }

            return stages;
        }

        /// <summary>
        /// 씬의 자리를 이 스테이지의 정의에 맞춘다. 정의보다 자리가 많으면 남는 자리를 끈다.
        ///
        /// <para>
        /// <b><see cref="Build"/> 안에 있어야 한다.</b> 자리 수는 스테이지마다 다르고 스테이지
        /// 교체는 <see cref="Build"/> 재호출이므로, <see cref="Awake"/> 에 두면 두 번째
        /// 스테이지에서 자리가 그대로 남는다.
        /// </para>
        /// <para>
        /// <b>정의가 비어 있으면 아무것도 하지 않는다.</b> 씬에 박힌 값을 그대로 쓴다는 뜻이며,
        /// 자리 정의 없이 세우는 테스트 하네스가 살아 있는 이유다. 여기서 전부 꺼 버리면
        /// 그런 구성이 통째로 죽는다.
        /// </para>
        /// <para>
        /// <b>정의가 자리보다 많아도 예외를 내지 않는다.</b> 씬을 조금씩 조립하는 동안 흔한
        /// 상태이고, 여기서 터지면 나머지를 아무것도 확인할 수 없다. 자리를 새로 만들지도
        /// 않는다 — 프로덕션에서 오브젝트를 만드는 지점은 풀 하나로 유지한다 (§3.4).
        /// </para>
        /// </summary>
        private void BindSlots()
        {
            if (_slots == null)
            {
                return;
            }

            // 자리를 먼저 전부 비운다. 배치 서비스는 판마다 새로 열려 «아무도 안 앉은»
            // 상태로 시작하는데, 자리는 씬 오브젝트라 직전 판의 손님을 그대로 들고 있다 —
            // 다시 시작한 판의 테이블에 이전 손님이 남아 있던 것이 이 때문이다.
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    _slots[i].Vacate();
                }
            }

            var definitions = ActiveStage.TableSlots;
            if (definitions.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot == null)
                {
                    continue;
                }

                var used = i < definitions.Count;
                if (used)
                {
                    slot.Bind(definitions[i]);
                }

                slot.gameObject.SetActive(used);
            }
        }

        /// <summary>
        /// 보상 화면을 세운다. 카탈로그나 화면이 없으면 조용히 건너뛴다 — 보상은 스테이지가
        /// 돌아가는 데 필요한 것이 아니라 클리어 뒤에 붙는 것이라, 없다고 판이 서지 못하면
        /// 씬을 조금씩 조립하는 동안 아무것도 못 돌린다.
        /// </summary>
        private void BuildRewards()
        {
            if (_rewardCatalog == null || _rewardView == null)
            {
                return;
            }

            Rewards = new RewardSelectionPresenter(_rewardView, new RewardGenerator(_rewardCatalog),
                                                   Windows);
            Rewards.Closed += OnRewardsClosed;
            _rewardView.Bind(Rewards);
        }

        /// <summary>
        /// 전환 화면을 세운다. 화면이 없으면 조용히 건너뛰고, 그러면 흐름이 보상 화면에서
        /// 멈춘다 — 보상과 같은 판단이다.
        ///
        /// <para>
        /// <c>RunCompleted</c> 를 구독하지 않는다. 런이 끝났다는 표시는 화면이 이미 했고,
        /// 여기서 할 일이 없다 — <b>빈 핸들러를 두면 다음 사람이 그게 필요한 자리인 줄
        /// 안다.</b> 메인화면으로 돌아가는 것은 M6 이고, 그때 이 이벤트가 붙을 자리다.
        /// </para>
        /// </summary>
        private void BuildTransition()
        {
            if (_transitionView == null)
            {
                return;
            }

            Transition = new StageTransitionPresenter(_transitionView, Progression);
            Transition.StageAdvanced += OnStageAdvanced;
            _transitionView.Bind(Transition);
        }

        /// <summary>
        /// 덱 보기를 세운다. 화면이 없으면 조용히 건너뛴다 — 보상·전환과 같은 판단이며,
        /// 덱 보기는 판이 돌아가는 데 필요한 것이 아니라 그 위에 얹히는 것이다.
        /// </summary>
        private void BuildDeckPanel()
        {
            if (_deckPanelView == null)
            {
                return;
            }

            Deck = new DeckPanelPresenter(_deckPanelView, Run, Windows);
            _deckPanelView.Bind(Deck);
        }

        /// <summary>
        /// 인스테이지 메뉴를 세운다. 화면이나 라우터가 없으면 조용히 건너뛴다 — 덱·보상과
        /// 같은 판단이며, 메뉴는 판이 돌아가는 데 필요한 것이 아니다.
        /// </summary>
        private void BuildStageMenu()
        {
            if (_stageMenuView == null || _sceneRouter == null)
            {
                return;
            }

            Menu = new StageMenuPresenter(_stageMenuView, Pause, this, _sceneRouter, Windows);
            _stageMenuView.Bind(Menu);
        }

        /// <summary>
        /// 조정자가 밀어낸 창을 실제로 내린다. <b>조정자는 뷰를 모른다</b> — 창마다 닫기
        /// 절차가 다르고(저장·이벤트 발행), 그것을 조정자가 알면 창이 늘 때마다 거기를 고친다.
        ///
        /// <para>
        /// 보상은 여기에 없다. 보상은 밀려나는 창이 아니라 <b>밑에 깔리는 창</b>이고,
        /// 닫히는 순간이 곧 다음 판이라 임의로 내릴 수 없다.
        /// </para>
        /// </summary>
        private void OnWindowCloseRequested(StageWindow window)
        {
            switch (window)
            {
                case StageWindow.Menu:
                    Menu?.Close();
                    break;
                case StageWindow.Deck:
                    Deck?.Close();
                    break;
                case StageWindow.CustomerInfo:
                    Inspector?.Close();
                    break;
            }
        }

        /// <summary>
        /// 가려진 창의 조작을 막는다. <b>보이지만 눌리지 않는 상태</b>로 둔다.
        ///
        /// <para>
        /// 숨기지 않는 이유: 보상 창은 밑에 깔린 채로 <b>무엇을 고르는 중이었는지</b> 보여야
        /// 하고, 위의 창이 닫히면 그대로 다시 조작할 수 있어야 한다.
        /// </para>
        /// <para>
        /// <c>blocksRaycasts</c> 는 <b>건드리지 않는다.</b> 끄면 클릭이 창을 통과해 뒤의
        /// 판으로 떨어진다 — 가려진 창 위를 눌렀을 때 손님이 앉으면 더 나쁘다.
        /// </para>
        /// </summary>
        private void OnWindowCoverageChanged(StageWindow window, bool covered)
        {
            var view = window switch
            {
                StageWindow.Menu => (Component)_stageMenuView,
                StageWindow.Deck => _deckPanelView,
                StageWindow.Reward => _rewardView,
                StageWindow.CustomerInfo => _inspectorView,
                _ => null
            };

            if (view == null)
            {
                return;
            }

            // 씬에 미리 놓아 두지 않아도 되게 여기서 챙긴다. 요구했다가 빠지면 «가렸는데
            // 그대로 눌린다» 가 조용히 돌아온다.
            if (!view.TryGetComponent<CanvasGroup>(out var group))
            {
                group = view.gameObject.AddComponent<CanvasGroup>();
            }

            group.interactable = !covered;
        }

        /// <summary>
        /// 같은 판을 다시 연다. <c>Retry</c> 와 같은 것이며, 이름이 둘이 되지 않도록
        /// 명시적 구현으로 넘긴다 — 재시작 경로가 하나여야 덱·명부 유지 규칙도 한 곳에 남는다.
        /// </summary>
        void IStageRestarter.Restart()
        {
            Retry();
        }

        /// <summary>
        /// 보상 화면이 닫혔다. <b>다음 판으로 바로 넘어간다.</b>
        ///
        /// <para>
        /// 확인 입력(Enter)을 한 번 더 받던 자리다. 보상을 고르는 것 자체가 이미 «다음으로
        /// 가겠다» 는 입력이라, 한 박자를 더 두면 <b>고르고 나서 아무 일도 일어나지 않는</b>
        /// 화면이 된다 — 안내 문구가 키를 알려 주어도 그 화면을 처음 보는 사람에게는 멈춘
        /// 것으로 읽힌다.
        /// </para>
        /// <para>
        /// <b>런 완료만 화면으로 남긴다.</b> 3판을 다 깼다는 것을 알릴 곳이 여기뿐이고,
        /// 그때는 넘어갈 다음 판도 없어 «바로 넘어간다» 가 성립하지 않는다.
        /// </para>
        /// </summary>
        private void OnRewardsClosed()
        {
            if (Transition == null)
            {
                return;
            }

            Transition.Open();

            if (!Transition.IsRunFinale)
            {
                Transition.Proceed();
            }
        }

        /// <summary>
        /// 다음 스테이지가 정해졌다. <see cref="Build"/> 가 <see cref="Progression"/> 에서
        /// 다시 읽으므로 인자를 쓰지 않는다 — 여기서 <see cref="ActiveStage"/> 를 직접
        /// 대입하면 진실이 두 곳이 된다.
        /// </summary>
        private void OnStageAdvanced(StageConfig next)
        {
            Build();
        }

        /// <summary>
        /// 같은 스테이지를 다시 시작한다. <b>덱·명부는 유지되고</b> 원장·지갑·벨트·순차번호는
        /// 새로 열린다 (착수 시 확정 — 실패해도 런은 끝나지 않는다).
        ///
        /// <para>
        /// <see cref="Build"/> 가 이미 <c>Teardown</c> 으로 시작하므로 정리 코드를 새로 쓰지
        /// 않는다.
        /// </para>
        /// </summary>
        public void Retry()
        {
            Run?.RecordFailedAttempt();
            Build();
        }

        /// <summary>
        /// 런마다 달라지는 유일한 지점. 테스트에서 시드를 고정하고 싶어지면 갈아 끼울 자리다.
        /// <c>Presentation</c> 이라 전역 난수를 써도 되지만, <b>여기 한 곳뿐</b>이어야 한다.
        /// </summary>
        private static int NewSeed()
        {
            return UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// 판정이 났다. <b>클리어는 보상 화면, 실패는 메뉴</b>로 간다.
        ///
        /// <para>
        /// 실패했을 때 아무 화면도 뜨지 않으면 플레이어가 메뉴 아이콘을 스스로 찾아야
        /// 하는데, 그 시점의 화면은 <b>멈춘 판과 구분되지 않는다.</b> 필요한 것은 다시
        /// 시작과 나가기 둘이고 그 둘은 이미 메뉴에 있으므로, 전용 화면을 새로 만들지 않고
        /// 재개 버튼만 빠진 형태로 연다.
        /// </para>
        /// </summary>
        private void OnOutcomeDecided(StageOutcome outcome)
        {
            // 판정이 나면 멈춘다. 시계는 컨트롤러가 이미 세우지만, 멈춤은 «시간» 이 아니라
            // «판을 조작할 수 있는지» 를 뜻하기도 한다 — 이 줄이 없으면 보상 화면 위에서
            // 손님을 앉혀 끝난 판에 영입 재화가 나간다. 다음 판은 Build 가 다시 흐르게 한다.
            Pause?.Pause();

            // 판이 끝나면 정보 창을 내린다. 보상은 «밑에 깔리는» 창이라 조정자가 아무것도
            // 밀어내지 않고, 정보 창은 아이콘으로 토글되지도 않아 스스로 닫힐 길이 없다 —
            // 그대로 두면 보상 화면 위에 남고 다음 판까지 따라간다.
            Inspector?.Close();

            if (outcome == StageOutcome.Cleared)
            {
                Rewards?.Open(Run);
                return;
            }

            if (outcome == StageOutcome.Failed)
            {
                Menu?.OpenAfterFailure();
            }
        }

        /// <summary>
        /// 인스펙터에서 비어 있는 참조를 <b>자기 하위 계층에서만</b> 찾아 채운다.
        ///
        /// <para>
        /// <c>FindObjectOfType</c> 같은 씬 전역 탐색이 아니다 (§4.3 금지 대상). 이 스테이지
        /// 루트가 소유한 자식만 본다. 씬을 스크립트로 조립할 때 오브젝트 참조를 일일이
        /// 물리지 않아도 되게 하는 장치이며, <b>인스펙터에 물린 값이 있으면 그대로 둔다.</b>
        /// </para>
        /// </summary>
        private void ResolveMissingReferences()
        {
            if (_viewPool == null)
            {
                _viewPool = GetComponentInChildren<SushiPoolBehaviour>(true);
            }

            if (_beltView == null)
            {
                _beltView = GetComponentInChildren<SushiBeltView>(true);
            }

            if (_hud == null)
            {
                _hud = GetComponentInChildren<StageHudView>(true);
            }

            if (_rewardView == null)
            {
                _rewardView = GetComponentInChildren<RewardSelectionView>(true);
            }

            if (_transitionView == null)
            {
                _transitionView = GetComponentInChildren<StageTransitionView>(true);
            }

            if (_deckPanelView == null)
            {
                _deckPanelView = GetComponentInChildren<DeckPanelView>(true);
            }

            if (_stageMenuView == null)
            {
                _stageMenuView = GetComponentInChildren<StageMenuView>(true);
            }

            if (_sceneRouter == null)
            {
                _sceneRouter = GetComponentInChildren<SceneRouter>(true);
            }

            if (_placementController == null)
            {
                _placementController = GetComponentInChildren<CustomerPlacementController>(true);
            }

            if (_audioDirector == null)
            {
                _audioDirector = GetComponentInChildren<AudioDirector>(true);
            }

            if (_effectDirector == null)
            {
                _effectDirector = GetComponentInChildren<EffectDirector>(true);
            }

            if (_hand == null)
            {
                _hand = GetComponentInChildren<CustomerHandView>(true);
            }

            if (_inspectorView == null)
            {
                _inspectorView = GetComponentInChildren<CustomerInspectorView>(true);
            }

            if (_tapRouter == null)
            {
                _tapRouter = GetComponentInChildren<CustomerTapRouter>(true);
            }

            if (_slots == null || _slots.Length == 0)
            {
                _slots = GetComponentsInChildren<TableSlotView>(true);
            }

            if (_beltStart == null)
            {
                _beltStart = transform.Find(BeltStartName);
            }

            if (_beltEnd == null)
            {
                _beltEnd = transform.Find(BeltEndName);
            }
        }

        /// <summary>벨트 시작점 마커의 자식 이름. 씬 조립 스크립트와 공유하는 약속이다.</summary>
        private const string BeltStartName = "BeltStart";

        /// <summary>벨트 끝점 마커의 자식 이름.</summary>
        private const string BeltEndName = "BeltEnd";

        private void Awake()
        {
            if (_stageConfig != null || (_runConfig != null && _runConfig.StageCount > 0))
            {
                Build();
            }
        }

        /// <summary>
        /// 조율자를 <b>직접 틱하지 않는다.</b> 컨트롤러를 우회하면 결과가 난 뒤에도 벨트가
        /// 계속 흘러 정지 계약이 무너진다.
        /// </summary>
        private void Update()
        {
            // 정보 창은 멈춤 게이트 **앞**이다. 멈춘 동안에도 열려 있을 수 있고, 그때 값이
            // 안 변하는 것은 판이 안 도니 당연하다 — 게이트 뒤에 두면 «멈추면 정보 창이
            // 굳는다» 가 아니라 «멈추기 직전 값에서 영영 멈춘다» 가 된다.
            Inspector?.Tick();

            // 멈춤은 이 한 줄을 건너뛰는 것이다. 벨트·손님·시계·판정이 전부 여기를 지나므로
            // 멈춤을 위해 새 경로를 만들 필요가 없다 (README D4).
            if (Pause != null && Pause.IsPaused)
            {
                return;
            }

            Stage?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Teardown();
            ReleaseRunScope();
        }

        /// <summary>
        /// 런 수명 객체의 구독을 끊는다. <see cref="Teardown"/> 이 아니라 <see cref="OnDestroy"/>
        /// 에서만 부른다 — 스테이지가 넘어갈 때 끊으면 다음 판에서 보상 화면이 닫혀도
        /// 아무 일도 일어나지 않는다 (<c>.claude/rules/scripts.md</c> §6).
        /// </summary>
        private void ReleaseRunScope()
        {
            if (Rewards != null)
            {
                Rewards.Closed -= OnRewardsClosed;
                Rewards = null;
            }

            if (Transition != null)
            {
                Transition.StageAdvanced -= OnStageAdvanced;
                Transition = null;
            }

            Deck = null;
            Menu = null;
            Inspector = null;
            Pause = null;

            if (Windows != null)
            {
                Windows.CloseRequested -= OnWindowCloseRequested;
                Windows.CoverageChanged -= OnWindowCoverageChanged;
                Windows = null;
            }

            if (_audioDirector != null)
            {
                _audioDirector.Unbind();
            }

            if (_effectDirector != null)
            {
                _effectDirector.Unbind();
            }
        }

        /// <summary>
        /// 스테이지 수명 객체만 정리한다. <b>런 수명(<see cref="Run"/> · <see cref="Progression"/>
        /// · <see cref="Rewards"/> · <see cref="Transition"/>)은 건드리지 않는다.</b>
        /// </summary>
        private void Teardown()
        {
            // 정보 창이 붙들고 있는 손님은 이 판의 것이다. 안 내리면 다음 판에 죽은 손님의
            // 값이 그대로 떠 있고, 그 손님은 이미 조율자에서 빠져 갱신도 멈춰 있다.
            Inspector?.Close();

            _beltView?.Unbind();
            if (_hud != null)
            {
                _hud.Unbind();
            }

            // 지갑은 판마다 새로 열린다. 끊지 않으면 손패가 죽은 지갑을 계속 듣는다.
            if (_hand != null)
            {
                _hand.Unwatch();
            }

            if (Stage != null)
            {
                Stage.OutcomeDecided -= OnOutcomeDecided;
                Stage = null;
            }

            Coordinator?.Dispose();
            Coordinator = null;
            Belt = null;
            Placement = null;
            Wallet = null;
            Revenue = null;
        }
    }
}
