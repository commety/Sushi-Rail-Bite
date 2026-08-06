namespace SushiDefense.Stages
{
    /// <summary>
    /// 클리어/실패 규칙. <b>시간을 모른다</b> — 만료 여부를 <c>bool</c> 로 받으므로 경계
    /// 규칙을 시계 없이 고정할 수 있다.
    /// </summary>
    public static class StageEvaluator
    {
        /// <summary>
        /// 지금 판정하면 무엇이 나오나.
        ///
        /// <para>
        /// <b>매출을 먼저 본다.</b> 그래서 제한 시간 정각에 목표를 채우면 클리어다. 순서를
        /// 뒤집어 만료를 먼저 보면, 마지막 프레임에 목표를 채운 플레이어가 실패한다.
        /// </para>
        /// <para>
        /// <b>보너스 목표는 여기 들어오지 않는다</b> (<c>StageConfig.BonusObjectives</c>).
        /// 그건 추가 보상 조건이지 클리어 조건이 아니다 — 조건이 늘어 이 함수의 분기가
        /// 셋 이상이 되면 규칙이 새로 생긴 것이므로 설계를 다시 본다.
        /// </para>
        /// </summary>
        public static StageOutcome Evaluate(int revenue, int targetRevenue, bool expired)
        {
            if (revenue >= targetRevenue)
            {
                return StageOutcome.Cleared;
            }

            return expired ? StageOutcome.Failed : StageOutcome.InProgress;
        }
    }
}
