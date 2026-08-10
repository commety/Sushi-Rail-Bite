namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님의 상태. 전이는 <c>Idle → Eating → Digesting → Idle</c> 이다.
    ///
    /// <para>
    /// 전이 조건과 타이머 진행은 여기 없다 — 테스트 가능한 순수 클래스인 <c>CustomerLogic</c>(M2)
    /// 이 갖는다 (<c>CLAUDE.md</c> §3.5).
    /// </para>
    /// </summary>
    public enum CustomerState
    {
        /// <summary>먹고 있지 않다. 새 초밥을 배정받을 수 있는 유일한 상태다.</summary>
        Idle = 0,

        /// <summary>배정받은 초밥을 소비하는 중이다.</summary>
        Eating = 1,

        /// <summary>포화되어 쉬는 중이다. 소화가 끝나면 <see cref="Idle"/> 로 돌아간다.</summary>
        Digesting = 2
    }
}
