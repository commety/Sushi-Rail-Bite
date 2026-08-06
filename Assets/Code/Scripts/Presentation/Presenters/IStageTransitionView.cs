namespace SushiDefense.UI
{
    /// <summary>
    /// 스테이지 전환 화면이 프레젠터에게 제공하는 것.
    ///
    /// <para>
    /// <b>Unity 타입이 하나도 없다.</b> 프레젠터를 EditMode 로 검증하기 위해서다
    /// (<c>CLAUDE.md</c> §3.6). <c>StageConfig</c> 가 아니라 <b>번호</b>만 넘기는 것도 같은
    /// 이유이며, 뷰가 스테이지 설정을 읽을 필요가 없어진다.
    /// </para>
    /// <para>
    /// 클리어와 런 종료를 <b>다른 메서드</b>로 나눈 이유: 한 메서드에 <c>bool</c> 을
    /// 넘기면 뷰가 그것으로 분기하게 되고, 그 분기가 곧 판정이 된다. 무엇을 보여줄지는
    /// 프레젠터가 이미 정했다.
    /// </para>
    /// </summary>
    public interface IStageTransitionView
    {
        /// <summary>스테이지를 깼고 다음이 있다. 두 번호를 모두 보여 준다.</summary>
        void ShowStageCleared(int clearedStageNumber, int nextStageNumber);

        /// <summary>마지막 스테이지를 깼다. 넘어갈 곳이 없다.</summary>
        void ShowRunComplete(int clearedStageNumber);

        /// <summary>화면을 내린다.</summary>
        void Hide();
    }
}
