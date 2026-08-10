namespace SushiDefense.Customers
{
    /// <summary>
    /// 대기 클립 한 장을 <b>먹기</b>와 <b>소화</b> 두 모습으로 갈라 내는 재생 배속.
    ///
    /// <para>
    /// 배속을 손으로 박지 않고 <b>밸런스에서 유도한다</b>: 손님이 초밥 하나를 먹는 동안
    /// 클립이 정확히 <i>n</i> 번 돈다. 그래서 먹는 시간이 짧은 먹보는 저절로 빠르게,
    /// 소식가는 느리게 씹는다 — 유형별 성격이 밸런스 수치 하나에서 따라 나온다.
    /// </para>
    /// <para>
    /// <b>도는 횟수는 밸런스가 아니라 연출이다.</b> «한 입에 몇 번 씹나» 는 사람이 보며
    /// 정하는 값이라 프리팹의 <c>[SerializeField]</c> 에 살고
    /// (<c>.claude/domain/presentation-and-audio.md</c> §3), 이 클래스는 그 둘을 곱하는
    /// 산술만 갖는다.
    /// </para>
    /// </summary>
    public static class CustomerMotionSpeed
    {
        /// <summary>배속을 유도할 수 없을 때 쓰는 값. 클립이 제 속도로 돈다.</summary>
        public const float Natural = 1f;

        /// <summary>
        /// <paramref name="motion"/> 에 줄 <c>Animator.speed</c>.
        ///
        /// <para>
        /// 집기는 <see cref="Natural"/> 이다 — 집는 동작은 손님이 얼마나 오래 먹는지와
        /// 무관하게 같은 속도로 일어난다.
        /// </para>
        /// </summary>
        /// <param name="clipSeconds">대기 클립 한 바퀴의 길이.</param>
        /// <param name="eatSeconds">초밥 하나를 소비하는 시간 (<c>CustomerData.EatSeconds</c>).</param>
        /// <param name="digestSeconds">포화 후 쉬는 시간 (<c>CustomerData.DigestSeconds</c>).</param>
        /// <param name="chewCycles">먹는 동안 클립이 도는 횟수.</param>
        /// <param name="restCycles">쉬는 동안 클립이 도는 횟수.</param>
        public static float For(CustomerMotion motion, float clipSeconds,
                                float eatSeconds, float digestSeconds,
                                int chewCycles, int restCycles)
        {
            return motion switch
            {
                CustomerMotion.Eating => Over(clipSeconds, eatSeconds, chewCycles),
                CustomerMotion.Digesting => Over(clipSeconds, digestSeconds, restCycles),
                _ => Natural
            };
        }

        /// <summary>
        /// <paramref name="windowSeconds"/> 안에 클립이 <paramref name="cycles"/> 번 돌게 하는 배속.
        ///
        /// <para>
        /// <b>0 을 나누지 않는다.</b> 먹는 시간이 0 인 손님(placeholder)이 실제로 있고,
        /// 그대로 나누면 배속이 무한대가 되어 <c>Animator</c> 가 조용히 멈춘다 — 화면에는
        /// 첫 프레임에 굳은 손님만 남고 예외는 나지 않는다.
        /// </para>
        /// </summary>
        private static float Over(float clipSeconds, float windowSeconds, int cycles)
        {
            if (clipSeconds <= 0f || windowSeconds <= 0f || cycles <= 0)
            {
                return Natural;
            }

            return cycles * clipSeconds / windowSeconds;
        }
    }
}
