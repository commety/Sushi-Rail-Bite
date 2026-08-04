using System.Collections.Generic;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배정 쌍의 우선순위. <b>M2 가 정렬 키를 추가하는 유일한 지점이다.</b>
    ///
    /// <para>
    /// M1 의 키 — 초밥 순차번호(낮은 순) → 손님 순차번호(낮은 순).
    /// M2 에서 앞에 붙을 키 — <c>−|가격 − 타겟팅|</c>(가까운 순) → 가격(높은 순).
    /// </para>
    /// <para>
    /// <b>초밥 키가 손님 키보다 위다.</b> TD 로 보면 적(초밥)이 최선, 타워(손님)가 차선이다.
    /// 순차번호가 유일하므로 이 비교는 완전순서이고, 서로 다른 쌍에서 0 이 나오지 않는다 —
    /// 덕분에 승자가 항상 하나로 확정되고 난수가 필요 없다 (<c>CLAUDE.md</c> §1.1-3b).
    /// </para>
    /// </summary>
    public sealed class ClaimPairComparer : IComparer<ClaimCandidatePair>
    {
        public int Compare(ClaimCandidatePair a, ClaimCandidatePair b)
        {
            // 1순위 — 초밥 순차번호. M2 에서 이 앞에 타겟팅 거리와 가격이 들어온다.
            var bySushi = a.Sushi.SequenceNumber.CompareTo(b.Sushi.SequenceNumber);
            if (bySushi != 0)
            {
                return bySushi;
            }

            // 2순위 — 손님 순차번호. 같은 초밥을 두고 겨룰 때만 작동한다.
            return a.Customer.State.SequenceNumber.CompareTo(b.Customer.State.SequenceNumber);
        }
    }
}
