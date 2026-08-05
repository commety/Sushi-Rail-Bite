using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Scoring;
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
    /// 렌더링에 <see cref="TextMesh"/> 를 쓴다. 씬에 Canvas 가 없고 전부 월드 스페이스라
    /// 여기에 맞췄고, <c>UnityEngine.UI</c> 어셈블리 참조를 늘리지 않기 위해서이기도 하다.
    /// <b>M6(메인화면·덱빌딩)에서 제대로 된 UI 로 교체될 placeholder 다.</b>
    /// </para>
    /// </summary>
    public sealed class StageHudView : MonoBehaviour
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string RevenueLabelName = "RevenueLabel";

        private const string WalletLabelName = "WalletLabel";
        private const string PlacementLabelName = "PlacementLabel";
        private const string WaitingLabelName = "WaitingLabel";

        [SerializeField] private TextMesh _revenueLabel;
        [SerializeField] private TextMesh _walletLabel;
        [SerializeField] private TextMesh _placementLabel;
        [SerializeField] private TextMesh _waitingLabel;

        private RevenueLedger _revenue;
        private RecruitWallet _wallet;
        private CustomerPlacementService _placement;
        private StageConfig _config;
        private ClaimCoordinator _coordinator;

        private int _shownPlacedCount = -1;
        private int _shownWaitingCount = -1;

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

        /// <summary>
        /// 표시 대상을 물린다. 이미 물려 있으면 먼저 끊는다 — <c>Build()</c> 를 두 번 부르면
        /// 구독이 겹쳐 한 번의 변화가 두 번 반영된다.
        /// </summary>
        public void Bind(RevenueLedger revenue, RecruitWallet wallet,
                         CustomerPlacementService placement, StageConfig config,
                         ClaimCoordinator coordinator)
        {
            Unbind();

            _revenue = revenue;
            _wallet = wallet;
            _placement = placement;
            _config = config;
            _coordinator = coordinator;

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
            RefreshPlacement();
            RefreshWaiting();
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
            _config = null;
            _coordinator = null;
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
            _revenueLabel = PlaceholderLabel.Resolve(transform, _revenueLabel, RevenueLabelName);
            _walletLabel = PlaceholderLabel.Resolve(transform, _walletLabel, WalletLabelName);
            _placementLabel = PlaceholderLabel.Resolve(transform, _placementLabel, PlacementLabelName);
            _waitingLabel = PlaceholderLabel.Resolve(transform, _waitingLabel, WaitingLabelName);
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
            PlaceholderLabel.Write(_waitingLabel, WaitingText);
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
            PlaceholderLabel.Write(_placementLabel, PlacementText);
        }

        private void OnRevenueChanged(int total)
        {
            var target = _config != null ? _config.TargetRevenue : 0;
            RevenueText = $"매출 {total}/{target}";
            PlaceholderLabel.Write(_revenueLabel, RevenueText);
        }

        private void OnBalanceChanged(int balance)
        {
            WalletText = $"영입 재화 {balance}";
            PlaceholderLabel.Write(_walletLabel, WalletText);
        }

    }
}
