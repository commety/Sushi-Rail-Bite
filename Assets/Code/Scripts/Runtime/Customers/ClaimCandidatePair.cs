using SushiDefense.Belt;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배정 후보 한 쌍 — "이 손님이 이 초밥을 가져갈 수도 있다".
    ///
    /// <para>
    /// 배정을 손님 기준이나 초밥 기준이 아니라 <b>쌍 기준</b>으로 다루는 이유는,
    /// 1:N · N:1 · N:M 을 케이스별로 나누지 않고 하나의 정렬로 덮기 위해서다
    /// (<c>.claude/domain/sushi-claim-flow.md</c> §2).
    /// </para>
    /// </summary>
    public readonly struct ClaimCandidatePair
    {
        /// <summary>이 쌍의 손님.</summary>
        public CustomerLogic Customer { get; }

        /// <summary>이 쌍의 초밥.</summary>
        public SushiItem Sushi { get; }

        public ClaimCandidatePair(CustomerLogic customer, SushiItem sushi)
        {
            Customer = customer;
            Sushi = sushi;
        }
    }
}
