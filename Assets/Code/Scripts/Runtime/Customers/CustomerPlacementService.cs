using System;
using System.Collections.Generic;
using SushiDefense.Data;
using SushiDefense.Scoring;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배치가 가능한지 판정하고 확정한다. 입력·화면을 모른다 — 그래야 배치 규칙이
    /// EditMode 로 검증된다 (<c>CLAUDE.md</c> §3.2).
    ///
    /// <para>
    /// 배치 조건은 셋이다 — <b>자리가 비어 있다 ∧ 배치 한도 미만 ∧ 잔액 ≥ 영입 비용</b>.
    /// </para>
    /// <para>
    /// <b>스테이지 진행 중에도 배치할 수 있다</b> (M1 Q4 확정). 배치 즉시 조율자에 등록되어
    /// 다음 배정부터 참여한다.
    /// </para>
    /// </summary>
    public sealed class CustomerPlacementService
    {
        private readonly ClaimCoordinator _coordinator;
        private readonly StageConfig _config;
        private readonly SequenceNumberIssuer _customerSequenceNumbers;
        private readonly RecruitWallet _wallet;
        private readonly Dictionary<int, CustomerLogic> _bySlot = new();

        /// <summary>지금 배치돼 있는 손님 수.</summary>
        public int PlacedCount => _bySlot.Count;

        /// <summary>이 스테이지에 배치할 수 있는 손님 수의 상한.</summary>
        public int MaxPlacedCustomers => _config.MaxPlacedCustomers;

        public CustomerPlacementService(ClaimCoordinator coordinator, StageConfig config,
                                        SequenceNumberIssuer customerSequenceNumbers,
                                        RecruitWallet wallet)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _customerSequenceNumbers = customerSequenceNumbers
                                       ?? throw new ArgumentNullException(nameof(customerSequenceNumbers));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        /// <summary>
        /// 이 손님을 이 자리에 놓을 수 있는가 — 점유 · 배치 한도 · 잔액.
        ///
        /// <para>
        /// <b>잔액을 읽기만 한다.</b> 여기서 차감하면 <see cref="TryPlace"/> 가 검사와 확정에서
        /// 두 번 차감한다.
        /// </para>
        /// </summary>
        public bool CanPlace(CustomerData data, int slotIndex)
        {
            return data != null
                   && !_bySlot.ContainsKey(slotIndex)
                   && PlacedCount < MaxPlacedCustomers
                   && _wallet.CanAfford(data.RecruitCost);
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

            if (!CanPlace(data, slotIndex))
            {
                throw new InvalidOperationException(
                    $"자리 {slotIndex} 에 배치할 수 없습니다 " +
                    $"(점유 중이거나, 배치 한도 {MaxPlacedCustomers} 초과이거나, " +
                    $"영입 비용 {data.RecruitCost} 에 잔액 {_wallet.Balance} 가 모자랍니다).");
            }

            // 차감이 번호 발급보다 먼저다. 번호를 먼저 발급하면 실패한 배치가
            // 순차번호를 태워 이후 배정 결과가 달라진다 — 결정성이 흔들린다.
            if (!_wallet.TrySpend(data.RecruitCost))
            {
                throw new InvalidOperationException(
                    $"영입 비용 {data.RecruitCost} 을 지불하지 못했습니다 (잔액 {_wallet.Balance}).");
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
            return CanPlace(data, slotIndex) ? Place(data, slotIndex, slotBeltPosition) : null;
        }

        /// <summary>
        /// 배치를 취소한다. 자리를 비우고 조율자에서 뺀다.
        ///
        /// <para>
        /// <b>영입 비용은 환불하지 않는다.</b> 환불하면 배치·해제를 반복해 재화를 되찾는
        /// 경로가 생기고, 배치 비용이 제한으로서 의미를 잃는다.
        /// </para>
        /// </summary>
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
