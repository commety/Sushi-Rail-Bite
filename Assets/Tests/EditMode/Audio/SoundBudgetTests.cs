using System;
using NUnit.Framework;
using SushiDefense.Audio;

namespace SushiDefense.Tests.EditMode.Audio
{
    public sealed class SoundBudgetTests
    {
        private const int EatenCue = 0;
        private const int PlacedCue = 1;

        /// <summary>상한을 보는 테스트에서 쿨다운이 대신 막지 않게 하는 값.</summary>
        private const float NoCooldown = 0f;

        /// <summary>쿨다운을 보는 테스트에서 상한이 대신 막지 않게 하는 값.</summary>
        private const int RoomyLimit = 8;

        private const float Duration = 0.1f;

        [Test]
        public void Constructor_MaxConcurrentZero_Throws()
        {
            // 0 이면 소리가 하나도 나지 않는다. 조용히 받아들이면 "왜 무음이지" 를
            // 재생 쪽에서 찾게 된다.
            Assert.Throws<ArgumentOutOfRangeException>(() => new SoundBudget(0));
        }

        [Test]
        public void TryPlay_SameCueWithinCooldown_ReturnsFalse()
        {
            var budget = new SoundBudget(RoomyLimit);
            budget.TryPlay(EatenCue, 0f, 1f, Duration);

            var second = budget.TryPlay(EatenCue, 0.5f, 1f, Duration);

            Assert.IsFalse(second);
        }

        [Test]
        public void TryPlay_SameCueAfterCooldown_ReturnsTrue()
        {
            var budget = new SoundBudget(RoomyLimit);
            budget.TryPlay(EatenCue, 0f, 1f, Duration);

            var second = budget.TryPlay(EatenCue, 1.5f, 1f, Duration);

            Assert.IsTrue(second);
        }

        [Test]
        public void TryPlay_ExactlyAtCooldownExpiry_ReturnsTrue()
        {
            var budget = new SoundBudget(RoomyLimit);
            budget.TryPlay(EatenCue, 0f, 1f, Duration);

            var second = budget.TryPlay(EatenCue, 1f, 1f, Duration);

            Assert.IsTrue(second);
        }

        [Test]
        public void TryPlay_DifferentCueWithinCooldown_ReturnsTrue()
        {
            // 쿨다운은 큐별이다. 전역 하나로 두면 먹힘 소리가 배치 소리까지 막는다.
            var budget = new SoundBudget(RoomyLimit);
            budget.TryPlay(EatenCue, 0f, 1f, Duration);

            var other = budget.TryPlay(PlacedCue, 0.5f, 1f, Duration);

            Assert.IsTrue(other);
        }

        [Test]
        public void TryPlay_ZeroCooldown_AllowsAgainImmediately()
        {
            var budget = new SoundBudget(RoomyLimit);
            budget.TryPlay(EatenCue, 0f, NoCooldown, Duration);

            var second = budget.TryPlay(EatenCue, 0f, NoCooldown, Duration);

            Assert.IsTrue(second);
        }

        [Test]
        public void TryPlay_AtConcurrentLimit_ReturnsFalse()
        {
            var budget = new SoundBudget(2);
            budget.TryPlay(EatenCue, 0f, NoCooldown, Duration);
            budget.TryPlay(EatenCue, 0f, NoCooldown, Duration);

            var third = budget.TryPlay(EatenCue, 0f, NoCooldown, Duration);

            Assert.IsFalse(third);
        }

        [Test]
        public void TryPlay_AfterOldestExpired_ReturnsTrue()
        {
            var budget = new SoundBudget(2);
            budget.TryPlay(EatenCue, 0f, NoCooldown, Duration);
            budget.TryPlay(EatenCue, 0f, NoCooldown, Duration);

            var afterExpiry = budget.TryPlay(EatenCue, Duration + 0.01f, NoCooldown, Duration);

            Assert.IsTrue(afterExpiry);
        }

        [Test]
        public void TryPlay_MaxConcurrentOne_AllowsOneAtATime()
        {
            var budget = new SoundBudget(1);

            Assert.IsTrue(budget.TryPlay(EatenCue, 0f, NoCooldown, Duration));
            Assert.IsFalse(budget.TryPlay(EatenCue, 0.05f, NoCooldown, Duration));
            Assert.IsTrue(budget.TryPlay(EatenCue, 0.2f, NoCooldown, Duration));
        }

        [Test]
        public void ActiveCount_WhileSoundsPlaying_CountsThem()
        {
            // 구체값을 박는다. "둘이 같다" 만 보면 상수를 돌려주는 구현도 통과한다.
            var budget = new SoundBudget(RoomyLimit);
            budget.TryPlay(EatenCue, 0f, NoCooldown, Duration);
            budget.TryPlay(PlacedCue, 0f, NoCooldown, Duration);

            Assert.AreEqual(2, budget.ActiveCount(0.05f));
        }

        [Test]
        public void ActiveCount_AfterAllExpired_IsZero()
        {
            var budget = new SoundBudget(RoomyLimit);
            budget.TryPlay(EatenCue, 0f, NoCooldown, Duration);
            budget.TryPlay(PlacedCue, 0f, NoCooldown, Duration);

            Assert.AreEqual(0, budget.ActiveCount(Duration + 0.01f));
        }

        [Test]
        public void ActiveCount_FreshBudget_IsZero()
        {
            Assert.AreEqual(0, new SoundBudget(RoomyLimit).ActiveCount(0f));
        }

        [Test]
        public void Reset_AfterLimitReached_AllowsImmediately()
        {
            var budget = new SoundBudget(1);
            budget.TryPlay(EatenCue, 0f, 10f, Duration);

            budget.Reset();

            Assert.IsTrue(budget.TryPlay(EatenCue, 0f, 10f, Duration),
                          "재시작 후에도 직전 판의 쿨다운·슬롯이 남아 있으면 안 된다");
        }
    }
}
