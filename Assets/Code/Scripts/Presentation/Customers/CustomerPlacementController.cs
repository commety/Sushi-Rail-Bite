using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배치 입력만 처리하는 껍데기.
    ///
    /// <para>
    /// <b>규칙이 하나도 없다.</b> "놓을 수 있나" 는 <see cref="CustomerPlacementService"/> 가
    /// 답하고, 이 클래스는 그 답을 화면에 반영만 한다. 규칙을 여기 복제하면 두 판정이
    /// 어긋날 수 있고 EditMode 로 검증할 수도 없다 (<c>CLAUDE.md</c> §3.2).
    /// </para>
    /// </summary>
    public sealed class CustomerPlacementController : MonoBehaviour
    {
        [SerializeField] private TableSlotView[] _slots;
        [SerializeField] private CustomerData[] _roster;

        private CustomerPlacementService _service;
        private ClaimCoordinator _coordinator;
        private int _pendingIndex;

        /// <summary>
        /// 지금 앉히려는 손님. 명부가 비었으면 <c>null</c>.
        ///
        /// <para>
        /// 명부는 런이 들고 있고 보상으로 자란다. <b>고르는 UI 는 여기 없다</b> —
        /// <see cref="SelectPending"/> 이 공개 진입점이며, 실제 선택 화면은 M6(메인화면·
        /// 덱빌딩)에서 만든다. 지금 만들면 그때 버린다.
        /// </para>
        /// </summary>
        public CustomerData PendingCustomer =>
            _roster != null && _pendingIndex >= 0 && _pendingIndex < _roster.Length
                ? _roster[_pendingIndex]
                : null;

        /// <summary>
        /// 씬 진입점이 배치 서비스를 물려 준다. 조율자는 <b>여기서 쓰지 않고</b> 앉히는
        /// 손님 뷰에 그대로 넘긴다 — 뷰가 스스로 찾으면 §4.3 위반이다.
        /// </summary>
        public void Bind(CustomerPlacementService service, ClaimCoordinator coordinator)
        {
            _service = service;
            _coordinator = coordinator;
        }

        /// <summary>인스펙터 없이 자리 목록과 명부를 물린다. 테스트·부트스트랩용이다.</summary>
        public void Initialize(TableSlotView[] slots, CustomerData[] roster)
        {
            _slots = slots;
            _roster = roster;
            _pendingIndex = 0;
        }

        /// <summary>
        /// 다음에 앉힐 손님을 고른다. 범위를 벗어나면 <c>false</c> 를 돌려주고
        /// <b>선택을 그대로 둔다</b>.
        /// </summary>
        public bool SelectPending(int index)
        {
            if (_roster == null || index < 0 || index >= _roster.Length)
            {
                return false;
            }

            _pendingIndex = index;
            return true;
        }

        /// <summary>
        /// 이 자리에 배치를 시도한다. 클릭·터치 핸들러가 부른다.
        /// 놓을 수 없으면 서비스가 <c>null</c> 을 돌려주고, 여기서는 아무 일도 하지 않는다.
        /// </summary>
        public bool TryPlaceAt(TableSlotView slot)
        {
            return TryPlace(PendingCustomer, slot);
        }

        /// <summary>
        /// <b>어느 손님을</b> 놓을지 함께 받는다. 손패에서 카드를 끌어다 놓는 경로가 쓰며,
        /// 고르는 것과 놓는 것이 한 동작이라 선택 상태를 거칠 이유가 없다.
        /// </summary>
        public bool TryPlace(CustomerData customer, TableSlotView slot)
        {
            if (customer == null || slot == null)
            {
                return false;
            }

            var placed = _service.TryPlace(customer, slot.SlotIndex, slot.BeltPosition);
            if (placed == null)
            {
                return false;
            }

            slot.Occupy(placed, _coordinator);
            return true;
        }

        /// <summary>
        /// 이 손님을 이 자리에 놓을 수 있는가. <b>판정하지 않고 서비스에 묻는다</b> —
        /// 카드를 흐리게 표시하려면 놓아 보기 전에 알아야 한다.
        /// </summary>
        public bool CanPlace(CustomerData customer, TableSlotView slot)
        {
            return customer != null && slot != null && _service != null
                   && _service.CanPlace(customer, slot.SlotIndex);
        }

        /// <summary>이 자리의 손님을 물린다. 배치 취소 조작이 부른다.</summary>
        public bool RemoveAt(TableSlotView slot)
        {
            if (!_service.Remove(slot.SlotIndex))
            {
                return false;
            }

            slot.Vacate();
            return true;
        }
    }
}
