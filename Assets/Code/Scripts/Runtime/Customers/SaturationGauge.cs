using System;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 포화도를 <b>몇 칸으로 그릴지</b> 정한다. 계산만 하고 그리지 않는다.
    ///
    /// <para>
    /// 칸은 프리팹에 미리 놓이므로 개수에 상한이 있고 (프로덕션에서 오브젝트를 만드는
    /// 지점은 풀 하나로 유지한다 — <c>CLAUDE.md</c> §3.4), 최대 포화도는 그 상한을 넘을 수
    /// 있다. 두 경우를 <b>분기 없이 한 식으로</b> 덮되, 상한 안에서는 근사가 끼지 않는다 —
    /// 한 입이 정확히 한 칸이다.
    /// </para>
    /// <para>
    /// <b>초밥 한 개가 한 칸이 아니다.</b> 채우는 양은 초밥마다 다르므로(장어 3 · 성게 4)
    /// 칸은 <i>먹은 개수</i>가 아니라 <i>포화도</i>를 센다.
    /// </para>
    /// <para>
    /// <b>범위 밖 입력에 예외를 던지지 않는다.</b> 여기는 판정이 아니라 표시이며, 그리는
    /// 쪽이 예외로 죽으면 손님이 통째로 사라진다 — 표시는 로직의 전제 조건이 아니다.
    /// </para>
    /// </summary>
    public static class SaturationGauge
    {
        /// <summary>
        /// 실제로 켤 칸 수. 최대 포화도가 상한보다 작으면 남는 칸은 그리지 않는다 —
        /// 회색으로 남겨 두면 "아직 못 채운 칸" 으로 읽힌다.
        /// </summary>
        public static int VisibleCells(int maxSaturation, int cellCapacity)
        {
            if (maxSaturation <= 0 || cellCapacity <= 0)
            {
                return 0;
            }

            return Math.Min(maxSaturation, cellCapacity);
        }

        /// <summary>
        /// 채워진 칸 수.
        ///
        /// <para>
        /// <b>올림이다.</b> 내림으로 하면 최대 포화도가 상한보다 훨씬 클 때 여러 개를 먹어도
        /// 칸이 하나도 안 차서, 먹은 것이 화면에 없는 상태가 된다.
        /// </para>
        /// <para>
        /// <c>maxSaturation ≤ cellCapacity</c> 이면 이 식은 <paramref name="currentSaturation"/>
        /// 과 정확히 같아진다. 출시된 손님 셋(3 · 5 · 8)이 전부 그 구간이므로 실제 플레이에
        /// 근사가 등장하지 않는다.
        /// </para>
        /// </summary>
        public static int FilledCells(int currentSaturation, int maxSaturation, int cellCapacity)
        {
            var visible = VisibleCells(maxSaturation, cellCapacity);
            if (visible <= 0 || currentSaturation <= 0)
            {
                return 0;
            }

            var filled = (visible * currentSaturation + maxSaturation - 1) / maxSaturation;
            return Math.Min(visible, filled);
        }
    }
}
