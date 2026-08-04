using NUnit.Framework;
using SushiDefense.Customers;

namespace SushiDefense.Tests.EditMode.Customers
{
    public sealed class ReachWindowTests
    {
        private const float ReachMin = 40f;
        private const float ReachMax = 60f;
        private const float Speed = 10f;

        [Test]
        public void Solve_SushiAlreadyInside_EnterSecondsIsZero()
        {
            var window = ReachWindow.Solve(50f, Speed, ReachMin, ReachMax);

            Assert.IsTrue(window.WillEverEnter);
            Assert.AreEqual(0f, window.EnterSeconds, 0.0001f);
        }

        [Test]
        public void Solve_SushiAlreadyInside_ExitSecondsCountsToFarEdge()
        {
            var window = ReachWindow.Solve(50f, Speed, ReachMin, ReachMax);

            Assert.AreEqual((ReachMax - 50f) / Speed, window.ExitSeconds, 0.0001f);
        }

        [Test]
        public void Solve_SushiApproaching_ReturnsPositiveEnterSeconds()
        {
            var window = ReachWindow.Solve(20f, Speed, ReachMin, ReachMax);

            Assert.IsTrue(window.WillEverEnter);
            Assert.AreEqual((ReachMin - 20f) / Speed, window.EnterSeconds, 0.0001f);
            Assert.AreEqual((ReachMax - 20f) / Speed, window.ExitSeconds, 0.0001f);
        }

        [Test]
        public void Solve_SushiAtNearEdge_EnterSecondsIsZero()
        {
            // 범위는 폐구간이다 (CustomerLogic.IsInReach 와 같은 규약).
            var window = ReachWindow.Solve(ReachMin, Speed, ReachMin, ReachMax);

            Assert.IsTrue(window.WillEverEnter);
            Assert.AreEqual(0f, window.EnterSeconds, 0.0001f);
        }

        [Test]
        public void Solve_SushiAlreadyPassed_WillEverEnterIsFalse()
        {
            var window = ReachWindow.Solve(ReachMax + 0.01f, Speed, ReachMin, ReachMax);

            Assert.IsFalse(window.WillEverEnter);
        }

        [Test]
        public void Solve_ZeroBeltSpeed_WillEverEnterIsFalse()
        {
            // 0 나눗셈 방어. 멈춘 벨트에서는 진입 이벤트가 발생하지 않는다.
            var window = ReachWindow.Solve(20f, 0f, ReachMin, ReachMax);

            Assert.IsFalse(window.WillEverEnter);
        }

        [Test]
        public void Solve_NegativeBeltSpeed_WillEverEnterIsFalse()
        {
            var window = ReachWindow.Solve(20f, -5f, ReachMin, ReachMax);

            Assert.IsFalse(window.WillEverEnter);
        }

        [Test]
        public void Solve_SushiAtFarEdge_ExitSecondsIsZero()
        {
            // 먼 쪽 경계는 "지금 막 나가는 중" 이다. 여기서만 진입 시각과 이탈 시각이 같다.
            var window = ReachWindow.Solve(ReachMax, Speed, ReachMin, ReachMax);

            Assert.IsTrue(window.WillEverEnter);
            Assert.AreEqual(0f, window.EnterSeconds, 0.0001f);
            Assert.AreEqual(0f, window.ExitSeconds, 0.0001f);
        }

        [Test]
        public void Solve_ExitAfterEnter_WhileStillInside()
        {
            foreach (var position in new[] { 0f, 20f, ReachMin, 50f })
            {
                var window = ReachWindow.Solve(position, Speed, ReachMin, ReachMax);

                Assert.IsTrue(window.WillEverEnter, $"위치 {position} 는 진입 가능해야 한다");
                Assert.Greater(window.ExitSeconds, window.EnterSeconds,
                               $"위치 {position} 에서 이탈이 진입보다 빠르다");
            }
        }

        [Test]
        public void Solve_SameInputTwice_ProducesIdenticalWindow()
        {
            var left = ReachWindow.Solve(23f, Speed, ReachMin, ReachMax);
            var right = ReachWindow.Solve(23f, Speed, ReachMin, ReachMax);

            Assert.AreEqual(left.EnterSeconds, right.EnterSeconds);
            Assert.AreEqual(left.ExitSeconds, right.ExitSeconds);
            Assert.AreEqual(left.WillEverEnter, right.WillEverEnter);
        }
    }
}
