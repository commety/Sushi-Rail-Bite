namespace SushiDefense.UI
{
    /// <summary>
    /// 손님 정보 창의 화면 쪽 계약. 구현은 <c>MonoBehaviour</c> 지만 프레젠터는 그것을 모른다
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// <b>정적 줄과 실시간 줄을 두 메서드로 가른다.</b> 하나로 합치면 매 프레임 정적
    /// 문자열까지 다시 만들게 되고, 그것이 곧 <c>Update</c> 경로 할당이다 (§4.3).
    /// </para>
    /// <para>
    /// <b>글자만 오간다.</b> 손님 타입을 넘기면 화면이 <c>Runtime</c> 을 알게 되어, 무엇을
    /// 어떻게 적을지가 뷰로 새어 나간다.
    /// </para>
    /// </summary>
    public interface ICustomerInspectorView
    {
        /// <summary>
        /// 창을 띄우고 손님마다 고정된 줄을 그린다.
        ///
        /// <para>
        /// <b>유형을 따로 받지 않는다.</b> 세 손님의 표시 이름이 곧 유형이라(<c>기본</c>·
        /// <c>소식</c>·<c>먹보</c>) 둘을 다 그리면 같은 글자가 창에 두 번 나온다. 이름과
        /// 유형이 갈라지는 손님이 생기면 그때 되살릴 자리다.
        /// </para>
        /// </summary>
        void ShowCustomer(string name, string stats);

        /// <summary>
        /// 판이 흐르는 동안 바뀌는 줄만 다시 쓴다.
        /// </summary>
        /// <param name="remaining">소화 중이 아니면 빈 문자열이다.</param>
        void RefreshLive(string state, string saturation, string remaining);

        /// <summary>창을 내린다.</summary>
        void Hide();
    }
}
