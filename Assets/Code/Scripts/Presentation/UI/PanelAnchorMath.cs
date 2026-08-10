using UnityEngine;

namespace SushiDefense.UI
{
    /// <summary>
    /// 떠 있는 창을 부모 사각형 안으로 <b>최소한만</b> 민다. 순수 계산이라 오브젝트 없이
    /// 검증된다 (<c>CLAUDE.md</c> §3.2).
    ///
    /// <para>
    /// <b>좌표 변환을 하지 않는다.</b> 월드 → 화면 → 부모 로컬은 Unity 가 하고, 여기서는
    /// 이미 로컬 좌표가 된 사각형만 다룬다. 변환까지 넣으면 <c>Camera</c> 와
    /// <c>RectTransform</c> 이 필요해져 경계값을 넣어 보기가 어려워진다.
    /// </para>
    /// <para>
    /// <b>화면 밖이면 «뒤집지» 않고 «민다».</b> 뒤집기는 «어느 쪽으로» 라는 두 번째 규칙이
    /// 필요하고 그 규칙이 자리마다 다르게 보인다. 밀어 넣기는 규칙이 하나(경계 안)라 자리
    /// 수가 늘어도 그대로 성립한다.
    /// </para>
    /// </summary>
    public static class PanelAnchorMath
    {
        /// <summary>
        /// 창이 경계 안에 들어오도록 위치를 옮긴다. 이미 안에 있으면 그대로 돌려준다.
        /// </summary>
        /// <param name="anchoredPosition">부모 로컬 좌표에서의 위치. 피벗이 놓이는 점이다.</param>
        /// <param name="size">창의 크기.</param>
        /// <param name="pivot">창의 피벗 (0~1). 어느 점이 <paramref name="anchoredPosition"/> 에 놓이는지 정한다.</param>
        /// <param name="bounds">
        /// 부모의 <c>RectTransform.rect</c>. <b>이미 피벗이 반영된 로컬 사각형</b>이라
        /// 원점 대칭이 아닐 수 있다.
        /// </param>
        /// <param name="margin">경계에서 띄울 여백.</param>
        public static Vector2 ClampInside(Vector2 anchoredPosition, Vector2 size, Vector2 pivot,
                                          Rect bounds, float margin)
        {
            var min = anchoredPosition - Vector2.Scale(pivot, size);

            return new Vector2(
                anchoredPosition.x + Shift(min.x, size.x, bounds.xMin + margin, bounds.xMax - margin),
                anchoredPosition.y + Shift(min.y, size.y, bounds.yMin + margin, bounds.yMax - margin));
        }

        /// <summary>
        /// 한 축을 경계 안으로 밀 거리. <b>두 축이 서로를 모른다</b> — 한쪽만 넘쳤는데 양쪽이
        /// 움직이면 창이 엉뚱한 데로 간다.
        ///
        /// <para>
        /// 들어갈 자리 자체가 없으면 <b>아래(왼쪽) 모서리에 붙인다.</b> 어느 쪽이든 잘리지만,
        /// 입력에 따라 붙는 쪽이 달라지면 «가끔 위가 잘리고 가끔 아래가 잘린다» 가 되어
        /// 원인을 찾기 어렵다.
        /// </para>
        /// </summary>
        private static float Shift(float min, float length, float low, float high)
        {
            if (length >= high - low || min < low)
            {
                return low - min;
            }

            var max = min + length;
            return max > high ? high - max : 0f;
        }
    }
}
