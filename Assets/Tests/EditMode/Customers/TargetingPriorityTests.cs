using NUnit.Framework;
using SushiDefense.Customers;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// <see cref="TargetingPriority"/> 는 배정에서만 쓰인다 — <b>자격 판정에는 등장하지 않는다</b>
    /// (<c>CLAUDE.md</c> §1.1-3a).
    ///
    /// <para>
    /// 계산 자체는 몇 줄이라 검증할 것이 적다. 그럼에도 별도 타입인 이유는 대역 산술이 사는
    /// 곳을 셀 수 있게 만들기 위해서다 — 비교자 안에 인라인하면, 다음 사람이 같은 식을
    /// 자격 판정 쪽에 복사해 넣어도 grep 이 잡지 못한다.
    /// </para>
    /// </summary>
    public sealed class TargetingPriorityTests
    {
        /// <summary>기본(100~300)에 해당하는 폭 있는 대역. 대부분의 케이스가 이걸 쓴다.</summary>
        private const int Min = 100;

        private const int Max = 300;

        [Test]
        public void BandDistance_PriceInsideBand_ReturnsZero()
        {
            Assert.AreEqual(0, TargetingPriority.BandDistance(200, Min, Max));
        }

        [Test]
        public void BandDistance_PriceAtBandEdges_ReturnsZero()
        {
            // 폐구간이다. 경계에 딱 걸친 가격을 대역 밖으로 보면 정확히 그 가격의
            // 초밥에서만 손님이 갑자기 기다리기 시작한다.
            Assert.AreEqual(0, TargetingPriority.BandDistance(Min, Min, Max));
            Assert.AreEqual(0, TargetingPriority.BandDistance(Max, Min, Max));
        }

        [Test]
        public void BandDistance_PriceBelowBand_ReturnsGapToMin()
        {
            Assert.AreEqual(40, TargetingPriority.BandDistance(60, Min, Max));
        }

        [Test]
        public void BandDistance_PriceAboveBand_ReturnsGapToMax()
        {
            // 상한 쪽 거리를 min 기준으로 재면 여기가 깨진다 — 350 은 min 에서 250 이다.
            Assert.AreEqual(50, TargetingPriority.BandDistance(350, Min, Max));
        }

        [Test]
        public void BandDistance_TwoPricesInsideBand_AreEqual()
        {
            // 대역 안은 전부 동점이다. 205 와 250 을 구분하지 않는 것은 정보를 버리는
            // 것이 아니라 분업이다 — 그 동점은 정렬 키 2(고가 우선)가 깬다.
            var cheap = TargetingPriority.BandDistance(105, Min, Max);
            var expensive = TargetingPriority.BandDistance(295, Min, Max);

            Assert.AreEqual(cheap, expensive);

            // 같은지만 보면 상수 0 을 돌려주는 구현에서도 통과한다. 대역 밖이
            // 0 이 아님을 같은 테스트에서 함께 못박는다.
            Assert.AreNotEqual(0, TargetingPriority.BandDistance(500, Min, Max));
        }

        [Test]
        public void BandDistance_EqualGapOnBothSides_AreEqual()
        {
            // 대역 '밖' 에서는 여전히 대칭이다 — 대칭성이 사라진 것은 대역 안뿐이다.
            // 90 과 310 은 둘 다 거리 10 이고, 이 동점도 키 2 가 310 쪽으로 깬다.
            var below = TargetingPriority.BandDistance(Min - 10, Min, Max);
            var above = TargetingPriority.BandDistance(Max + 10, Min, Max);

            Assert.AreEqual(10, below);
            Assert.AreEqual(10, above);
        }

        [Test]
        public void BandDistance_FartherFromBand_IsLarger()
        {
            var near = TargetingPriority.BandDistance(Max + 20, Min, Max);
            var far = TargetingPriority.BandDistance(Max + 200, Min, Max);

            Assert.That(far, Is.GreaterThan(near));
        }

        [Test]
        public void BandDistance_ZeroWidthBand_BehavesLikeSinglePrice()
        {
            // 폭 0 은 M2 의 단일 값과 같다. 옛 동작이 대역의 특수 케이스로 흡수됐음을
            // 고정해 두면, 이 마일스톤이 규칙을 갈아엎은 것이 아니라 일반화한 것임이 남는다.
            Assert.AreEqual(0, TargetingPriority.BandDistance(200, 200, 200));
            Assert.AreEqual(10, TargetingPriority.BandDistance(210, 200, 200));
            Assert.AreEqual(10, TargetingPriority.BandDistance(190, 200, 200));
        }

        [Test]
        public void BandDistance_NeverNegative()
        {
            // 음수가 나오면 오름차순 정렬에서 "먼 쪽이 먼저" 로 뒤집힌다.
            Assert.That(TargetingPriority.BandDistance(0, Min, Max), Is.GreaterThanOrEqualTo(0));
            Assert.That(TargetingPriority.BandDistance(99999, Min, Max), Is.GreaterThanOrEqualTo(0));
            Assert.That(TargetingPriority.BandDistance(200, Min, Max), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void BandWidth_WideBand_IsGreaterThanNarrow()
        {
            var wide = TargetingPriority.BandWidth(100, 300);
            var narrow = TargetingPriority.BandWidth(280, 300);

            Assert.That(wide, Is.GreaterThan(narrow));
        }

        [Test]
        public void BandWidth_ZeroWidthBand_ReturnsZero()
        {
            // 폭 0 = 완전 전문가. 정렬 키 4에서 가장 강하다.
            Assert.AreEqual(0, TargetingPriority.BandWidth(250, 250));
        }

        [Test]
        public void BandWidth_SameSpanAtDifferentPrices_IsEqual()
        {
            // 폭은 대역의 '위치' 와 무관하다. 위치가 섞이면 저가대 전문가와 고가대
            // 전문가의 강도가 달라져, 밸런스가 아니라 산술이 유형을 갈라 버린다.
            Assert.AreEqual(TargetingPriority.BandWidth(100, 150),
                            TargetingPriority.BandWidth(500, 550));
        }
    }
}
