using System;

namespace SushiDefense.Scoring
{
    /// <summary>
    /// 스테이지 매출. 소비된 초밥 가격의 합이며 클리어 판정(M3)의 입력이다.
    ///
    /// <para>
    /// <b>영입 재화와 합치지 않는다.</b> 둘은 획득 경로도 용도도 다른 자원이고, 하나로
    /// 묶으면 "스테이지 초기 예산" 과 "매출 유래분" 이 한 스트림에 섞여 잔액 추적이
    /// 어려워진다 (<c>.claude/domain/data-model.md</c> §4).
    /// </para>
    /// <para>
    /// <b>초밥을 받지 않는다</b> — <c>int</c> 만 받으므로 이 클래스가 벨트·손님 어느 쪽에도
    /// 묶이지 않는다.
    /// </para>
    /// </summary>
    public sealed class RevenueLedger
    {
        /// <summary>지금까지 누적된 매출.</summary>
        public int Total { get; private set; }

        /// <summary>매출이 바뀌었다. 값이 실제로 달라질 때만 발생한다.</summary>
        public event Action<int> TotalChanged;

        /// <summary>
        /// 초밥 1개가 소비됐다. 가격을 그대로 더한다.
        /// 음수는 예외다 — 조용히 통과시키면 매출이 줄어드는 경로가 생긴다.
        /// </summary>
        public void Add(int price)
        {
            if (price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price), price, "가격은 음수일 수 없습니다.");
            }

            if (price == 0)
            {
                return;
            }

            Total += price;
            TotalChanged?.Invoke(Total);
        }

        /// <summary>스테이지 시작 시 0 으로 되돌린다.</summary>
        public void Reset()
        {
            if (Total == 0)
            {
                return;
            }

            Total = 0;
            TotalChanged?.Invoke(Total);
        }
    }
}
