using System;
using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배치가 가능한지 판정하고 확정한다. 입력·화면을 모른다 — 그래야 배치 규칙이
    /// EditMode 로 검증된다 (<c>CLAUDE.md</c> §3.2).
    ///
    /// <para>
    /// <b>M1 의 배치는 무료다.</b> <c>CustomerData.RecruitCost</c> 와 영입 재화는 읽지 않는다 —
    /// 경제는 M2 다. 여기서 보는 제한은 자리 점유와 <c>StageConfig.MaxPlacedCustomers</c> 뿐이다.
    /// </para>
    /// </summary>
    public sealed class CustomerPlacementService
    {
        private readonly ClaimCoordinator _coordinator;
        private readonly StageConfig _config;
        private readonly SequenceNumberIssuer _customerSequenceNumbers;
        private readonly Dictionary<int, CustomerLogic> _bySlot = new();

        /// <summary>지금 배치돼 있는 손님 수.</summary>
        public int PlacedCount => _bySlot.Count;

        /// <summary>이 스테이지에 배치할 수 있는 손님 수의 상한.</summary>
        public int MaxPlacedCustomers => _config.MaxPlacedCustomers;

        public CustomerPlacementService(ClaimCoordinator coordinator, StageConfig config,
                                        SequenceNumberIssuer customerSequenceNumbers)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _customerSequenceNumbers = customerSequenceNumbers
                                       ?? throw new ArgumentNullException(nameof(customerSequenceNumbers));
        }

        /// <summary>이 자리에 놓을 수 있는가. 점유 여부와 배치 한도만 본다.</summary>
        public bool CanPlace(int slotIndex)
        {
            return !_bySlot.ContainsKey(slotIndex) && PlacedCount < MaxPlacedCustomers;
        }

        /// <summary>
        /// 배치를 확정한다. 손님 순차번호를 발급하고 조율자에 등록해
        /// <b>다음 배정부터 바로 참여</b>시킨다 — 스테이지 진행 중에도 부를 수 있다.
        /// 놓을 수 없는 자리면 예외를 던진다.
        /// </summary>
        public CustomerLogic Place(CustomerData data, int slotIndex, float slotBeltPosition)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!CanPlace(slotIndex))
            {
                throw new InvalidOperationException(
                    $"자리 {slotIndex} 에 배치할 수 없습니다 (점유 중이거나 배치 한도 {MaxPlacedCustomers} 초과).");
            }

            var state = new CustomerRuntimeState(data, _customerSequenceNumbers.Next());
            var customer = new CustomerLogic(state, slotBeltPosition);

            _bySlot[slotIndex] = customer;
            _coordinator.PlaceCustomer(customer);
            return customer;
        }

        /// <summary>
        /// 놓을 수 있으면 배치하고, 아니면 <c>null</c> 을 돌려준다.
        /// 입력 껍데기가 규칙을 되묻지 않아도 되게 하는 진입점이다.
        /// </summary>
        public CustomerLogic TryPlace(CustomerData data, int slotIndex, float slotBeltPosition)
        {
            return CanPlace(slotIndex) ? Place(data, slotIndex, slotBeltPosition) : null;
        }

        /// <summary>배치를 취소한다. 자리를 비우고 조율자에서 뺀다.</summary>
        public bool Remove(int slotIndex)
        {
            if (!_bySlot.TryGetValue(slotIndex, out var customer))
            {
                return false;
            }

            _bySlot.Remove(slotIndex);
            _coordinator.RemoveCustomer(customer);
            return true;
        }

        /// <summary>이 자리에 앉아 있는 손님. 비어 있으면 <c>null</c>.</summary>
        public CustomerLogic OccupantOf(int slotIndex)
        {
            return _bySlot.TryGetValue(slotIndex, out var customer) ? customer : null;
        }
    }
}
