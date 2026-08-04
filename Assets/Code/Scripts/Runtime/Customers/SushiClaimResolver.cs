using System;
using System.Collections.Generic;
using SushiDefense.Belt;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 인식된 쌍을 랭킹해 그리디로 배정한다.
    ///
    /// <para>
    /// 케이스별 분기를 두지 않는다 — 하나의 정렬 규칙이 1:N · N:1 · N:M 을 모두 덮는다
    /// (<c>.claude/domain/sushi-claim-flow.md</c> §2). 경우를 나눠 짜면 경계에서 반드시 어긋난다.
    /// </para>
    /// <para>
    /// <b>난수를 쓰지 않는다.</b> 순차번호가 모든 동률을 끝내므로 같은 판 상태는 항상 같은
    /// 배정을 낸다 (<c>CLAUDE.md</c> §1.1-3b).
    /// </para>
    /// </summary>
    public sealed class SushiClaimResolver
    {
        private readonly ClaimPairComparer _comparer = new();
        private readonly List<ClaimCandidatePair> _pairs = new();
        private readonly HashSet<CustomerLogic> _assigned = new();

        /// <summary>
        /// 배정을 확정한다. 확정된 쌍의 손님과 초밥은 이후 쌍에서 제외된다.
        ///
        /// <para>
        /// <paramref name="customers"/> 는 <b>이미 자격을 통과한 손님</b>이라는 것이 전제다 —
        /// 자격 판정을 여기서 다시 하지 않는다 (작업서 D6).
        /// </para>
        /// <para>
        /// 결과를 반환하지 않고 <paramref name="results"/> 에 채우는 이유는, 이 경로가 배정이
        /// 필요할 때마다 돌기 때문이다. 반환하면 호출마다 할당이 생긴다.
        /// </para>
        /// </summary>
        public void Resolve(IReadOnlyList<CustomerLogic> customers,
                            IReadOnlyDictionary<CustomerLogic, CandidateSet> candidates,
                            List<ClaimCandidatePair> results)
        {
            if (customers == null)
            {
                throw new ArgumentNullException(nameof(customers));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            CollectPairs(customers, candidates);
            _pairs.Sort(_comparer);
            Assign(results);
        }

        private void CollectPairs(IReadOnlyList<CustomerLogic> customers,
                                  IReadOnlyDictionary<CustomerLogic, CandidateSet> candidates)
        {
            _pairs.Clear();

            for (var i = 0; i < customers.Count; i++)
            {
                var customer = customers[i];
                if (!candidates.TryGetValue(customer, out var candidateSet))
                {
                    continue;
                }

                var items = candidateSet.Items;
                for (var j = 0; j < items.Count; j++)
                {
                    var sushi = items[j];

                    // 이미 배정·소비된 초밥은 죽은 쌍이라 정렬에 넣지 않는다.
                    // 자격 판정을 다시 하는 것이 아니다 — 그건 호출자가 이미 끝냈다.
                    if (sushi == null || sushi.State != SushiState.OnBelt)
                    {
                        continue;
                    }

                    _pairs.Add(new ClaimCandidatePair(customer, sushi));
                }
            }
        }

        private void Assign(List<ClaimCandidatePair> results)
        {
            _assigned.Clear();

            for (var i = 0; i < _pairs.Count; i++)
            {
                var pair = _pairs[i];
                if (_assigned.Contains(pair.Customer))
                {
                    continue;
                }

                // 초밥 쪽 중복은 TryClaim 이 막는다 — 이미 확정된 초밥은 false 를 돌려준다.
                if (!pair.Sushi.TryClaim(pair.Customer.State.SequenceNumber))
                {
                    continue;
                }

                _assigned.Add(pair.Customer);
                results.Add(pair);
            }
        }
    }
}
