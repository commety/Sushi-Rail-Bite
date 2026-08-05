using System;
using SushiDefense.Belt;

namespace SushiDefense.Customers
{
    /// <summary>
    /// "지금 확정할까, 더 좋은 것을 기다릴까" 만 답한다.
    ///
    /// <para>
    /// <b>누가 무엇을 가져가는지는 모른다</b> — 그건 <see cref="SushiClaimResolver"/> 의
    /// 몫이다. 랭킹과 타이밍을 한 함수에 합치면 둘 다 테스트가 어려워지므로, 이 클래스는
    /// 리졸버 <b>앞의 필터</b>로만 쓰인다 (작업서 D3).
    /// </para>
    /// <para>
    /// 규칙은 셋뿐이다.
    /// <list type="number">
    ///   <item>후보 중 <b>대역 안</b>이 있으면 즉시 확정한다</item>
    ///   <item>대역 밖만 있으면 <b>최선 후보가 범위를 벗어나기 직전</b>까지 기다린다</item>
    ///   <item>그 시각이 되면 확정한다</item>
    /// </list>
    /// 1번을 <b>최선 후보만 보고</b> 판단할 수 있는 이유: 대역 거리가 정렬 키 1이라,
    /// 최선이 대역 밖이면 나머지도 전부 대역 밖이다. 후보를 두 번 훑지 않는다.
    /// </para>
    /// <para>
    /// <b>마감시한 갱신 코드가 따로 없다.</b> 매번 최선 후보를 다시 고르므로, 더 좋은 것이
    /// 인식되면 최선이 바뀌고 마감시한도 함께 따라 움직인다. 못한 후보가 먼저 사라지는
    /// 것은 손해가 아니다.
    /// </para>
    /// <para>
    /// <b>유예 시간(초) 필드가 없다.</b> 마감은 그 (손님, 초밥) 쌍의 기하로 정해진다 —
    /// 벨트가 1차원·등속이라 <c>ReachWindow</c> 가 이미 답을 갖고 있다. 고정 길이 타이머는
    /// 쓸 수 없다: 유예 시계는 손님 기준이고 이탈은 초밥 기준이라 둘이 정렬되지 않는다.
    /// </para>
    /// <para>
    /// <b>자격을 다시 보지 않는다.</b> 자격 없는 손님은 조율자가 틱 5단계에서 이미 후보를
    /// 비웠으므로 <see cref="ClaimTiming.Nothing"/> 으로 자연히 걸러진다. 여기서
    /// <c>CanAcceptSushi</c> 를 부르면 자격과 타이밍이 한 함수가 된다.
    /// </para>
    /// </summary>
    public sealed class ClaimDeadline
    {
        private readonly ClaimPairComparer _comparer;

        /// <param name="comparer">
        /// 최선 후보를 고르는 데 쓴다. <b>정렬 키를 여기서 다시 짜지 않기 위해서다</b> —
        /// 같은 손님끼리 비교하면 키 4(대역 폭)·5(손님 SeqNo)가 동률이라 키 1~3 만
        /// 작동하고, 그게 곧 "이 손님에게 가장 좋은 초밥" 이다.
        /// </param>
        public ClaimDeadline(ClaimPairComparer comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        /// <summary>이 손님의 배정을 지금 확정해도 되는가.</summary>
        public ClaimTiming Evaluate(CustomerLogic customer, CandidateSet candidates, float nowSeconds)
        {
            if (customer == null)
            {
                throw new ArgumentNullException(nameof(customer));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            var bestIndex = BestIndex(customer, candidates);
            if (bestIndex < 0)
            {
                return ClaimTiming.Nothing;
            }

            var data = customer.State.Data;
            var bestPrice = candidates.Items[bestIndex].Data.Price;
            if (TargetingPriority.BandDistance(bestPrice, data.TargetingMin, data.TargetingMax) == 0)
            {
                return ClaimTiming.Due;
            }

            // 이탈 직후 한 틱 늦게 확정되는 것은 정상이다 — 래치가 정확히 그 지연을
            // 받쳐 주려고 있다 (§1.1-3c). 이탈 시각을 앞당겨 보정하지 않는다.
            return nowSeconds >= candidates.ExitAtOf(bestIndex)
                ? ClaimTiming.Due
                : ClaimTiming.Waiting;
        }

        /// <summary>
        /// 살아 있는 후보 중 이 손님에게 가장 좋은 것의 인덱스. 없으면 <c>-1</c>.
        ///
        /// <para>
        /// 이미 배정·소비된 초밥은 건너뛴다. <b>자격 판정을 다시 하는 것이 아니다</b> —
        /// 죽은 쌍을 최선으로 골라 두면 마감시한이 영영 오지 않는 초밥에 걸린다.
        /// </para>
        /// </summary>
        private int BestIndex(CustomerLogic customer, CandidateSet candidates)
        {
            var items = candidates.Items;
            var bestIndex = -1;
            var best = default(ClaimCandidatePair);

            for (var i = 0; i < items.Count; i++)
            {
                var sushi = items[i];
                if (sushi == null || sushi.State != SushiState.OnBelt)
                {
                    continue;
                }

                var pair = new ClaimCandidatePair(customer, sushi);
                if (bestIndex < 0 || _comparer.Compare(pair, best) < 0)
                {
                    bestIndex = i;
                    best = pair;
                }
            }

            return bestIndex;
        }
    }
}
