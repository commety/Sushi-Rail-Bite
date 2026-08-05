using System.Collections.Generic;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배정 쌍의 우선순위. <b>기획의 정렬 키 4단이 사는 유일한 지점이다.</b>
    ///
    /// <list type="number">
    ///   <item><c>|가격 − 타겟팅|</c> — 가까울수록 먼저</item>
    ///   <item>가격 — <b>높을수록</b> 먼저</item>
    ///   <item>초밥 순차번호 — 낮을수록 먼저</item>
    ///   <item>손님 순차번호 — 낮을수록 먼저</item>
    /// </list>
    ///
    /// <para>
    /// <b>초밥 키가 손님 키보다 위다.</b> TD 로 보면 적(초밥)이 최선, 타워(손님)가 차선이다.
    /// 순차번호가 유일하므로 이 비교는 완전순서이고, 서로 다른 쌍에서 0 이 나오지 않는다 —
    /// 덕분에 승자가 항상 하나로 확정되고 난수가 필요 없다 (<c>CLAUDE.md</c> §1.1-3b).
    /// </para>
    /// <para>
    /// <b>케이스별 분기가 없다.</b> 이 한 규칙이 1:N · N:1 · N:M 을 전부 덮는다 — 경우를
    /// 나눠 짜면 경계에서 반드시 어긋난다 (<c>.claude/domain/sushi-claim-flow.md</c> §2).
    /// </para>
    /// <para>
    /// 여기가 가격을 읽는다고 해서 <b>자격 판정이 가격을 보는 것은 아니다.</b> 자격은
    /// <see cref="CustomerLogic"/> 이 범위·포화도·상태만으로 이미 끝냈고, 이 비교자에
    /// 들어오는 쌍은 그 관문을 통과한 것들이다.
    /// </para>
    /// </summary>
    public sealed class ClaimPairComparer : IComparer<ClaimCandidatePair>
    {
        public int Compare(ClaimCandidatePair a, ClaimCandidatePair b)
        {
            // 1순위 — 타겟팅 거리. 오름차순이다 (도메인 문서의 −|가격−타겟팅| 과 결과가 같다).
            var byDistance = DistanceOf(a).CompareTo(DistanceOf(b));
            if (byDistance != 0)
            {
                return byDistance;
            }

            // 2순위 — 가격. 동거리는 `타겟팅 ± d` 두 지점에서만 생기므로 여기서 비싼 쪽을
            // 고른다. 목적이 점수 최대화라서다. 인자 순서를 뒤집은 것이 "높을수록 먼저" 다.
            var byPrice = b.Sushi.Data.Price.CompareTo(a.Sushi.Data.Price);
            if (byPrice != 0)
            {
                return byPrice;
            }

            // 3순위 — 초밥 순차번호. 여기 오는 것은 가격이 완전히 같은 초밥들이다
            // (같은 종류가 두 번 스폰된 흔한 상황).
            var bySushi = a.Sushi.SequenceNumber.CompareTo(b.Sushi.SequenceNumber);
            if (bySushi != 0)
            {
                return bySushi;
            }

            // 4순위 — 손님 순차번호. 같은 초밥을 두고 겨룰 때만 작동한다.
            return a.Customer.State.SequenceNumber.CompareTo(b.Customer.State.SequenceNumber);
        }

        private static int DistanceOf(ClaimCandidatePair pair)
        {
            // step-01 임시 배선 — 대역 하한만 넘겨 옛 단일 값 동작을 유지한다.
            // 대역 거리(BandDistance)로의 교체는 step-02·03 이다.
            return TargetingPriority.Distance(pair.Sushi.Data.Price,
                                              pair.Customer.State.Data.TargetingMin);
        }
    }
}
