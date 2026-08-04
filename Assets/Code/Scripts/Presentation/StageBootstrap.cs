using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Scoring;
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
        [SerializeField] private CustomerData _defaultCustomer;

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

        /// <summary>인스펙터 없이 참조를 물린다. 테스트용 진입점이다.</summary>
        public void Initialize(StageConfig stageConfig, SushiPoolBehaviour viewPool,
                               SushiBeltView beltView, Transform beltStart, Transform beltEnd,
                               CustomerPlacementController placementController,
                               TableSlotView[] slots, CustomerData defaultCustomer)
        {
            _stageConfig = stageConfig;
            _viewPool = viewPool;
            _beltView = beltView;
            _beltStart = beltStart;
            _beltEnd = beltEnd;
            _placementController = placementController;
            _slots = slots;
            _defaultCustomer = defaultCustomer;
        }

        /// <summary>
        /// 로직을 조립하고 뷰에 물린다. <see cref="Awake"/> 가 자동으로 부르지만,
        /// 인스펙터 없이 세운 경우 테스트가 직접 부를 수 있다.
        /// </summary>
        public void Build()
        {
            Teardown();
            ResolveMissingReferences();

            Belt = new SushiBelt(_stageConfig, new SequenceNumberIssuer(),
                                 new SushiPool<SushiItem>(new SushiItemFactory()));
            Revenue = new RevenueLedger();
            Wallet = new RecruitWallet(_stageConfig.InitialRecruitBudget);
            Coordinator = new ClaimCoordinator(Belt, _stageConfig, Revenue, Wallet);
            Placement = new CustomerPlacementService(Coordinator, _stageConfig,
                                                     new SequenceNumberIssuer(), Wallet);

            _beltView.Initialize(_viewPool, _stageConfig, _beltStart, _beltEnd);
            _beltView.Bind(Belt);

            _placementController.Initialize(_slots, _defaultCustomer);
            _placementController.Bind(Placement);
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

        private void Update()
        {
            Coordinator?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Teardown();
        }

        private void Teardown()
        {
            _beltView?.Unbind();
            Coordinator?.Dispose();
            Coordinator = null;
            Belt = null;
            Placement = null;
            Wallet = null;
            Revenue = null;
        }
    }
}
