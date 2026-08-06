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
    /// 덱은 런 중에 자란다. <c>StageConfig.SpawnTable</c> 은 그 <b>시작 상태</b>일 뿐이며,
    /// 벨트가 SO 를 직접 읽으면 보상이 반영될 자리가 없다 (작업서 D4).
    /// </summary>
    public sealed class SushiDeckTests
    {
        private readonly List<Object> _disposables = new();

        private StageConfig _config;

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                Object.DestroyImmediate(_config);
                _config = null;
            }

            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        [Test]
        public void Cards_NewDeck_IsEmptyNotNull()
        {
            var deck = new SushiDeck();

            Assert.IsNotNull(deck.Cards);
            Assert.IsEmpty(deck.Cards);
            Assert.AreEqual(0, deck.Count);
        }

        [Test]
        public void TryAdd_NewCard_AddsAndReturnsTrue()
        {
            var deck = new SushiDeck();
            var card = CreateSushi();

            Assert.IsTrue(deck.TryAdd(card));

            Assert.AreEqual(1, deck.Count);
            Assert.IsTrue(deck.Contains(card));
        }

        [Test]
        public void TryAdd_DuplicateCard_ReturnsFalseAndKeepsCount()
        {
            var deck = new SushiDeck();
            var card = CreateSushi();
            deck.TryAdd(card);

            Assert.IsFalse(deck.TryAdd(card));

            Assert.AreEqual(1, deck.Count);
        }

        [Test]
        public void TryAdd_Null_ReturnsFalse()
        {
            var deck = new SushiDeck();

            Assert.IsFalse(deck.TryAdd(null));

            Assert.AreEqual(0, deck.Count);
        }

        [Test]
        public void Contains_CardNeverAdded_ReturnsFalse()
        {
            var deck = new SushiDeck();
            deck.TryAdd(CreateSushi());

            Assert.IsFalse(deck.Contains(CreateSushi()));
        }

        /// <summary>
        /// <b>순서가 계약이다.</b> <c>SpawnSequence</c> 는 credit 동률을 덱 순서로 끊으므로,
        /// 순서가 흔들리면 스폰 결과가 흔들린다. 카드 셋으로 짠다 — 둘이면 우연히 맞을 여지가 있다.
        /// </summary>
        [Test]
        public void Cards_MultipleAdds_PreservesInsertionOrder()
        {
            var deck = new SushiDeck();
            var first = CreateSushi();
            var second = CreateSushi();
            var third = CreateSushi();

            deck.TryAdd(second);
            deck.TryAdd(third);
            deck.TryAdd(first);

            Assert.AreSame(second, deck.Cards[0]);
            Assert.AreSame(third, deck.Cards[1]);
            Assert.AreSame(first, deck.Cards[2]);
        }

        [Test]
        public void FromSpawnTable_ConfigWithDeck_CopiesEveryCardInOrder()
        {
            var first = CreateSushi();
            var second = CreateSushi();
            _config = new StageConfigBuilder()
                .WithSpawnEntry(first)
                .WithSpawnEntry(second)
                .Build();

            var deck = SushiDeck.FromSpawnTable(_config);

            Assert.AreEqual(2, deck.Count);
            Assert.AreSame(first, deck.Cards[0]);
            Assert.AreSame(second, deck.Cards[1]);
        }

        /// <summary>
        /// 빈 슬롯은 인스펙터에서 목록을 늘리면 자연히 생긴다. <c>SpawnShareTable</c> 이
        /// 이미 같은 상황을 걸러 내고 있으므로 여기서도 거른다.
        /// </summary>
        [Test]
        public void FromSpawnTable_EntryWithNullSushi_SkipsIt()
        {
            var real = CreateSushi();
            _config = new StageConfigBuilder()
                .WithSpawnEntry(null)
                .WithSpawnEntry(real)
                .Build();

            var deck = SushiDeck.FromSpawnTable(_config);

            Assert.AreEqual(1, deck.Count);
            Assert.AreSame(real, deck.Cards[0]);
        }

        [Test]
        public void FromSpawnTable_EmptyConfig_ProducesEmptyDeck()
        {
            _config = new StageConfigBuilder().Build();

            var deck = SushiDeck.FromSpawnTable(_config);

            Assert.AreEqual(0, deck.Count);
        }

        [Test]
        public void FromSpawnTable_TwiceFromSameConfig_ProducesEqualDecks()
        {
            _config = new StageConfigBuilder()
                .WithSpawnEntry(CreateSushi())
                .WithSpawnEntry(CreateSushi())
                .Build();

            var left = SushiDeck.FromSpawnTable(_config);
            var right = SushiDeck.FromSpawnTable(_config);

            Assert.AreEqual(left.Count, right.Count);
            for (var i = 0; i < left.Count; i++)
            {
                Assert.AreSame(left.Cards[i], right.Cards[i], $"{i} 번째가 어긋났습니다");
            }
        }

        /// <summary>
        /// 덱은 SO 의 <b>사본</b>이다. 런타임이 SO 를 오염시키면 플레이 종료 후에도 디스크에
        /// 남는다 (<c>.claude/rules/scriptable-object.md</c> §2).
        /// </summary>
        [Test]
        public void FromSpawnTable_MutatingDeck_DoesNotTouchConfig()
        {
            _config = new StageConfigBuilder()
                .WithSpawnEntry(CreateSushi())
                .Build();
            var deck = SushiDeck.FromSpawnTable(_config);

            deck.TryAdd(CreateSushi());

            Assert.AreEqual(2, deck.Count);
            Assert.AreEqual(1, _config.SpawnTable.Count, "SO 의 시작 덱이 함께 자랐다");
        }

        [Test]
        public void Constructor_WithCards_CopiesThemInOrder()
        {
            var first = CreateSushi();
            var second = CreateSushi();

            var deck = new SushiDeck(new[] { first, second });

            Assert.AreEqual(2, deck.Count);
            Assert.AreSame(first, deck.Cards[0]);
            Assert.AreSame(second, deck.Cards[1]);
        }

        [Test]
        public void Constructor_WithDuplicatesAndNulls_KeepsOnlyDistinctCards()
        {
            var card = CreateSushi();

            var deck = new SushiDeck(new[] { card, null, card });

            Assert.AreEqual(1, deck.Count);
            Assert.AreSame(card, deck.Cards[0]);
        }

        private SushiData CreateSushi()
        {
            return StageConfigBuilder.CreateSushi(_disposables);
        }
    }
}
