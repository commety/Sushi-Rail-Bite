using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Belt
{
    public sealed class SpawnSequenceTests
    {
        private readonly List<Object> _disposables = new();
        private StageConfig _config;

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();

            if (_config != null)
            {
                Object.DestroyImmediate(_config);
                _config = null;
            }
        }

        [Test]
        public void IsEmpty_NoEntries_ReturnsTrue()
        {
            var sequence = new SpawnSequence(BuildTable());

            Assert.IsTrue(sequence.IsEmpty);
        }

        [Test]
        public void IsEmpty_OnlyEntriesWithoutSushi_ReturnsTrue()
        {
            var sequence = new SpawnSequence(BuildTable(null, null));

            Assert.IsTrue(sequence.IsEmpty);
        }

        [Test]
        public void Next_SingleEntry_AlwaysReturnsThatSushi()
        {
            var sushi = NewSushi();
            var sequence = new SpawnSequence(BuildTable(sushi));

            Assert.AreSame(sushi, sequence.Next());
            Assert.AreSame(sushi, sequence.Next());
            Assert.AreSame(sushi, sequence.Next());
        }

        [Test]
        public void Next_PastEnd_WrapsAround()
        {
            var first = NewSushi();
            var second = NewSushi();
            var sequence = new SpawnSequence(BuildTable(first, second));

            sequence.Next();
            sequence.Next();

            Assert.AreSame(first, sequence.Next());
        }

        [Test]
        public void Next_SameSequenceTwice_ProducesIdenticalOrder()
        {
            // 결정성 회귀 방지 — 스폰에 난수가 들어오면 이 테스트가 먼저 깨진다 (작업서 D4).
            var first = NewSushi();
            var second = NewSushi();
            var table = BuildTable(first, second);

            var left = new SpawnSequence(table);
            var right = new SpawnSequence(table);

            for (var i = 0; i < 6; i++)
            {
                Assert.AreSame(left.Next(), right.Next(), $"{i} 번째 스폰이 어긋났습니다");
            }
        }

        [Test]
        public void Reset_AfterConsuming_RestartsFromBeginning()
        {
            var first = NewSushi();
            var second = NewSushi();
            var sequence = new SpawnSequence(BuildTable(first, second));
            sequence.Next();

            sequence.Reset();

            Assert.AreSame(first, sequence.Next());
        }

        [Test]
        public void Next_EntryWithoutSushi_IsSkipped()
        {
            var sushi = NewSushi();
            var sequence = new SpawnSequence(BuildTable(null, sushi));

            Assert.AreSame(sushi, sequence.Next());
        }

        private SushiData NewSushi() => StageConfigBuilder.CreateSushi(_disposables);

        private IReadOnlyList<SushiSpawnEntry> BuildTable(params SushiData[] deck)
        {
            var builder = new StageConfigBuilder();
            foreach (var sushi in deck)
            {
                builder.WithSpawnEntry(sushi);
            }

            _config = builder.Build();
            return _config.SpawnTable;
        }
    }
}
