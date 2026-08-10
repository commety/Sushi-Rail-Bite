namespace SushiDefense.Navigation
{
    /// <summary>
    /// 어느 화면으로 갈지. <b>구현만 씬 로딩을 안다.</b>
    ///
    /// <para>
    /// 프레젠터가 씬을 직접 로드하면 <i>"나가기를 누르면 메인으로 간다"</i> 를 EditMode 로
    /// 확인할 수 없다 — 씬을 실제로 띄워야 하기 때문이다. 인터페이스 뒤로 밀면 스텁이
    /// 호출 횟수만 세면 된다 (<c>CLAUDE.md</c> §3.6, <c>ISettingsStore</c> 와 같은 방향 전환).
    /// </para>
    /// </summary>
    public interface ISceneRouter
    {
        /// <summary>메인 화면으로 간다. <b>런은 버려진다</b> — 저장은 M6 의 범위가 아니다.</summary>
        void LoadMain();

        /// <summary>스테이지로 간다. 새 런이 열린다.</summary>
        void LoadStage();
    }
}
