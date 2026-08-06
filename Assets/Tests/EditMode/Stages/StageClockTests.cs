using System;
using NUnit.Framework;
using SushiDefense.Stages;

namespace SushiDefense.Tests.EditMode.Stages
{
    /// <summary>
    /// 시계는 시간만 안다. 클리어 여부를 묻지 않는다 — 그건 <c>StageEvaluator</c> 다.
    /// </summary>
    public sealed class StageClockTests
    {
        private const float Limit = 60f;
        private const float Tolerance = 0.0001f;

        private StageClock _clock;

        [SetUp]
        public void SetUp()
        {
            _clock = new StageClock(Limit);
        }

        [Test]
        public void ElapsedSeconds_NewClock_StartsAtZero()
        {
            Assert.AreEqual(0f, _clock.ElapsedSeconds, Tolerance);
            Assert.AreEqual(Limit, _clock.RemainingSeconds, Tolerance);
        }

        /// <summary>
        /// <b>구체값을 박는다.</b> "둘이 같다" 만 확인하면 상수를 돌려주는 구현도 통과한다
        /// (<c>.claude/rules/tests.md</c> §3).
        /// </summary>
        [Test]
        public void RemainingSeconds_Midway_ReportsRemainder()
        {
            _clock.Advance(2.5f);

            Assert.AreEqual(2.5f, _clock.ElapsedSeconds, Tolerance);
            Assert.AreEqual(57.5f, _clock.RemainingSeconds, Tolerance);
        }

        [Test]
        public void RemainingSeconds_BeyondLimit_ClampsToZero()
        {
            _clock.Advance(Limit + 10f);

            Assert.AreEqual(0f, _clock.RemainingSeconds, Tolerance);
        }

        /// <summary>
        /// 경과는 잘리지 않는다. 남은 시간만 0 에서 멈춘다 — 표시에 음수가 새면 안 되지만,
        /// 얼마나 넘겼는지는 진단에 쓸 수 있어야 한다.
        /// </summary>
        [Test]
        public void ElapsedSeconds_BeyondLimit_KeepsGrowing()
        {
            _clock.Advance(Limit + 10f);

            Assert.AreEqual(Limit + 10f, _clock.ElapsedSeconds, Tolerance);
        }

        [Test]
        public void IsExpired_ExactlyAtLimit_ReturnsTrue()
        {
            _clock.Advance(Limit);

            Assert.IsTrue(_clock.IsExpired);
        }

        /// <summary>
        /// 이 테스트가 없으면 <c>IsExpired</c> 가 늘 <c>true</c> 인 구현이 통과한다.
        /// </summary>
        [Test]
        public void IsExpired_JustBeforeLimit_ReturnsFalse()
        {
            _clock.Advance(Limit - 0.01f);

            Assert.IsFalse(_clock.IsExpired);
        }

        [Test]
        public void IsExpired_NewClock_ReturnsFalse()
        {
            Assert.IsFalse(_clock.IsExpired);
        }

        /// <summary>
        /// 틱 길이가 달라져도 같은 시각에 같은 상태여야 한다. 프레임 레이트에 따라
        /// 판정이 흔들리면 WebGL 에서 재현 불가능한 차이가 생긴다.
        /// </summary>
        [Test]
        public void Advance_SplitIntoManySteps_MatchesOneBigStep()
        {
            var stepped = new StageClock(Limit);
            for (var i = 0; i < 100; i++)
            {
                stepped.Advance(0.1f);
            }

            _clock.Advance(10f);

            Assert.AreEqual(_clock.ElapsedSeconds, stepped.ElapsedSeconds, 0.001f);
            Assert.AreEqual(_clock.IsExpired, stepped.IsExpired);
        }

        [Test]
        public void Advance_Negative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _clock.Advance(-1f));
        }

        [Test]
        public void Advance_Zero_IsAllowed()
        {
            _clock.Advance(0f);

            Assert.AreEqual(0f, _clock.ElapsedSeconds, Tolerance);
        }

        [Test]
        public void Constructor_ZeroLimit_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StageClock(0f));
        }

        [Test]
        public void Constructor_NegativeLimit_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StageClock(-5f));
        }

        [Test]
        public void Reset_AfterExpiry_ClearsElapsed()
        {
            _clock.Advance(Limit + 5f);

            _clock.Reset();

            Assert.AreEqual(0f, _clock.ElapsedSeconds, Tolerance);
            Assert.AreEqual(Limit, _clock.RemainingSeconds, Tolerance);
            Assert.IsFalse(_clock.IsExpired);
        }

        /// <summary>제한 시간은 재시작해도 그대로다 — 리셋은 경과만 되돌린다.</summary>
        [Test]
        public void Reset_AfterExpiry_KeepsLimit()
        {
            _clock.Advance(Limit + 5f);

            _clock.Reset();

            Assert.AreEqual(Limit, _clock.LimitSeconds, Tolerance);
        }
    }
}
