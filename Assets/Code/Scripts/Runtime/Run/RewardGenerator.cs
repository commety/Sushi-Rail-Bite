using System;
using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.Run
{
    /// <summary>
    /// 클리어 시 제시할 후보를 뽑고, 고른 것을 런에 반영한다.
    ///
    /// <para>
    /// <b>이미 가진 카드는 제시하지 않는다.</b> 스폰 비중이 가격에서만 유도되므로
    /// (<c>.claude/domain/spawn-composition.md</c>) 중복 카드는 아무 효과가 없다 — 손으로
    /// 적는 가중치 필드가 없다는 것이 곧 그 뜻이다.
    /// </para>
    /// <para>
    /// <b>초밥과 손님을 한 풀에 섞는다.</b> 종류별 쿼터를 두지 않는다 — 쿼터를 두면 한쪽
    /// 풀이 말랐을 때 "몇 개를 제시하나" 가 애매해지고, 그 규칙이 곧 밸런스 손잡이가 되어
    /// SO 필드를 또 부른다.
    /// </para>
    /// </summary>
    public sealed class RewardGenerator
    {
        private readonly RewardCatalog _catalog;

        /// <summary>
        /// 미보유 후보 버퍼. 필드로 잡아 재사용한다 — 클리어는 판당 한 번이라 성능이
        /// 문제는 아니지만, 배출 경로가 매번 리스트를 만드는 형태로 굳으면 다른 경로도
        /// 따라간다.
        /// </summary>
        private readonly List<RewardOffer> _candidates = new();

        public RewardGenerator(RewardCatalog catalog)
        {
            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
        }

        /// <summary>
        /// 이 런에 제시할 후보를 <paramref name="results"/> 에 채운다. <b>먼저 비운다.</b>
        ///
        /// <para>
        /// 최대 <c>catalog.OfferCount</c> 개이며, 미보유 카드가 그보다 적으면 있는 만큼만
        /// 담긴다. 하나도 없으면 빈 목록이다 — 오류가 아니라 유효한 상태다.
        /// </para>
        /// </summary>
        public void Generate(RunState run, List<RewardOffer> results)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            CollectUnowned(run);

            var take = _catalog.OfferCount < _candidates.Count
                ? _catalog.OfferCount
                : _candidates.Count;

            // 부분 Fisher-Yates. 전체를 섞으면 카탈로그가 커질수록 낭비가 커지고,
            // "인덱스를 무작위로 골라 중복이면 다시" 방식은 루프 횟수가 난수에 따라 달라져
            // **같은 시드로도 소비되는 난수 개수가 흔들린다.** 여기서는 정확히 take 번 뽑는다.
            for (var i = 0; i < take; i++)
            {
                var pick = i + run.Random.Next(_candidates.Count - i);
                (_candidates[i], _candidates[pick]) = (_candidates[pick], _candidates[i]);
                results.Add(_candidates[i]);
            }

            _candidates.Clear();
        }

        /// <summary>
        /// 고른 보상을 런에 반영한다. 이미 가진 카드였다면 <c>false</c> 를 돌려주고
        /// <b>런을 그대로 둔다</b> — 부분 적용이 없다 (<c>RecruitWallet.TrySpend</c> 와 같은 계약).
        /// </summary>
        public static bool Apply(RunState run, in RewardOffer offer)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return offer.Kind == RewardKind.SushiCard
                ? run.Sushi.TryAdd(offer.Sushi)
                : run.Customers.TryAdd(offer.Customer);
        }

        private void CollectUnowned(RunState run)
        {
            _candidates.Clear();

            var sushiPool = _catalog.SushiPool;
            for (var i = 0; i < sushiPool.Count; i++)
            {
                var sushi = sushiPool[i];
                if (sushi != null && !run.Sushi.Contains(sushi))
                {
                    _candidates.Add(RewardOffer.OfSushi(sushi));
                }
            }

            var customerPool = _catalog.CustomerPool;
            for (var i = 0; i < customerPool.Count; i++)
            {
                var customer = customerPool[i];
                if (customer != null && !run.Customers.Contains(customer))
                {
                    _candidates.Add(RewardOffer.OfCustomer(customer));
                }
            }
        }
    }
}
