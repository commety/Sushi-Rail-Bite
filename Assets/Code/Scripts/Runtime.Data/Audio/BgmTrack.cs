namespace SushiDefense.Data
{
    /// <summary>
    /// 한 화면이 트는 배경음이 뱅크의 어느 칸인가.
    ///
    /// <para>
    /// 화면이 스스로 «내 곡» 을 들고 있게 하지 않는 이유: 곡과 볼륨은 밸런스라 애셋에 있어야
    /// 하고(<c>.claude/rules/scriptable-object.md</c> §1), 그러면 남는 질문은 «둘 중 어느
    /// 칸이냐» 뿐이다. 그 한 가지만 씬에 둔다.
    /// </para>
    /// </summary>
    public enum BgmTrack
    {
        /// <summary>스테이지 배경음.</summary>
        Stage = 0,

        /// <summary>메인 화면 배경음.</summary>
        Main = 1
    }
}
