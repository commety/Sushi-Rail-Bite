namespace SushiDefense.UI
{
    /// <summary>
    /// 인스테이지 메뉴가 프레젠터에게 제공하는 것.
    ///
    /// <para>
    /// <b>Unity 타입이 하나도 없다.</b> 프레젠터를 EditMode 로 검증하기 위해서다
    /// (<c>CLAUDE.md</c> §3.6).
    /// </para>
    /// <para>
    /// 화면이 상태를 저장하지 않고 <b>인자로 받는다.</b> 저장하면 프레젠터의 것과 어긋날 수
    /// 있고, 그때 어느 쪽이 진실인지 알 수 없다.
    /// </para>
    /// </summary>
    public interface IStageMenuView
    {
        /// <summary>
        /// 메뉴를 연다.
        /// </summary>
        /// <param name="canResume">
        /// 판으로 돌아갈 수 있나. 실패해서 열린 메뉴는 <c>false</c> 이고, 그때 재개 버튼을
        /// 감춘다 — 눌러도 아무 일이 없는 버튼을 남기면 «고장» 으로 읽힌다.
        /// </param>
        void ShowMenu(bool canResume);

        /// <summary>메뉴를 내린다.</summary>
        void Hide();
    }
}
