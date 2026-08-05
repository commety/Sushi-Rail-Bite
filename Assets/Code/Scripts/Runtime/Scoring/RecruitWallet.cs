using System;

namespace SushiDefense.Scoring
{
    /// <summary>
    /// 손님 배치에 쓰는 영입 재화. 스테이지 초기 예산으로 열리고, 초밥이 소비될 때마다
    /// <c>가격 / 10</c> 만큼 적립된다.
    ///
    /// <para>
    /// <b>정수 나눗셈이다</b> (착수 시 확정). 가격 하한이 100 이라 초밥 1개당 최소 10 이
    /// 들어오므로, 실수 누적·잔여분 이월 같은 장치 없이도 재화가 조용히 0 에 머무는
    /// 상황이 생기지 않는다. 실수를 경유하면 결과는 같아도 다음 사람이 잔여분 이월을
    /// 넣고 싶어진다.
    /// </para>
    /// <para>
    /// <b>매출 원장을 구독하지 않는다.</b> 조율자가 소비 시점에 양쪽 모두에 알린다 —
    /// 구독하면 초기 예산과 매출 유래분이 한 스트림에 섞인다.
    /// </para>
    /// <para>
    /// <b>스테이지 간 이월이 없다</b> (착수 시 확정). <see cref="Reset"/> 은 잔액을 초기
    /// 예산으로 되돌린다.
    /// </para>
    /// </summary>
    public sealed class RecruitWallet
    {
        /// <summary>
        /// 재화 1 에 해당하는 매출. 원본 기획의 <c>점수/10</c> 을 그대로 옮긴 것이라
        /// 튜닝 손잡이가 아니다 — 스테이지마다 다르게 하고 싶어지면 그때 SO 로 승격한다.
        /// </summary>
        private const int RevenuePerCurrency = 10;

        private readonly int _initialBudget;

        /// <summary>지금 쓸 수 있는 영입 재화.</summary>
        public int Balance { get; private set; }

        /// <summary>잔액이 바뀌었다. 값이 실제로 달라질 때만 발생한다.</summary>
        public event Action<int> BalanceChanged;

        /// <summary>초기 예산으로 스테이지를 연다.</summary>
        public RecruitWallet(int initialBudget)
        {
            if (initialBudget < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialBudget), initialBudget,
                                                      "초기 예산은 음수일 수 없습니다.");
            }

            _initialBudget = initialBudget;
            Balance = initialBudget;
        }

        /// <summary>소비된 초밥 가격에서 재화를 적립한다.</summary>
        public void AccrueFrom(int price)
        {
            if (price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price), price, "가격은 음수일 수 없습니다.");
            }

            Change(price / RevenuePerCurrency);
        }

        /// <summary>이 비용을 지불할 수 있는가. <b>잔액을 바꾸지 않는다.</b></summary>
        public bool CanAfford(int cost)
        {
            return cost <= Balance;
        }

        /// <summary>
        /// 비용을 지불한다. 모자라면 <c>false</c> 를 돌려주고 <b>잔액을 그대로 둔다</b> —
        /// 부분 차감이 없다.
        /// </summary>
        public bool TrySpend(int cost)
        {
            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), cost, "비용은 음수일 수 없습니다.");
            }

            if (!CanAfford(cost))
            {
                return false;
            }

            Change(-cost);
            return true;
        }

        /// <summary>스테이지 시작 시 초기 예산으로 되돌린다.</summary>
        public void Reset()
        {
            Change(_initialBudget - Balance);
        }

        private void Change(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            Balance += delta;
            BalanceChanged?.Invoke(Balance);
        }
    }
}
