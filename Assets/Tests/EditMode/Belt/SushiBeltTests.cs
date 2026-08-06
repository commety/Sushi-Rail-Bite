using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Belt
{
    public sealed class SushiBeltTests
    {
        private const float Interval = 1f;
        private const float Speed = 10f;
        private const float Length = 100f;
        private const float ShortLength = 5f;
        private const float ShortBeltTravelSeconds = ShortLength / Speed;

        private readonly List<Object> _disposables = new();

        private StageConfig _config;
        private SushiPool<SushiItem> _pool;
        private SequenceNumberIssuer _sequenceNumbers;

        [SetUp]
        public void SetUp()
        {
            _config = new StageConfigBuilder()
                .WithBeltSpeed(Speed)
                .WithSpawnInterval(Interval)
                .WithBeltLength(Length)
                .WithSpawnEntry(StageConfigBuilder.CreateSushi(_disposables))
                .Build();

            _pool = new SushiPool<SushiItem>(new SushiItemFactory());
            _sequenceNumbers = new SequenceNumberIssuer();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Tick_BeforeInterval_SpawnsNothing()
        {
            var belt = NewBelt();

            belt.Tick(Interval * 0.5f);

            Assert.IsEmpty(belt.ActiveSushi);
        }

        [Test]
        public void Tick_AtInterval_SpawnsOne()
        {
            var belt = NewBelt();

            belt.Tick(Interval);

            Assert.AreEqual(1, belt.ActiveSushi.Count);
        }

        [Test]
        public void Tick_LongerThanTwoIntervals_SpawnsTwo()
        {
            // 남은 시간을 버리면 저사양 기기에서 초밥이 덜 나온다. 누적해야 한다.
            var belt = NewBelt();

            belt.Tick(Interval * 2.5f);

            Assert.AreEqual(2, belt.ActiveSushi.Count);
        }

        [Test]
        public void Tick_RepeatedSmallSteps_AccumulatesToSpawn()
        {
            var belt = NewBelt();

            for (var i = 0; i < 4; i++)
            {
                belt.Tick(Interval * 0.25f);
            }

            Assert.AreEqual(1, belt.ActiveSushi.Count);
        }

        [Test]
        public void Tick_Spawned_AssignsIncreasingSequenceNumbers()
        {
            var belt = NewBelt();

            belt.Tick(Interval);
            belt.Tick(Interval);

            Assert.AreEqual(0, belt.ActiveSushi[0].SequenceNumber);
            Assert.AreEqual(1, belt.ActiveSushi[1].SequenceNumber);
        }

        [Test]
        public void Tick_Spawned_RaisesSushiSpawned()
        {
            var belt = NewBelt();
            var spawned = new List<SushiItem>();
            belt.SushiSpawned += spawned.Add;

            belt.Tick(Interval);

            Assert.AreEqual(1, spawned.Count);
            Assert.AreSame(belt.ActiveSushi[0], spawned[0]);
        }

        [Test]
        public void Tick_Spawned_CarriesDataFromSpawnTable()
        {
            var belt = NewBelt();

            belt.Tick(Interval);

            Assert.IsNotNull(belt.ActiveSushi[0].Data);
        }

        [Test]
        public void Tick_Advances_MovesByBeltSpeedTimesDelta()
        {
            var belt = NewBelt();
            belt.Tick(Interval);
            var sushi = belt.ActiveSushi[0];
            var before = sushi.BeltPosition;

            belt.Tick(0.5f);

            Assert.AreEqual(before + Speed * 0.5f, sushi.BeltPosition, 0.0001f);
        }

        [Test]
        public void Tick_ReachedBeltEnd_RemovesFromActive()
        {
            var belt = NewShortBelt();
            belt.Tick(Interval);

            belt.Tick(ShortBeltTravelSeconds);

            Assert.IsEmpty(belt.ActiveSushi);
        }

        [Test]
        public void Tick_ReachedBeltEnd_ReturnsToPool()
        {
            var belt = NewShortBelt();
            belt.Tick(Interval);

            belt.Tick(ShortBeltTravelSeconds);

            Assert.AreEqual(1, _pool.CountInactive);
            Assert.AreEqual(0, _pool.CountActive);
        }

        [Test]
        public void Tick_ReachedBeltEnd_RaisesSushiRemoved()
        {
            var belt = NewShortBelt();
            var removed = new List<SushiItem>();
            belt.SushiRemoved += removed.Add;
            belt.Tick(Interval);

            belt.Tick(ShortBeltTravelSeconds);

            Assert.AreEqual(1, removed.Count);
        }

        [Test]
        public void Remove_ConsumedItem_ReturnsToPoolAndRaisesRemoved()
        {
            var belt = NewBelt();
            var removed = new List<SushiItem>();
            belt.SushiRemoved += removed.Add;
            belt.Tick(Interval);
            var sushi = belt.ActiveSushi[0];

            belt.Remove(sushi);

            Assert.IsEmpty(belt.ActiveSushi);
            Assert.AreEqual(1, _pool.CountInactive);
            Assert.AreEqual(1, removed.Count);
        }

        [Test]
        public void Remove_UnknownItem_Throws()
        {
            var belt = NewBelt();
            var stranger = new SushiItem(null, 99);

            Assert.Throws<System.ArgumentException>(() => belt.Remove(stranger));
        }

        [Test]
        public void Tick_ReusedFromPool_HasCleanState()
        {
            var belt = NewBelt();
            belt.Tick(Interval);
            var first = belt.ActiveSushi[0];
            first.TryClaim(7);
            belt.Remove(first);

            belt.Tick(Interval);
            var reused = belt.ActiveSushi[0];

            Assert.AreSame(first, reused, "풀에서 재사용돼야 한다");
            Assert.AreEqual(SushiState.OnBelt, reused.State);
            Assert.AreEqual(SushiItem.NoCustomer, reused.ClaimedByCustomerSequenceNumber);
            Assert.AreEqual(0f, reused.BeltPosition, 0.0001f);
            Assert.AreEqual(1, reused.SequenceNumber, "재사용해도 새 순차번호를 받는다");
        }

        [Test]
        public void Tick_EmptySpawnTable_SpawnsNothing()
        {
            Object.DestroyImmediate(_config);
            _config = new StageConfigBuilder()
                .WithBeltSpeed(Speed)
                .WithSpawnInterval(Interval)
                .WithBeltLength(Length)
                .Build();
            var belt = NewBelt();

            belt.Tick(Interval * 3f);

            Assert.IsEmpty(belt.ActiveSushi);
        }

        [Test]
        public void Tick_SameConfigTwice_ProducesIdenticalPositions()
        {
            var left = NewBelt();
            var right = new SushiBelt(_config, SushiDeck.FromSpawnTable(_config).Cards,
                                  new SequenceNumberIssuer(),
                                      new SushiPool<SushiItem>(new SushiItemFactory()));

            for (var i = 0; i < 5; i++)
            {
                left.Tick(0.4f);
                right.Tick(0.4f);
            }

            Assert.AreEqual(left.ActiveSushi.Count, right.ActiveSushi.Count);
            for (var i = 0; i < left.ActiveSushi.Count; i++)
            {
                Assert.AreEqual(left.ActiveSushi[i].BeltPosition,
                                right.ActiveSushi[i].BeltPosition, 0.0001f);
                Assert.AreEqual(left.ActiveSushi[i].SequenceNumber,
                                right.ActiveSushi[i].SequenceNumber);
            }
        }

        private SushiBelt NewBelt() =>
            new(_config, SushiDeck.FromSpawnTable(_config).Cards, _sequenceNumbers, _pool);

        /// <summary>
        /// 끝점 검증용 벨트. 통과 시간(<see cref="ShortBeltTravelSeconds"/>)이 스폰 간격보다
        /// 짧아야 "끝에 닿아 사라졌다" 를 새로 스폰된 초밥과 섞이지 않게 볼 수 있다.
        /// </summary>
        private SushiBelt NewShortBelt()
        {
            Object.DestroyImmediate(_config);
            _config = new StageConfigBuilder()
                .WithBeltSpeed(Speed)
                .WithSpawnInterval(Interval)
                .WithBeltLength(ShortLength)
                .WithSpawnEntry(StageConfigBuilder.CreateSushi(_disposables))
                .Build();

            return NewBelt();
        }
    }
}
