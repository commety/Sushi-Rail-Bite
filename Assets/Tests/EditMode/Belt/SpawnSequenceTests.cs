using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Belt
{
    /// <summary>
    /// <see cref="SpawnSequence"/> 는 "어떤 순서로" 만 답한다 — 비율은
    /// <c>SpawnShareTable</c> 이 이미 정했다.
    ///
    /// <para>
    /// 여기서 지킬 성질은 둘이고 서로 긴장 관계다: 비율이 share 에 정확히 비례할 것,
    /// 그리고 share 가 낮은 유형이 앞에 뭉치지 않을 것. credit 누적이 둘을 동시에 만족한다.
    /// </para>
    /// </summary>
    public sealed class SpawnSequenceTests
    {
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
        public void IsEmpty_NoEntries_ReturnsTrue()
        {
            var sequence = NewSequence();

            Assert.IsTrue(sequence.IsEmpty);
        }

        [Test]
        public void IsEmpty_OnlyEntriesWithoutSushi_ReturnsTrue()
        {
            var sequence = new SpawnSequence(new SushiData[] { null }, AlphaOne);

            Assert.IsTrue(sequence.IsEmpty);
        }

        [Test]
        public void Next_EmptyDeck_ReturnsNull()
        {
            var sequence = NewSequence();

            Assert.IsNull(sequence.Next());
        }

        [Test]
        public void Next_SingleEntry_AlwaysReturnsThatSushi()
        {
            var deck = BuildDeck(100);
            var sequence = new SpawnSequence(deck, AlphaOne);
            var only = deck[0];

            Assert.AreSame(only, sequence.Next());
            Assert.AreSame(only, sequence.Next());
            Assert.AreSame(only, sequence.Next());
        }

        [Test]
        public void Next_EntryWithoutSushi_IsSkipped()
        {
            var sushi = StageConfigBuilder.CreateSushi(_disposables, 100);

            var sequence = new SpawnSequence(new[] { null, sushi }, AlphaOne);

            Assert.AreSame(sushi, sequence.Next());
        }

        [Test]
        public void Emit_TwoToOneShares_AlternatesEvenly()
        {
            // 가격 100 / 200 / 200 → share 0.5 / 0.25 / 0.25.
            // credit 누적이 내는 순서는 A B C A | A B C A — 비율은 정확히 2:1:1 이고
            // A 가 앞에 몰리지 않는다.
            var deck = BuildDeck(100, 200, 200);
            var sequence = new SpawnSequence(deck, AlphaOne);
            var a = deck[0];
            var b = deck[1];
            var c = deck[2];

            var expected = new[] { a, b, c, a, a, b, c, a };

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreSame(expected[i], sequence.Next(), $"{i} 번째 배출이 어긋났습니다");
            }
        }

        [Test]
        public void Emit_LowShareType_NotClusteredInFirstWindow()
        {
            // 플랜의 예시 덱 — 장어 300 (share 8.7%) 은 24개 구간에 2번 나온다.
            // 앞 1/3 구간(8개)에 그 2번이 다 몰리면 "비싼 게 초반에 뭉치는" 그림이다.
            // 임계값 1 은 기대치(0.7회)의 바로 위 정수다.
            const int Window = 8;
            const int MaxInWindow = 1;

            var deck = BuildDeck(300, 150, 120, 100, 100);
            var sequence = new SpawnSequence(deck, AlphaOne);
            var rare = deck[0];

            var seen = 0;
            for (var i = 0; i < Window; i++)
            {
                if (ReferenceEquals(sequence.Next(), rare))
                {
                    seen++;
                }
            }

            Assert.That(seen, Is.LessThanOrEqualTo(MaxInWindow),
                        $"앞 {Window}개 중 희소 유형이 {seen}번 나왔습니다");
        }

        [Test]
        public void Emit_OverLongWindow_RatioMatchesShare()
        {
            const int Emissions = 1000;
            const float Tolerance = 0.01f;

            var deck = BuildDeck(300, 150, 120, 100, 100);
            var table = new SpawnShareTable(deck, AlphaOne);
            var sequence = new SpawnSequence(deck, AlphaOne);

            var counts = new int[table.Count];
            for (var i = 0; i < Emissions; i++)
            {
                var emitted = sequence.Next();
                for (var t = 0; t < table.Count; t++)
                {
                    if (ReferenceEquals(table.SushiAt(t), emitted))
                    {
                        counts[t]++;
                        break;
                    }
                }
            }

            for (var t = 0; t < table.Count; t++)
            {
                Assert.That((float)counts[t] / Emissions,
                            Is.EqualTo(table.ShareOf(t)).Within(Tolerance),
                            $"{t} 번째 유형의 등장 비율이 share 에서 벗어났습니다");
            }
        }

        [Test]
        public void Emit_TypeNotInDeck_NeverAppears()
        {
            var outsider = StageConfigBuilder.CreateSushi(_disposables, 100);
            var sequence = NewSequence(100, 200, 400);

            for (var i = 0; i < 50; i++)
            {
                Assert.AreNotSame(outsider, sequence.Next());
            }
        }

        [Test]
        public void Emit_SameDeckTwice_ProducesIdenticalSequence()
        {
            // 결정성 회귀 방지 — 배출 경로에 난수가 들어오면 이 테스트가 먼저 깨진다.
            var deck = BuildDeck(300, 150, 100);

            var left = new SpawnSequence(deck, AlphaOne);
            var right = new SpawnSequence(deck, AlphaOne);

            for (var i = 0; i < 30; i++)
            {
                Assert.AreSame(left.Next(), right.Next(), $"{i} 번째 배출이 어긋났습니다");
            }
        }

        [Test]
        public void Reset_AtStageStart_RestartsFromBeginning()
        {
            var sequence = NewSequence(100, 200, 200);

            var firstRun = new SushiData[6];
            for (var i = 0; i < firstRun.Length; i++)
            {
                firstRun[i] = sequence.Next();
            }

            sequence.Reset();

            for (var i = 0; i < firstRun.Length; i++)
            {
                Assert.AreSame(firstRun[i], sequence.Next(), $"리셋 후 {i} 번째가 어긋났습니다");
            }
        }

        private SpawnSequence NewSequence(params int[] prices)
        {
            return new SpawnSequence(BuildDeck(prices), AlphaOne);
        }

        /// <summary>
        /// 덱을 바로 만든다. 벨트가 <c>StageConfig</c> 대신 초밥 목록을 주입받게 되면서
        /// 스폰 테스트가 더 이상 스테이지 SO 를 세우지 않아도 된다.
        /// </summary>
        private IReadOnlyList<SushiData> BuildDeck(params int[] prices)
        {
            var deck = new List<SushiData>();
            foreach (var price in prices)
            {
                deck.Add(StageConfigBuilder.CreateSushi(_disposables, price));
            }

            return deck;
        }
    }
}
