namespace SushiDefense.Run
{
    /// <summary>보상 한 장이 무엇인가. 원본 기획의 <b>"초밥 카드 추가 또는 손님 영입"</b> 둘이다.</summary>
    public enum RewardKind
    {
        /// <summary>덱에 초밥 종류가 하나 는다.</summary>
        SushiCard = 0,

        /// <summary>명부에 배치 가능한 손님이 하나 는다.</summary>
        Customer = 1
    }
}
