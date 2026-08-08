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
    /// 지금 멈춰 있는지를 <b>인자로 받는다.</b> 화면이 상태를 저장하면 프레젠터의 것과
    /// 어긋날 수 있고, 그때 어느 쪽이 진실인지 알 수 없다.
    /// </para>
    /// </summary>
    public interface IStageMenuView
    {
        /// <summary>메뉴를 연다. 버튼 표시를 맞추도록 지금 멈춰 있는지도 함께 넘긴다.</summary>
        void ShowMenu(bool paused);

        /// <summary>메뉴를 내린다.</summary>
        void Hide();
    }
}
