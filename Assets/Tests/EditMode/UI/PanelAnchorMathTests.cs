using NUnit.Framework;
using SushiDefense.UI;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 떠 있는 창을 부모 사각형 안으로 미는 계산. <b>변환은 여기 없다</b> — 월드→화면→로컬은
    /// Unity 가 하고, 여기서는 «주어진 사각형을 주어진 경계 안으로» 만 푼다.
    ///
    /// <para>
    /// <b>정사각형도, 원점 대칭 경계도 쓰지 않는다.</b> 크기를 <c>200×280</c> 으로, 경계를
    /// 원점에서 치우치게 잡아야 <c>x</c>/<c>y</c> 를 바꿔 쓰거나 <c>min</c>/<c>max</c> 를
    /// 뒤집은 구현이 걸린다 (<c>.claude/rules/tests.md</c> §3).
    /// </para>
    /// </summary>
    public sealed class PanelAnchorMathTests
    {
        /// <summary>일부러 원점 대칭이 아니다 — x ∈ [-400, 560], y ∈ [-200, 340].</summary>
        private static readonly Rect Bounds = new(-400f, -200f, 960f, 540f);

        /// <summary>가로세로가 다르다. 정사각형이면 두 축을 뒤바꾼 구현도 통과한다.</summary>
        private static readonly Vector2 Size = new(200f, 280f);

        /// <summary>손님 위로 자라는 창이라 아래변이 기준점이다.</summary>
        private static readonly Vector2 BottomCenter = new(0.5f, 0f);

        /// <summary>
        /// 안에 들어 있으면 그대로 둔다.
        ///
        /// <para>
        /// <b>반례를 같은 테스트에 함께 박는다.</b> «그대로 돌려준다» 만 보면 인자를 그냥
        /// 반환하는 구현이 통과한다.
        /// </para>
        /// </summary>
        [Test]
        public void ClampInside_AlreadyInside_ReturnsUnchanged()
        {
            var inside = PanelAnchorMath.ClampInside(Vector2.zero, Size, BottomCenter, Bounds, 0f);
            var outside = PanelAnchorMath.ClampInside(new Vector2(520f, 0f), Size, BottomCenter,
                                                     Bounds, 0f);

            Assert.AreEqual(Vector2.zero, inside);
            Assert.AreEqual(new Vector2(460f, 0f), outside, "밖에 있는 것까지 그대로 돌려준다");
        }

        /// <summary>
        /// 오른쪽으로 넘치면 <b>왼쪽으로만</b> 민다. 두 축을 함께 건드리는 구현은 여기서 걸린다.
        /// </summary>
        [Test]
        public void ClampInside_OverflowsRight_ShiftsLeftOnly()
        {
            // 오른쪽 끝이 620 이라 경계 560 을 60 넘는다. 위쪽은 280 이라 340 안이다.
            var clamped = PanelAnchorMath.ClampInside(new Vector2(520f, 0f), Size, BottomCenter,
                                                      Bounds, 0f);

            Assert.AreEqual(460f, clamped.x, 0.001f);
            Assert.AreEqual(0f, clamped.y, 0.001f, "y 는 넘치지 않았는데 움직였다");
        }

        [Test]
        public void ClampInside_OverflowsLeft_ShiftsRightOnly()
        {
            var clamped = PanelAnchorMath.ClampInside(new Vector2(-450f, 0f), Size, BottomCenter,
                                                      Bounds, 0f);

            Assert.AreEqual(-300f, clamped.x, 0.001f);
            Assert.AreEqual(0f, clamped.y, 0.001f);
        }

        [Test]
        public void ClampInside_OverflowsTop_ShiftsDownOnly()
        {
            var clamped = PanelAnchorMath.ClampInside(new Vector2(0f, 100f), Size, BottomCenter,
                                                      Bounds, 0f);

            Assert.AreEqual(0f, clamped.x, 0.001f);
            Assert.AreEqual(60f, clamped.y, 0.001f);
        }

        [Test]
        public void ClampInside_OverflowsBottom_ShiftsUpOnly()
        {
            var clamped = PanelAnchorMath.ClampInside(new Vector2(0f, -250f), Size, BottomCenter,
                                                      Bounds, 0f);

            Assert.AreEqual(0f, clamped.x, 0.001f);
            Assert.AreEqual(-200f, clamped.y, 0.001f);
        }

        [Test]
        public void ClampInside_OverflowsTwoAxes_ShiftsBoth()
        {
            var clamped = PanelAnchorMath.ClampInside(new Vector2(520f, 100f), Size, BottomCenter,
                                                      Bounds, 0f);

            Assert.AreEqual(new Vector2(460f, 60f), clamped);
        }

        /// <summary>
        /// 여백이 결과에 실제로 들어간다.
        ///
        /// <para>
        /// <b>여백이 0 이 아닌 경계를 고른다.</b> 안쪽에 있는 입력으로 재면 여백을 무시하는
        /// 구현과 결과가 같아진다.
        /// </para>
        /// </summary>
        [Test]
        public void ClampInside_WithMargin_KeepsThatGap()
        {
            var tight = PanelAnchorMath.ClampInside(new Vector2(520f, 0f), Size, BottomCenter,
                                                    Bounds, 0f);
            var spaced = PanelAnchorMath.ClampInside(new Vector2(520f, 0f), Size, BottomCenter,
                                                     Bounds, 8f);

            Assert.AreEqual(460f, tight.x, 0.001f);
            Assert.AreEqual(452f, spaced.x, 0.001f, "여백만큼 더 밀려야 한다");
        }

        /// <summary>
        /// 피벗이 결과를 가른다.
        ///
        /// <para>
        /// <b>나머지를 전부 같게 두고 피벗만 바꾼다.</b> 그래야 피벗을 아예 안 보는 구현이
        /// 걸린다 — 피벗을 <c>(0.5, 0.5)</c> 하나로만 재면 그 구현도 통과한다.
        /// </para>
        /// </summary>
        [Test]
        public void ClampInside_DifferentPivot_LandsAtDifferentPosition()
        {
            var fromBottom = PanelAnchorMath.ClampInside(new Vector2(0f, -250f), Size, BottomCenter,
                                                         Bounds, 0f);
            var fromTop = PanelAnchorMath.ClampInside(new Vector2(0f, -250f), Size,
                                                      new Vector2(0.5f, 1f), Bounds, 0f);

            Assert.AreEqual(-200f, fromBottom.y, 0.001f);
            Assert.AreEqual(80f, fromTop.y, 0.001f);
        }

        /// <summary>
        /// 경계보다 큰 창은 <b>들어갈 자리가 없다.</b> 예외를 던지지 않고 결정적으로 한쪽
        /// 모서리에 붙인다 — 어느 쪽이든 잘리지만, 매번 다른 쪽으로 붙으면 원인을 못 찾는다.
        /// </summary>
        [Test]
        public void ClampInside_LargerThanBounds_PinsToMinimumCorner()
        {
            var clamped = PanelAnchorMath.ClampInside(new Vector2(500f, 500f), Size, BottomCenter,
                                                      new Rect(0f, 0f, 100f, 100f), 0f);

            Assert.AreEqual(new Vector2(100f, 0f), clamped);
        }
    }
}
