using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Belt
{
    /// <summary>
    /// <see cref="SpawnShareTable"/> 는 "얼마나 자주" 만 답한다 — 순서는 <c>SpawnSequence</c> 다.
    ///
    /// <para>
    /// 둘을 나눈 이유가 여기서 보인다. share 가 깨지는 방식(스케일 의존·정규화 누락·
    /// 최저가 기준 착오)과 배출이 깨지는 방식(뭉침·비결정)이 서로 달라서, 한 클래스면
    /// 어느 쪽이 틀렸는지 테스트가 말해주지 못한다.
    /// </para>
    /// </summary>
    public sealed class SpawnShareTableTests
    {
        /// <summary>부동소수 비교 허용 오차. 밸런스 값이 아니라 테스트 도구다.</summary>
        private const float Tolerance = 1e-4f;

        /// <summary>기획 확정값. 유형별 매출 기여가 균등해지는 지점이다.</summary>
        private const float AlphaOne = 1f;

        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        [Test]
        public void Share_ExpensiveSushi_LowerThanCheap()
        {
            var table = NewTable(AlphaOne, 100, 300);

            Assert.That(table.ShareOf(1), Is.LessThan(table.ShareOf(0)));
        }

        [Test]
        public void Share_AllShares_SumToOne()
        {
            var table = NewTable(AlphaOne, 100, 150, 300);

            var sum = 0f;
            for (var i = 0; i < table.Count; i++)
            {
                sum += table.ShareOf(i);
            }

            Assert.That(sum, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Share_AllPricesScaledTenfold_ProducesIdenticalShares()
        {
            // 가격의 비율만 쓴다는 것의 검증. 절대 스케일 연산(가격/10 같은)이 끼면
            // 100엔 덱과 1000엔 덱이 다르게 동작하고, 그 순간 정규화 결정이 하나 늘어난다.
            var baseline = NewTable(AlphaOne, 100, 150, 300);
            var scaled = NewTable(AlphaOne, 1000, 1500, 3000);

            // 개수를 먼저 단언한다. 이게 없으면 Count 가 0 일 때 루프가 안 돌아
            // 아무것도 검증하지 않은 채 통과한다.
            Assert.AreEqual(3, baseline.Count);

            for (var i = 0; i < baseline.Count; i++)
            {
                Assert.That(scaled.ShareOf(i), Is.EqualTo(baseline.ShareOf(i)).Within(Tolerance),
                            $"{i} 번째 유형의 share 가 가격 스케일에 따라 달라졌습니다");
            }
        }

        [Test]
        public void Share_AlphaOne_EqualRevenuePerType()
        {
            // 플랜의 예시 덱 (장어 300 · 방어 150 · 연어 120 · 한치 100 · 광어 100).
            // α = 1.0 에서 `가격 × share` 가 모든 유형에서 같아야 한다 — 비싼 초밥은
            // 적게 나오지만 한 방이 커서, 총량으로는 이득도 손해도 아니라는 성질이다.
            var prices = new[] { 300, 150, 120, 100, 100 };
            var table = NewTable(AlphaOne, prices);

            Assert.AreEqual(prices.Length, table.Count);

            var expected = prices[0] * table.ShareOf(0);
            for (var i = 1; i < table.Count; i++)
            {
                Assert.That(prices[i] * table.ShareOf(i), Is.EqualTo(expected).Within(Tolerance),
                            $"{i} 번째 유형의 매출 기여가 어긋났습니다");
            }
        }

        [Test]
        public void Share_DeckWithoutCheapestCard_RebasesOnDeckMinimum()
        {
            // minPrice 는 덱 안에서 구한다. 덱이 바뀌면 남은 카드의 등장률도 바뀌는데,
            // 이건 버그가 아니라 덱빌딩의 의미 그 자체다.
            var withCheap = NewTable(AlphaOne, 100, 200);
            var withoutCheap = NewTable(AlphaOne, 200, 400);

            Assert.That(withoutCheap.ShareOf(0), Is.EqualTo(withCheap.ShareOf(0)).Within(Tolerance));
            Assert.AreEqual(200, withoutCheap.MinimumPrice);
        }

        [Test]
        public void MinimumPrice_Deck_IsCheapestInDeck()
        {
            var table = NewTable(AlphaOne, 300, 120, 500);

            Assert.AreEqual(120, table.MinimumPrice);
        }

        [Test]
        public void Share_AlphaZero_AllTypesEqual()
        {
            // α = 0 은 "가격을 무시한다" 는 유효한 구성이다.
            var table = NewTable(0f, 100, 500, 1000);

            Assert.AreEqual(3, table.Count);

            for (var i = 0; i < table.Count; i++)
            {
                Assert.That(table.ShareOf(i), Is.EqualTo(1f / 3f).Within(Tolerance));
            }
        }

        [Test]
        public void Share_HigherAlpha_MakesExpensiveRarer()
        {
            var gentle = NewTable(1f, 100, 400);
            var steep = NewTable(2f, 100, 400);

            Assert.That(steep.ShareOf(1), Is.LessThan(gentle.ShareOf(1)));
        }

        [Test]
        public void Count_EmptyDeck_IsZero()
        {
            var table = NewTable(AlphaOne);

            Assert.AreEqual(0, table.Count);
        }

        [Test]
        public void Count_EntryWithNullSushi_ExcludesIt()
        {
            var sushi = StageConfigBuilder.CreateSushi(_disposables, 100);

            var table = new SpawnShareTable(new[] { null, sushi }, AlphaOne);

            Assert.AreEqual(1, table.Count);
            Assert.AreSame(sushi, table.SushiAt(0));
        }

        [Test]
        public void SushiAt_Deck_PreservesOrder()
        {
            // 덱 순서는 배출 동률을 끝내는 기준이다 (step-03). 여기서 뒤섞이면
            // 스폰 결정성이 같이 무너진다.
            var table = NewTable(AlphaOne, 300, 100, 200);

            Assert.AreEqual(300, table.SushiAt(0).Price);
            Assert.AreEqual(100, table.SushiAt(1).Price);
            Assert.AreEqual(200, table.SushiAt(2).Price);
        }

        /// <summary>
        /// 덱을 바로 만든다. 비율표가 <c>StageConfig</c> 대신 초밥 목록을 받게 되면서
        /// 스테이지 SO 를 세울 이유가 사라졌다.
        /// </summary>
        private SpawnShareTable NewTable(float sparsityExponent, params int[] prices)
        {
            var deck = new List<SushiData>();
            foreach (var price in prices)
            {
                deck.Add(StageConfigBuilder.CreateSushi(_disposables, price));
            }

            return new SpawnShareTable(deck, sparsityExponent);
        }
    }
}
