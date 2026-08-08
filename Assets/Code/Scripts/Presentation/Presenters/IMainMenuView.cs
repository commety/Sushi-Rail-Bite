namespace SushiDefense.UI
{
    /// <summary>
    /// 메인 화면이 프레젠터에게 제공하는 것.
    ///
    /// <para>
    /// <b>Unity 타입이 하나도 없다.</b> 프레젠터를 EditMode 로 검증하기 위해서다
    /// (<c>CLAUDE.md</c> §3.6, <see cref="IStageMenuView"/> 와 같은 형태).
    /// </para>
    /// <para>
    /// 인자가 없다. 메인 화면에는 <b>바뀌는 값이 없다</b> — 제목과 버튼 셋이 전부이고,
    /// 스테이지 선택도 덱 편집도 여기 없다.
    /// </para>
    /// </summary>
    public interface IMainMenuView
    {
        /// <summary>메뉴를 띄운다.</summary>
        void ShowMenu();

        /// <summary>메뉴를 내린다. 설정·백과사전이 그 위를 덮을 때 쓴다.</summary>
        void Hide();
    }
}
