namespace SushiDefense.Stages
{
    /// <summary>
    /// 스테이지 한 판의 결과.
    ///
    /// <para>
    /// <see cref="InProgress"/> 는 <b>"아직 안 끝났다"</b> 이지 실패가 아니다. 실패는 제한
    /// 시간이 다 지나도록 목표에 닿지 못한 상태 하나뿐이다.
    /// </para>
    /// <para>
    /// <b>일시정지·재시도 같은 항목을 넣지 않는다.</b> 이 enum 은 <c>StageEvaluator</c> 가
    /// 답할 수 있는 값만 담는다 — 답할 수 없는 값이 섞이면 판정 함수가 그것을 돌려주지
    /// 못하면서도 호출자는 처리해야 하는 상태가 된다.
    /// </para>
    /// </summary>
    public enum StageOutcome
    {
        /// <summary>아직 진행 중이다.</summary>
        InProgress = 0,

        /// <summary>제한 시간 안에 목표 매출에 도달했다.</summary>
        Cleared = 1,

        /// <summary>제한 시간이 다 지나도록 목표 매출에 닿지 못했다.</summary>
        Failed = 2
    }
}
