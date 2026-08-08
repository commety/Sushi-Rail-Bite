using SushiDefense.Customers;
using SushiDefense.Run;
using SushiDefense.Scoring;
using SushiDefense.Stages;
using TMPro;
using UnityEngine;

namespace SushiDefense.UI
{
    /// <summary>
    /// 스테이지 진행 표시 — 매출 · 영입 재화 · 배치 수.
    ///
    /// <para>
    /// <b>계산하지 않는다.</b> 원장·지갑·배치 서비스가 이미 정한 값을 문자열로 옮기기만 한다
    /// (<c>CLAUDE.md</c> §3.2). 잔액이 모자라 배치가 거부되는 판단도 여기 없다 —
    /// <c>CustomerPlacementService</c> 의 몫이다.
    /// </para>
    /// <para>
    /// 렌더링에 <see cref="TMPro.TMP_Text"/> 를 쓰고 Canvas 위에 산다. 월드 스페이스
    /// 라벨은 브라우저 창 크기가 바뀌면 잘리거나 화면 밖으로 나가는데, 배포 타깃이 웹이라
    /// 창 크기는 사용자가 언제든 바꾸는 값이다.
    ///
    /// <para>
    /// <b>스테이지 안에서 뜨는 화면은 M5 의 몫이다.</b> 메인화면·덱빌딩·설정·백과사전이
    /// M6 이며, 이 뷰는 그때 교체되는 placeholder 가 아니다.
    /// </para>
    /// </para>
    /// </summary>
    public sealed class StageHudView : MonoBehaviour
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string RevenueLabelName = "RevenueLabel";

        private const string WalletLabelName = "WalletLabel";
        private const string PlacementLabelName = "PlacementLabel";
        private const string WaitingLabelName = "WaitingLabel";
        private const string TimeLabelName = "TimeLabel";
        private const string OutcomeLabelName = "OutcomeLabel";
        private const string StageLabelName = "StageLabel";

        /// <summary>멈춰 있을 때의 배너 문구.</summary>
        private const string PausedText = "일시정지";

        [SerializeField] private TMP_Text _revenueLabel;
        [SerializeField] private TMP_Text _walletLabel;
        [SerializeField] private TMP_Text _placementLabel;
        [SerializeField] private TMP_Text _waitingLabel;
        [SerializeField] private TMP_Text _timeLabel;
        [SerializeField] private TMP_Text _outcomeLabel;
        [SerializeField] private TMP_Text _stageLabel;

        private RevenueLedger _revenue;
        private RecruitWallet _wallet;
        private CustomerPlacementService _placement;
        private ClaimCoordinator _coordinator;
        private StageController _stage;
        private RunProgression _progression;
        private PauseState _pause;

        private int _shownPlacedCount = -1;
        private int _shownWaitingCount = -1;
        private int _shownSeconds = -1;
        private int _shownStageNumber = -1;
        private StageOutcome _shownOutcome = StageOutcome.InProgress;
        private bool _shownPaused;

        /// <summary>지금 표시 중인 매출 문구. 검증용이다.</summary>
        public string RevenueText { get; private set; }

        /// <summary>지금 표시 중인 잔액 문구.</summary>
        public string WalletText { get; private set; }

        /// <summary>지금 표시 중인 배치 수 문구.</summary>
        public string PlacementText { get; private set; }

        /// <summary>
        /// 지금 표시 중인 대기 인원 문구.
        ///
        /// <para>
        /// 손님별 대역·대기는 <c>CustomerView</c> 가 자리 옆에 그린다. 여기 있는 것은
        /// <b>전역 요약</b>이다 — "왜 아무도 안 먹지" 에 답하는 한 줄이며, 대기가 정상
        /// 동작임을 플레이어가 알 수 있게 한다.
        /// </para>
        /// </summary>
        public string WaitingText { get; private set; }

        /// <summary>지금 표시 중인 남은 시간 문구.</summary>
        public string TimeText { get; private set; }

        /// <summary>
        /// 지금 표시 중인 배너 문구 — 클리어 · 실패 · 일시정지. 아무것도 아니면 빈 문자열이다.
        ///
        /// <para>
        /// 셋을 한 라벨이 진다. 화면 최상단 가운데 한 자리를 셋이 나눠 쓰는 것이라
        /// 라벨을 셋으로 두면 위치를 세 번 맞춰야 하고, <b>동시에 뜨면 겹친다.</b>
        /// </para>
        /// </summary>
        public string OutcomeText { get; private set; } = string.Empty;

        /// <summary>지금 표시 중인 스테이지 단계 문구.</summary>
        public string StageText { get; private set; } = string.Empty;

        /// <summary>
        /// 표시 대상을 물린다. 이미 물려 있으면 먼저 끊는다 — <c>Build()</c> 를 두 번 부르면
        /// 구독이 겹쳐 한 번의 변화가 두 번 반영된다.
        /// </summary>
        /// <param name="progression">
        /// 몇 판째인지의 출처. <c>null</c> 이면 단계 표시만 빠진다 — 씬을 조금씩 조립하는
        /// 동안 흔한 상태다.
        /// </param>
        /// <param name="pause">멈춤 배너의 출처. <c>null</c> 이면 배너에 클리어·실패만 뜬다.</param>
        public void Bind(RevenueLedger revenue, RecruitWallet wallet,
                         CustomerPlacementService placement, ClaimCoordinator coordinator,
                         StageController stage, RunProgression progression, PauseState pause)
        {
            Unbind();

            _revenue = revenue;
            _wallet = wallet;
            _placement = placement;
            _coordinator = coordinator;
            _stage = stage;
            _progression = progression;
            _pause = pause;

            if (_revenue != null)
            {
                _revenue.TotalChanged += OnRevenueChanged;
                OnRevenueChanged(_revenue.Total);
            }

            if (_wallet != null)
            {
                _wallet.BalanceChanged += OnBalanceChanged;
                OnBalanceChanged(_wallet.Balance);
            }

            _shownPlacedCount = -1;
            _shownWaitingCount = -1;
            _shownSeconds = -1;
            _shownStageNumber = -1;
            _shownOutcome = StageOutcome.InProgress;
            _shownPaused = false;
            OutcomeText = string.Empty;
            HudLabel.Write(_outcomeLabel, OutcomeText);

            RefreshPlacement();
            RefreshWaiting();
            RefreshTime();
            RefreshStageNumber();
        }

        /// <summary>
        /// 구독을 끊는다. 원장·지갑은 <see cref="MonoBehaviour"/> 가 아니라 씬이 내려가도
        /// 살아 있을 수 있으므로, 떼지 않으면 파괴된 뷰를 계속 부른다
        /// (<c>.claude/rules/scripts.md</c> §6).
        /// </summary>
        public void Unbind()
        {
            if (_revenue != null)
            {
                _revenue.TotalChanged -= OnRevenueChanged;
                _revenue = null;
            }

            if (_wallet != null)
            {
                _wallet.BalanceChanged -= OnBalanceChanged;
                _wallet = null;
            }

            _placement = null;
            _coordinator = null;
            _stage = null;
            _progression = null;
            _pause = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// 인스펙터에서 비어 있는 라벨을 <b>자기 하위 계층에서만</b> 이름으로 찾아 채운다.
        /// <c>Find</c>/<c>FindObjectOfType</c> 같은 씬 전역 탐색이 아니다 (§4.3 금지 대상) —
        /// <c>StageBootstrap.ResolveMissingReferences</c> 와 같은 방식이며, 씬을 스크립트로
        /// 조립할 때 참조를 일일이 물리지 않아도 되게 한다.
        /// </summary>
        private void Awake()
        {
            _revenueLabel = HudLabel.Resolve(transform, _revenueLabel, RevenueLabelName);
            _walletLabel = HudLabel.Resolve(transform, _walletLabel, WalletLabelName);
            _placementLabel = HudLabel.Resolve(transform, _placementLabel, PlacementLabelName);
            _waitingLabel = HudLabel.Resolve(transform, _waitingLabel, WaitingLabelName);
            _timeLabel = HudLabel.Resolve(transform, _timeLabel, TimeLabelName);
            _outcomeLabel = HudLabel.Resolve(transform, _outcomeLabel, OutcomeLabelName);
            _stageLabel = HudLabel.Resolve(transform, _stageLabel, StageLabelName);
        }

        /// <summary>
        /// 배치 수만 매 프레임 확인한다. 배치 서비스에 변경 이벤트가 없기 때문이며,
        /// <b>값이 바뀐 프레임에만</b> 문자열을 만든다 — <c>Update</c> 경로에서 매 프레임
        /// 문자열을 만들면 WebGL 에서 GC 스파이크가 그대로 히칭이 된다 (§4.3).
        /// </summary>
        private void LateUpdate()
        {
            RefreshPlacement();
            RefreshWaiting();
            RefreshTime();
            RefreshBanner();
            RefreshStageNumber();
        }

        /// <summary>
        /// 남은 시간은 <b>초 단위로 잘라</b> 변경을 감지한다. 매 프레임 문자열을 만들면
        /// WebGL 에서 GC 스파이크가 그대로 히칭이 된다 (§4.3).
        ///
        /// <para>
        /// <c>CeilToInt</c> 인 이유: 0.3초 남았을 때 <c>0</c> 보다 <c>1</c> 이 낫다. 실제로
        /// 0 이 되는 순간은 만료 시점뿐이다.
        /// </para>
        /// </summary>
        private void RefreshTime()
        {
            if (_stage == null)
            {
                return;
            }

            var seconds = Mathf.CeilToInt(_stage.RemainingSeconds);
            if (seconds == _shownSeconds)
            {
                return;
            }

            _shownSeconds = seconds;
            TimeText = $"남은 시간 {seconds}";
            HudLabel.Write(_timeLabel, TimeText);
        }

        /// <summary>
        /// 화면 최상단 가운데의 한 줄 — 클리어 · 실패 · 일시정지.
        ///
        /// <para>
        /// <b>여기서 판정하지 않는다.</b> 결과는 컨트롤러가, 멈춤은 게이트가 이미 정했다 —
        /// 뷰가 매출과 시간을 다시 비교하면 판정이 두 곳에 살게 된다 (<c>CLAUDE.md</c> §3.2).
        /// </para>
        /// <para>
        /// <b>결과가 멈춤을 이긴다.</b> 실패한 판은 멈춘 판이기도 한데, 그때 알려야 하는
        /// 것은 «멈췄다» 가 아니라 «졌다» 이다.
        /// </para>
        /// </summary>
        private void RefreshBanner()
        {
            var outcome = _stage != null ? _stage.Outcome : StageOutcome.InProgress;
            var paused = _pause != null && _pause.IsPaused;

            if (outcome == _shownOutcome && paused == _shownPaused)
            {
                return;
            }

            _shownOutcome = outcome;
            _shownPaused = paused;

            OutcomeText = outcome switch
            {
                StageOutcome.Cleared => "클리어",
                StageOutcome.Failed => "실패",
                _ => paused ? PausedText : string.Empty
            };

            HudLabel.Write(_outcomeLabel, OutcomeText);
        }

        /// <summary>
        /// 몇 판째인가. <b>스테이지 설정의 표시 번호가 아니라 런의 진행 순서</b>다 —
        /// 순서의 진실은 목록이므로 번호도 거기서 나와야 한다
        /// (<c>RunProgression.CurrentStageNumber</c>).
        ///
        /// <para>
        /// 런이 끝나면 번호가 총수를 넘는다. 그대로 쓰면 «스테이지 4/3» 이 되므로
        /// <b>총수에서 자른다.</b>
        /// </para>
        /// </summary>
        private void RefreshStageNumber()
        {
            if (_progression == null)
            {
                return;
            }

            var number = Mathf.Min(_progression.CurrentStageNumber, _progression.StageCount);
            if (number == _shownStageNumber)
            {
                return;
            }

            _shownStageNumber = number;
            StageText = $"스테이지 {number}";
            HudLabel.Write(_stageLabel, StageText);
        }

        /// <summary>
        /// 대기 인원도 변경 이벤트가 없어 매 프레임 센다. 조율자에 대기 이벤트를 만들면
        /// 매 틱 손님 수만큼 델리게이트가 튀고 해제 경로도 함께 는다 — 세는 편이 싸다.
        /// </summary>
        private void RefreshWaiting()
        {
            if (_coordinator == null)
            {
                return;
            }

            var waiting = 0;
            var customers = _coordinator.Customers;
            for (var i = 0; i < customers.Count; i++)
            {
                if (_coordinator.IsWaiting(customers[i]))
                {
                    waiting++;
                }
            }

            if (waiting == _shownWaitingCount)
            {
                return;
            }

            _shownWaitingCount = waiting;
            WaitingText = $"대기 {waiting}";
            HudLabel.Write(_waitingLabel, WaitingText);
        }

        private void RefreshPlacement()
        {
            if (_placement == null)
            {
                return;
            }

            var placed = _placement.PlacedCount;
            if (placed == _shownPlacedCount)
            {
                return;
            }

            _shownPlacedCount = placed;
            PlacementText = $"손님 {placed}/{_placement.MaxPlacedCustomers}";
            HudLabel.Write(_placementLabel, PlacementText);
        }

        private void OnRevenueChanged(int total)
        {
            var target = _stage != null ? _stage.TargetRevenue : 0;
            RevenueText = $"매출 {total}/{target}";
            HudLabel.Write(_revenueLabel, RevenueText);
        }

        private void OnBalanceChanged(int balance)
        {
            WalletText = $"영입 재화 {balance}";
            HudLabel.Write(_walletLabel, WalletText);
        }

    }
}
