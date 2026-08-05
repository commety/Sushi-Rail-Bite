using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님을 놓는 고정 자리. 점유 여부와 벨트 좌표를 들고 있고, 배치 규칙은 갖지 않는다 —
    /// "놓을 수 있나" 는 <c>CustomerPlacementService</c> 가 답한다 (step-08).
    /// </summary>
    public sealed class TableSlotView : MonoBehaviour
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private float _beltPosition;

        /// <summary>
        /// 이 자리에 미리 놓아 둔 손님 시각 표현. 비어 있을 때는 꺼져 있다.
        ///
        /// <para>
        /// 배치할 때마다 <c>Instantiate</c> 하지 않는 이유: 프로덕션에서 오브젝트를 새로 만드는
        /// 지점은 <c>SushiPoolBehaviour</c> 하나로 유지한다 (<c>CLAUDE.md</c> §3.4).
        /// 자리 수는 스테이지마다 고정이라 미리 놓아 두는 편이 단순하다.
        /// </para>
        /// </summary>
        [SerializeField] private CustomerView _seatVisual;

        /// <summary>스테이지 안에서 이 자리를 가리키는 번호.</summary>
        public int SlotIndex => _slotIndex;

        /// <summary>집기 범위 판정의 기준이 되는 1차원 벨트 좌표.</summary>
        public float BeltPosition => _beltPosition;

        /// <summary>지금 손님이 앉아 있는가.</summary>
        public bool IsOccupied => Occupant != null;

        /// <summary>앉아 있는 손님의 뷰. 비어 있으면 <c>null</c>.</summary>
        public CustomerView Occupant { get; private set; }

        /// <summary>스테이지 정의에서 자리 정보를 받는다. 화면 위치도 함께 맞춘다.</summary>
        public void Bind(TableSlotDefinition definition)
        {
            _slotIndex = definition.SlotIndex;
            _beltPosition = definition.BeltPosition;
            transform.localPosition = definition.Position;
        }

        private void Awake()
        {
            // 씬을 스크립트로 조립할 때 오브젝트 참조를 일일이 물리지 않아도 되게,
            // 비어 있으면 자기 자식에서 찾는다. 씬 전역 탐색이 아니다.
            if (_seatVisual == null)
            {
                _seatVisual = GetComponentInChildren<CustomerView>(true);
            }

            Vacate();
        }

        /// <summary>인스펙터 없이 자리 시각 표현을 물린다. 테스트·부트스트랩용이다.</summary>
        public void Initialize(int slotIndex, float beltPosition, CustomerView seatVisual)
        {
            _slotIndex = slotIndex;
            _beltPosition = beltPosition;
            _seatVisual = seatVisual;
            Vacate();
        }

        /// <summary>
        /// 손님을 앉힌다. 자리에 딸린 시각 표현을 켜고 로직을 물린다.
        /// <paramref name="coordinator"/> 는 뷰가 "기다리는 중" 을 물어볼 곳이며,
        /// 자리 자체는 그 값을 쓰지 않고 넘기기만 한다.
        /// </summary>
        public void Occupy(CustomerLogic logic, ClaimCoordinator coordinator)
        {
            Occupant = _seatVisual;

            if (_seatVisual == null)
            {
                return;
            }

            _seatVisual.Bind(logic, coordinator);
            _seatVisual.gameObject.SetActive(true);
        }

        /// <summary>자리를 비운다.</summary>
        public void Vacate()
        {
            Occupant = null;

            if (_seatVisual == null)
            {
                return;
            }

            _seatVisual.Bind(null, null);
            _seatVisual.gameObject.SetActive(false);
        }
    }
}
