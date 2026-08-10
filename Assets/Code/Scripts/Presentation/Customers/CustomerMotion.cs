namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님 몸통이 지금 무엇을 하고 있는가. <b>상태(<see cref="CustomerState"/>)와 1:1 이 아니다</b> —
    /// 집기는 상태가 아니라 «먹기의 첫머리» 이고, 대기는 몸통이 아니라 말풍선이 맡는다.
    /// </summary>
    public enum CustomerMotion
    {
        /// <summary>아무 동작도 하지 않는다. 유형별 낱장 그림이 그대로 서 있는다.</summary>
        None = 0,

        /// <summary>초밥을 집는 순간. 배정이 확정된 직후 한 번 지나간다.</summary>
        Picking = 1,

        /// <summary>먹는 중. 대기 클립을 <b>빠르게</b> 돌린 모습이다.</summary>
        Eating = 2,

        /// <summary>소화 중. 같은 클립을 <b>느리게</b> 돌린 모습이다.</summary>
        Digesting = 3
    }
}
