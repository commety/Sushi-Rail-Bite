namespace SushiDefense.UI
{
    /// <summary>
    /// 스테이지 위에 뜨는 창. <b>값이 곧 우선순위</b>이며 클수록 위다.
    ///
    /// <para>
    /// 숫자를 명시적으로 적어 둔 이유는 <b>순서가 계약이기 때문</b>이다. 항목을 알파벳순으로
    /// 정렬하거나 가운데에 끼워 넣는 순간 우선순위가 조용히 바뀌는데, 그것은 화면에서만
    /// 드러나고 컴파일러는 아무 말도 하지 않는다.
    /// </para>
    /// <para>
    /// <b>유형별로 분기하기 위한 것이 아니다.</b> <see cref="StageWindowArbiter"/> 는 이 값을
    /// 크기 비교와 동일성 확인에만 쓴다 — 창마다 다른 규칙이 필요해지면 그것은 조정자가
    /// 아니라 그 창의 프레젠터가 가질 지식이다.
    /// </para>
    /// </summary>
    public enum StageWindow
    {
        /// <summary>앉아 있는 손님을 눌러 보는 정보 창. <b>가장 아래</b>다.</summary>
        CustomerInfo = 0,

        /// <summary>클리어 직후의 보상 선택. <b>하나를 위에 겹칠 수 있는 유일한 창</b>이다.</summary>
        Reward = 1,

        /// <summary>지금 초밥 덱 보기.</summary>
        Deck = 2,

        /// <summary>인스테이지 메뉴. 멈춤이 곧 이 창이다.</summary>
        Menu = 3,

        /// <summary>
        /// 설정. <b>메뉴 위에 겹쳐 뜨는 유일한 창</b>이라 가장 위다 — 메뉴에서 여는
        /// 화면인데 메뉴를 밀어내면 닫았을 때 돌아갈 곳이 없다.
        /// </summary>
        Settings = 4
    }
}
