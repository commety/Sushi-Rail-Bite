namespace SushiDefense.Customers
{
    /// <summary>
    /// 한 손님의 배정을 <b>언제</b> 확정할지에 대한 판정. <c>ClaimDeadline</c> 이 낸다.
    ///
    /// <para>
    /// <b>자격이 아니다.</b> <see cref="Nothing"/> 은 "먹을 수 없다" 가 아니라 "지금 집을
    /// 것이 없다" 이고, <see cref="Waiting"/> 은 유한 시간 안에 반드시 <see cref="Due"/> 로
    /// 바뀐다 (<c>CLAUDE.md</c> §1.1-3a).
    /// </para>
    /// </summary>
    public enum ClaimTiming
    {
        /// <summary>살아 있는 후보가 없다. 확정할 대상 자체가 없다.</summary>
        Nothing,

        /// <summary>대역 밖 후보뿐이라 더 좋은 것을 기다린다. 화면에 구분해 표시한다.</summary>
        Waiting,

        /// <summary>지금 확정한다 — 대역 안이거나, 최선 후보의 마감시한이 됐다.</summary>
        Due
    }
}
