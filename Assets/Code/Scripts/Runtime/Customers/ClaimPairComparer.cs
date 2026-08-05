using System.Collections.Generic;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 배정 쌍의 우선순위. <b>기획의 정렬 키 5단이 사는 유일한 지점이다.</b>
    ///
    /// <list type="number">
    ///   <item>대역 밖 거리 <c>max(0, min−가격, 가격−max)</c> — 가까울수록 먼저. 대역 안은 전부 0</item>
    ///   <item>가격 — <b>높을수록</b> 먼저. 대역 안 동점을 여기서 깬다</item>
    ///   <item>초밥 순차번호 — 낮을수록 먼저</item>
    ///   <item>대역 폭 — <b>좁을수록</b> 먼저. 전문가가 범용가를 이긴다</item>
    ///   <item>손님 순차번호 — 낮을수록 먼저</item>
    /// </list>
    ///
    /// <para>
    /// <b>초밥 키(1~3)가 손님 키(4~5)보다 위다.</b> TD 로 보면 적(초밥)이 최선, 타워(손님)가
    /// 차선이다. 순차번호가 유일하므로 이 비교는 완전순서이고, 서로 다른 쌍에서 0 이 나오지
    /// 않는다 — 덕분에 승자가 항상 하나로 확정되고 난수가 필요 없다 (<c>CLAUDE.md</c> §1.1-3b).
    /// </para>
    /// <para>
    /// <b>시간을 모른다.</b> "지금 확정할까 기다릴까" 는 <c>ClaimDeadline</c> 이 답한다 —
    /// 랭킹과 타이밍은 다른 관심사다. 이 비교자에 <c>nowSeconds</c> 를 넘기고 싶어지면
    /// 설계가 틀어진 것이다.
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

            // 4순위 — 대역 폭. 좁은 쪽이 먼저다. 전문가가 범용가를 이긴다.
            // 이게 없으면 대역이 겹치는 구간에서 **배치 순서**가 승자를 정해, 소식좌가
            // 자기 전문 분야를 먼저 앉은 범용 손님에게 빼앗긴다.
            var byWidth = WidthOf(a).CompareTo(WidthOf(b));
            if (byWidth != 0)
            {
                return byWidth;
            }

            // 5순위 — 손님 순차번호. 같은 초밥을 같은 폭의 손님 둘이 겨룰 때만 작동한다.
            return a.Customer.State.SequenceNumber.CompareTo(b.Customer.State.SequenceNumber);
        }

        private static int DistanceOf(ClaimCandidatePair pair)
        {
            var data = pair.Customer.State.Data;
            return TargetingPriority.BandDistance(pair.Sushi.Data.Price,
                                                  data.TargetingMin, data.TargetingMax);
        }

        private static int WidthOf(ClaimCandidatePair pair)
        {
            var data = pair.Customer.State.Data;
            return TargetingPriority.BandWidth(data.TargetingMin, data.TargetingMax);
        }
    }
}
