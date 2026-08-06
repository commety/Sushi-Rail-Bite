using System;
using NUnit.Framework;
using SushiDefense.Run;

namespace SushiDefense.Tests.EditMode.Run
{
    /// <summary>
    /// 이 프로젝트에서 <b>처음이자 유일하게 정당한 난수</b>다 (보상 추첨). 배정에는 여전히
    /// 난수가 없다 — 순차번호가 모든 동률을 끝낸다 (<c>CLAUDE.md</c> §1.1-3b).
    ///
    /// <para>
    /// 주입해서 쓰는 이유가 여기 있다: 시드가 같으면 수열이 같으므로 보상 테스트가
    /// 통계 검증이 아니라 <b>정확한 값 비교</b>가 된다.
    /// </para>
    /// </summary>
    public sealed class XorShiftRandomSourceTests
    {
        private const int Seed = 12345;

        [Test]
        public void Next_SameSeed_ProducesIdenticalSequence()
        {
            var left = new XorShiftRandomSource(Seed);
            var right = new XorShiftRandomSource(Seed);

            for (var i = 0; i < 20; i++)
            {
                Assert.AreEqual(left.Next(100), right.Next(100), $"{i} 번째가 어긋났습니다");
            }
        }

        /// <summary>
        /// 앞 한 값만 비교하면 우연히 같아 깜빡이는 테스트가 된다. 수열을 통째로 본다.
        /// </summary>
        [Test]
        public void Next_DifferentSeeds_ProduceDifferentSequences()
        {
            var left = new XorShiftRandomSource(Seed);
            var right = new XorShiftRandomSource(Seed + 1);

            var differs = false;
            for (var i = 0; i < 20; i++)
            {
                if (left.Next(1000) != right.Next(1000))
                {
                    differs = true;
                }
            }

            Assert.IsTrue(differs, "다른 시드가 같은 수열을 냈습니다");
        }

        /// <summary>
        /// 시드 0 은 xorshift 의 고정점이라 방치하면 영원히 같은 값만 나온다.
        /// </summary>
        [Test]
        public void Next_ZeroSeed_StillProducesVaryingValues()
        {
            var random = new XorShiftRandomSource(0);

            var first = random.Next(1000);
            var varied = false;
            for (var i = 0; i < 20; i++)
            {
                if (random.Next(1000) != first)
                {
                    varied = true;
                }
            }

            Assert.IsTrue(varied, "시드 0 에서 같은 값만 나왔습니다 — 고정점에 갇혔습니다");
        }

        [Test]
        public void Next_ExclusiveMaxOne_AlwaysReturnsZero()
        {
            var random = new XorShiftRandomSource(Seed);

            for (var i = 0; i < 20; i++)
            {
                Assert.AreEqual(0, random.Next(1));
            }
        }

        [Test]
        public void Next_ManyDraws_StaysWithinRange()
        {
            const int ExclusiveMax = 7;
            var random = new XorShiftRandomSource(Seed);

            for (var i = 0; i < 500; i++)
            {
                var value = random.Next(ExclusiveMax);
                Assert.That(value, Is.InRange(0, ExclusiveMax - 1), $"{i} 번째 뽑기가 범위를 벗어났습니다");
            }
        }

        /// <summary>
        /// 범위 전체를 쓰는지 본다. 늘 0 을 돌려주는 구현도 위 범위 테스트는 통과한다.
        /// </summary>
        [Test]
        public void Next_ManyDraws_CoversEveryValue()
        {
            const int ExclusiveMax = 4;
            var random = new XorShiftRandomSource(Seed);
            var seen = new bool[ExclusiveMax];

            for (var i = 0; i < 500; i++)
            {
                seen[random.Next(ExclusiveMax)] = true;
            }

            for (var i = 0; i < ExclusiveMax; i++)
            {
                Assert.IsTrue(seen[i], $"{i} 이 한 번도 나오지 않았습니다");
            }
        }

        [Test]
        public void Next_ExclusiveMaxZero_Throws()
        {
            var random = new XorShiftRandomSource(Seed);

            Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(0));
        }

        [Test]
        public void Next_ExclusiveMaxNegative_Throws()
        {
            var random = new XorShiftRandomSource(Seed);

            Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(-5));
        }
    }
}
