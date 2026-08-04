namespace SushiDefense.Customers
{
    /// <summary>
    /// 등속 1차원 벨트에서 초밥이 손님 범위에 들어오고 나가는 <b>시각</b>.
    ///
    /// <para>
    /// 매 프레임 손님 × 초밥을 전수 순회하는 대신, 벨트가 1차원·등속이라는 성질을 써서
    /// 진입 시각을 계산으로 얻는다. 물리 엔진도 트리거 콜백도 쓰지 않는다 — 콜백을 쓰면
    /// 판정이 <c>MonoBehaviour</c> 안으로 끌려들어가 EditMode 검증이 막힌다
    /// (<c>.claude/domain/sushi-claim-flow.md</c> §4, 작업서 D5).
    /// </para>
    /// </summary>
    public readonly struct ReachWindow
    {
        /// <summary>지금부터 진입까지 남은 시간(초). 이미 범위 안이면 0.</summary>
        public float EnterSeconds { get; }

        /// <summary>지금부터 이탈까지 남은 시간(초).</summary>
        public float ExitSeconds { get; }

        /// <summary>
        /// 앞으로 이 범위에 들어오거나 이미 들어와 있는가.
        /// 이미 지나갔거나 벨트가 멈춰 있으면 <c>false</c> 다.
        /// </summary>
        public bool WillEverEnter { get; }

        private ReachWindow(float enterSeconds, float exitSeconds, bool willEverEnter)
        {
            EnterSeconds = enterSeconds;
            ExitSeconds = exitSeconds;
            WillEverEnter = willEverEnter;
        }

        /// <summary>현재 위치·속도·범위로 진입/이탈 시각을 푼다.</summary>
        public static ReachWindow Solve(float sushiPosition, float beltSpeed,
                                        float reachMin, float reachMax)
        {
            // 멈춘(또는 뒤로 가는) 벨트에서는 진입이라는 사건 자체가 없다.
            // 0 으로 나누는 것을 막는 자리이기도 하다.
            if (beltSpeed <= 0f)
            {
                return default;
            }

            // 이미 먼 쪽 경계를 넘었다면 다시 들어올 일이 없다 — 벨트는 한 방향이다.
            if (sushiPosition > reachMax)
            {
                return default;
            }

            // 범위는 폐구간이라 가까운 쪽 경계에 걸친 초밥은 이미 안에 있는 것으로 본다.
            var enterSeconds = sushiPosition >= reachMin
                ? 0f
                : (reachMin - sushiPosition) / beltSpeed;

            var exitSeconds = (reachMax - sushiPosition) / beltSpeed;

            return new ReachWindow(enterSeconds, exitSeconds, true);
        }
    }
}
