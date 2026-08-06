using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
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
    public sealed class StageBootstrap : MonoBehaviour
    {
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

        /// <summary>이 스테이지의 정의. 진단·테스트용으로 읽기만 노출한다.</summary>
        public StageConfig StageConfig => _stageConfig;

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

        /// <summary>보상 선택의 로직. 카탈로그나 화면이 씬에 없으면 <c>null</c> 이다.</summary>
        public RewardSelectionPresenter Rewards { get; private set; }

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
        /// 로직을 조립하고 뷰에 물린다. <see cref="Awake"/> 가 자동으로 부르지만,
        /// 인스펙터 없이 세운 경우 테스트가 직접 부를 수 있다.
        /// </summary>
        public void Build()
        {
            Teardown();
            ResolveMissingReferences();

            // 런은 한 번만 만든다. Build() 를 다시 불러도 살아남아야 재시도에 덱이 유지된다.
            Run ??= new RunState(SushiDeck.FromSpawnTable(_stageConfig),
                                 new CustomerDeck(_startingCustomers), NewSeed());

            Belt = new SushiBelt(_stageConfig, Run.Sushi.Cards, new SequenceNumberIssuer(),
                                 new SushiPool<SushiItem>(new SushiItemFactory()));
            Revenue = new RevenueLedger();
            Wallet = new RecruitWallet(_stageConfig.InitialRecruitBudget);
            Coordinator = new ClaimCoordinator(Belt, _stageConfig, Revenue, Wallet);
            Placement = new CustomerPlacementService(Coordinator, _stageConfig,
                                                     new SequenceNumberIssuer(), Wallet);
            Stage = new StageController(Coordinator, Revenue, _stageConfig);
            Stage.OutcomeDecided += OnOutcomeDecided;

            _beltView.Initialize(_viewPool, _stageConfig, _beltStart, _beltEnd);
            _beltView.Bind(Belt);

            BindSlots();

            _placementController.Initialize(_slots, RosterArray());
            _placementController.Bind(Placement, Coordinator);

            if (_hud != null)
            {
                _hud.Bind(Revenue, Wallet, Placement, Coordinator, Stage, _placementController);
            }

            BuildRewards();
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

            var definitions = _stageConfig.TableSlots;
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

            Rewards = new RewardSelectionPresenter(_rewardView, new RewardGenerator(_rewardCatalog));
            _rewardView.Bind(Rewards);
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
        /// 명부를 배열로 옮긴다. 배치 껍데기가 인스펙터 배열을 그대로 쓰던 형태를 유지하되,
        /// 내용은 런에서 온다 — 보상으로 영입한 손님이 다음 판부터 앉힐 수 있게 된다.
        /// </summary>
        private CustomerData[] RosterArray()
        {
            var members = Run.Customers.Members;
            var roster = new CustomerData[members.Count];
            for (var i = 0; i < members.Count; i++)
            {
                roster[i] = members[i];
            }

            return roster;
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
        /// 판정이 났다. <b>클리어에만 보상 화면을 연다</b> — 실패는 재시도 경로다.
        ///
        /// <para>
        /// 다음 스테이지로 넘어가는 것(<c>RunState.AdvanceStage</c>)은 여기서 하지 않는다.
        /// 다음 스테이지의 씬·설정이 M4 이고, 프레젠터의 <c>Closed</c> 가 그때 붙일 자리다.
        /// </para>
        /// </summary>
        private void OnOutcomeDecided(StageOutcome outcome)
        {
            if (outcome == StageOutcome.Cleared)
            {
                Rewards?.Open(Run);
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

            if (_placementController == null)
            {
                _placementController = GetComponentInChildren<CustomerPlacementController>(true);
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
            if (_stageConfig != null)
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
            Stage?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Teardown();
        }

        private void Teardown()
        {
            _beltView?.Unbind();
            if (_hud != null)
            {
                _hud.Unbind();
            }

            if (Stage != null)
            {
                Stage.OutcomeDecided -= OnOutcomeDecided;
                Stage = null;
            }

            Rewards = null;

            Coordinator?.Dispose();
            Coordinator = null;
            Belt = null;
            Placement = null;
            Wallet = null;
            Revenue = null;
        }
    }
}
