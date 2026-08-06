using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.Run
{
    /// <summary>
    /// 후보 추첨과 반영. <b>이미 가진 카드는 제시하지 않는다</b> — 스폰 비중이 가격에서만
    /// 유도되므로(<c>.claude/domain/spawn-composition.md</c>) 중복 카드는 아무 효과가 없다.
    ///
    /// <para>
    /// 난수를 쓰지만 <b>시드를 주입</b>하므로 테스트는 통계 검증이 아니라 정확한 값 비교다.
    /// </para>
    /// </summary>
    public sealed class RewardGeneratorTests
    {
        private const int Seed = 20260806;

        private readonly List<Object> _disposables = new();
        private readonly List<RewardOffer> _offers = new();

        private RewardCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<RewardCatalog>();
            _offers.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        [Test]
        public void Generate_EmptyCatalog_ProducesNothing()
        {
            SetOfferCount(3);

            Generate(NewRun());

            Assert.IsEmpty(_offers);
        }

        /// <summary>
        /// <b>가정적인 케이스가 아니다.</b> 착수 시점의 실제 상태다 — 초밥 5종이 전부
        /// stage01 시작 덱에 들어 있어 미보유 카드가 0개다. 밸런스 애셋을 사람이 정할
        /// 때까지(step-09) 이것이 정상 동작이다.
        /// </summary>
        [Test]
        public void Generate_AllCardsOwned_ProducesNothing()
        {
            var owned = AddSushiToPool();
            var member = AddCustomerToPool();
            SetOfferCount(3);

            var run = NewRun();
            run.Sushi.TryAdd(owned);
            run.Customers.TryAdd(member);

            Generate(run);

            Assert.IsEmpty(_offers);
        }

        /// <summary>
        /// <b>공허하게 통과하기 쉬운 자리다.</b> 보유 1 + 미보유 1 에 제시 개수 2 로 짜면
        /// 중복 제거를 안 하는 구현도 "둘 중 하나는 미보유" 로 통과한다. 보유 셋 + 미보유
        /// 하나에 제시 개수 셋으로 두어, 결과가 정확히 그 한 장이어야 통과하게 한다.
        /// </summary>
        [Test]
        public void Generate_OwnedCard_IsNeverOffered()
        {
            var ownedA = AddSushiToPool();
            var ownedB = AddSushiToPool();
            var ownedC = AddSushiToPool();
            var fresh = AddSushiToPool();
            SetOfferCount(3);

            var run = NewRun();
            run.Sushi.TryAdd(ownedA);
            run.Sushi.TryAdd(ownedB);
            run.Sushi.TryAdd(ownedC);

            Generate(run);

            Assert.AreEqual(1, _offers.Count);
            Assert.AreSame(fresh, _offers[0].Sushi);
        }

        [Test]
        public void Generate_PoolLargerThanOfferCount_ProducesExactlyOfferCount()
        {
            for (var i = 0; i < 10; i++)
            {
                AddSushiToPool();
            }

            SetOfferCount(3);

            Generate(NewRun());

            Assert.AreEqual(3, _offers.Count);
        }

        [Test]
        public void Generate_PoolSmallerThanOfferCount_ProducesWholePool()
        {
            AddSushiToPool();
            AddSushiToPool();
            SetOfferCount(5);

            Generate(NewRun());

            Assert.AreEqual(2, _offers.Count);
        }

        [Test]
        public void Generate_NoDuplicatesAmongOffers()
        {
            for (var i = 0; i < 6; i++)
            {
                AddSushiToPool();
            }

            SetOfferCount(6);

            Generate(NewRun());

            var seen = new HashSet<SushiData>();
            foreach (var offer in _offers)
            {
                Assert.IsTrue(seen.Add(offer.Sushi), "같은 카드가 두 번 제시됐습니다");
            }
        }

        [Test]
        public void Generate_MixedPools_CanOfferBothKinds()
        {
            for (var i = 0; i < 5; i++)
            {
                AddSushiToPool();
                AddCustomerToPool();
            }

            SetOfferCount(10);

            Generate(NewRun());

            var sushiCount = 0;
            var customerCount = 0;
            foreach (var offer in _offers)
            {
                if (offer.Kind == RewardKind.SushiCard)
                {
                    sushiCount++;
                }
                else
                {
                    customerCount++;
                }
            }

            Assert.AreEqual(5, sushiCount);
            Assert.AreEqual(5, customerCount);
        }

        /// <summary>
        /// <b>난수를 들이면서도 결정성을 잃지 않았다는 것을 이 테스트가 지킨다.</b>
        /// 같은 시드의 두 런은 같은 후보를 같은 순서로 낸다.
        /// </summary>
        [Test]
        public void Generate_SameSeedTwice_ProducesIdenticalOffers()
        {
            for (var i = 0; i < 10; i++)
            {
                AddSushiToPool();
            }

            SetOfferCount(4);

            var generator = new RewardGenerator(_catalog);
            var left = new List<RewardOffer>();
            var right = new List<RewardOffer>();

            generator.Generate(NewRun(), left);
            generator.Generate(NewRun(), right);

            Assert.AreEqual(4, left.Count);
            for (var i = 0; i < left.Count; i++)
            {
                Assert.AreSame(left[i].Sushi, right[i].Sushi, $"{i} 번째 후보가 어긋났습니다");
            }
        }

        /// <summary>
        /// 풀을 넉넉히 잡는다. 풀 크기와 제시 개수가 같으면 시드와 무관하게 같은 집합이
        /// 나와 이 테스트가 늘 실패한다.
        /// </summary>
        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentOffers()
        {
            for (var i = 0; i < 12; i++)
            {
                AddSushiToPool();
            }

            SetOfferCount(3);

            var generator = new RewardGenerator(_catalog);
            var left = new List<RewardOffer>();
            var right = new List<RewardOffer>();

            generator.Generate(new RunState(new SushiDeck(), new CustomerDeck(), Seed), left);
            generator.Generate(new RunState(new SushiDeck(), new CustomerDeck(), Seed + 7), right);

            var differs = false;
            for (var i = 0; i < left.Count; i++)
            {
                if (!ReferenceEquals(left[i].Sushi, right[i].Sushi))
                {
                    differs = true;
                }
            }

            Assert.IsTrue(differs, "다른 시드가 같은 후보를 냈습니다");
        }

        [Test]
        public void Generate_CatalogWithNullEntry_SkipsIt()
        {
            SerializedFieldSetter.AppendObject(_catalog, "_sushiPool", null);
            var real = AddSushiToPool();
            SetOfferCount(3);

            Generate(NewRun());

            Assert.AreEqual(1, _offers.Count);
            Assert.AreSame(real, _offers[0].Sushi);
        }

        /// <summary>결과 리스트를 먼저 비운다. 안 그러면 두 번째 클리어에 후보가 쌓인다.</summary>
        [Test]
        public void Generate_CalledTwice_DoesNotAccumulateIntoResults()
        {
            for (var i = 0; i < 5; i++)
            {
                AddSushiToPool();
            }

            SetOfferCount(2);

            var generator = new RewardGenerator(_catalog);
            var run = NewRun();
            generator.Generate(run, _offers);
            generator.Generate(run, _offers);

            Assert.AreEqual(2, _offers.Count);
        }

        // ── 반영 ────────────────────────────────────────────────

        [Test]
        public void Apply_SushiOffer_AddsToDeck()
        {
            var run = NewRun();
            var card = CreateSushi();

            Assert.IsTrue(RewardGenerator.Apply(run, RewardOffer.OfSushi(card)));

            Assert.IsTrue(run.Sushi.Contains(card));
            Assert.AreEqual(0, run.Customers.Count);
        }

        [Test]
        public void Apply_CustomerOffer_AddsToRoster()
        {
            var run = NewRun();
            var member = CreateCustomer();

            Assert.IsTrue(RewardGenerator.Apply(run, RewardOffer.OfCustomer(member)));

            Assert.IsTrue(run.Customers.Contains(member));
            Assert.AreEqual(0, run.Sushi.Count);
        }

        [Test]
        public void Apply_AlreadyOwned_ReturnsFalseAndLeavesRunUnchanged()
        {
            var run = NewRun();
            var card = CreateSushi();
            run.Sushi.TryAdd(card);

            Assert.IsFalse(RewardGenerator.Apply(run, RewardOffer.OfSushi(card)));

            Assert.AreEqual(1, run.Sushi.Count);
        }

        // ── 헬퍼 ────────────────────────────────────────────────

        private void Generate(RunState run)
        {
            new RewardGenerator(_catalog).Generate(run, _offers);
        }

        private RunState NewRun()
        {
            return new RunState(new SushiDeck(), new CustomerDeck(), Seed);
        }

        private void SetOfferCount(int value)
        {
            SerializedFieldSetter.SetInt(_catalog, "_offerCount", value);
        }

        private SushiData CreateSushi()
        {
            return StageConfigBuilder.CreateSushi(_disposables);
        }

        private CustomerData CreateCustomer()
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(customer);
            return customer;
        }

        private SushiData AddSushiToPool()
        {
            var sushi = CreateSushi();
            SerializedFieldSetter.AppendObject(_catalog, "_sushiPool", sushi);
            return sushi;
        }

        private CustomerData AddCustomerToPool()
        {
            var customer = CreateCustomer();
            SerializedFieldSetter.AppendObject(_catalog, "_customerPool", customer);
            return customer;
        }
    }
}
