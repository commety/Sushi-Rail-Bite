namespace SushiDefense.UI
{
    /// <summary>
    /// 같은 스테이지를 다시 연다. 구현은 씬 진입점이다.
    ///
    /// <para>
    /// 프레젠터가 <c>StageBootstrap</c> 을 직접 알면 <c>MonoBehaviour</c> 없이 검증할 수
    /// 없다 — <see cref="SushiDefense.Navigation.ISceneRouter"/> 와 같은 방향 전환이다.
    /// </para>
    /// </summary>
    public interface IStageRestarter
    {
        /// <summary>
        /// 판을 다시 세운다. <b>덱과 명부는 유지되고</b> 시도 횟수가 오른다 — 스스로 다시
        /// 시작하는 것도 그 판을 포기한 것이므로, 실패 후 재시도와 같은 경로를 쓴다.
        /// </summary>
        void Restart();
    }
}
