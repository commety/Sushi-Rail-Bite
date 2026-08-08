using NUnit.Framework;
using SushiDefense.Customers;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// <see cref="SaturationGauge"/> 는 포화도를 <b>몇 칸으로 그릴지</b>만 답한다.
    ///
    /// <para>
    /// 칸은 프리팹에 미리 놓이므로 개수에 상한이 있고, <c>MaxSaturation</c> 은 그 상한을
    /// 넘을 수 있다 (Placeholder 애셋이 99 다). 분기 없이 한 식으로 덮되,
    /// <b>정상 구간(3·5·8)에는 근사가 끼지 않아야</b> 한다 — 한 입 먹었는데 칸이 안 차거나
    /// 두 칸이 차면 화면이 거짓말을 한다.
    /// </para>
    /// </summary>
    public sealed class SaturationGaugeTests
    {
        private const int Capacity = 8;

        [Test]
        public void VisibleCells_MaxWithinCapacity_EqualsMax()
        {
            Assert.AreEqual(5, SaturationGauge.VisibleCells(5, Capacity));
            Assert.AreEqual(3, SaturationGauge.VisibleCells(3, Capacity));
        }

        [Test]
        public void VisibleCells_MaxEqualsCapacity_EqualsCapacity()
        {
            Assert.AreEqual(Capacity, SaturationGauge.VisibleCells(Capacity, Capacity));
        }

        [Test]
        public void VisibleCells_MaxAboveCapacity_EqualsCapacity()
        {
            Assert.AreEqual(Capacity, SaturationGauge.VisibleCells(99, Capacity));
        }

        /// <summary>
        /// 그리는 쪽이 예외로 죽으면 안 된다 — 표시는 로직의 전제 조건이 아니다.
        /// <c>StageClock</c> 이 제한 시간 0 을 예외로 두는 것과 다른 판단이며, 이유는
        /// 여기가 판정이 아니라 표시이기 때문이다.
        /// </summary>
        [Test]
        public void VisibleCells_MaxZeroOrNegative_ReturnsZero()
        {
            Assert.AreEqual(0, SaturationGauge.VisibleCells(0, Capacity));
            Assert.AreEqual(0, SaturationGauge.VisibleCells(-4, Capacity));
        }

        [Test]
        public void VisibleCells_NoCells_ReturnsZero()
        {
            Assert.AreEqual(0, SaturationGauge.VisibleCells(5, 0));
            Assert.AreEqual(0, SaturationGauge.VisibleCells(5, -2));
        }

        [Test]
        public void FilledCells_NoneEaten_ReturnsZero()
        {
            Assert.AreEqual(0, SaturationGauge.FilledCells(0, 5, Capacity));
        }

        /// <summary>
        /// 상한 안에서는 <b>칸 수가 곧 포화도</b>다. 반례를 같은 테스트에 함께 박아,
        /// 인자를 그대로 돌려주는 구현과 늘 비례로 접는 구현을 동시에 배제한다.
        /// </summary>
        [Test]
        public void FilledCells_MaxWithinCapacity_EqualsCurrent()
        {
            Assert.AreEqual(3, SaturationGauge.FilledCells(3, 5, Capacity));
            Assert.AreEqual(1, SaturationGauge.FilledCells(1, 5, Capacity));
            Assert.AreEqual(7, SaturationGauge.FilledCells(7, Capacity, Capacity));

            // 반례: 상한을 넘으면 같은 current 라도 접힌다.
            Assert.AreEqual(1, SaturationGauge.FilledCells(3, 99, Capacity));
        }

        /// <summary>
        /// 한 입이라도 먹었으면 최소 한 칸은 찬다. 내림으로 구현하면 99 짜리 손님이
        /// 열두 개를 먹을 때까지 칸이 하나도 안 찬다.
        /// </summary>
        [Test]
        public void FilledCells_OneEatenOfHugeMax_ReturnsOne()
        {
            Assert.AreEqual(1, SaturationGauge.FilledCells(1, 99, Capacity));
        }

        [Test]
        public void FilledCells_Full_EqualsVisibleCells()
        {
            Assert.AreEqual(5, SaturationGauge.FilledCells(5, 5, Capacity));
            Assert.AreEqual(Capacity, SaturationGauge.FilledCells(99, 99, Capacity));
        }

        /// <summary>
        /// 포화도가 상한을 넘는 상태는 나오지 않아야 하지만, 나와도 칸을 넘어 그리지 않는다.
        /// </summary>
        [Test]
        public void FilledCells_CurrentAboveMax_DoesNotExceedVisible()
        {
            Assert.AreEqual(5, SaturationGauge.FilledCells(9, 5, Capacity));
        }

        [Test]
        public void FilledCells_MaxZero_ReturnsZero()
        {
            Assert.AreEqual(0, SaturationGauge.FilledCells(3, 0, Capacity));
        }

        [Test]
        public void FilledCells_NegativeCurrent_ReturnsZero()
        {
            Assert.AreEqual(0, SaturationGauge.FilledCells(-2, 5, Capacity));
        }

        /// <summary>
        /// 소식(3) · 기본(5) · 먹보(8) 는 전부 상한 안이다. 세 유형 모두에서 한 입이
        /// 한 칸이어야 한다 — 유형마다 다르게 보이면 포화도를 비교할 수 없다.
        /// </summary>
        [Test]
        public void FilledCells_EveryShippedCustomer_IsOneCellPerPoint()
        {
            foreach (var max in new[] { 3, 5, 8 })
            {
                for (var current = 0; current <= max; current++)
                {
                    Assert.AreEqual(current, SaturationGauge.FilledCells(current, max, Capacity),
                                    $"max {max}, current {current}");
                }
            }
        }
    }
}
