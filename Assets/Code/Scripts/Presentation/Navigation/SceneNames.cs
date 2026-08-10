namespace SushiDefense.Navigation
{
    /// <summary>
    /// 씬 이름의 <b>유일한 출처</b>.
    ///
    /// <para>
    /// 문자열 씬 이름은 오타가 컴파일에 잡히지 않는다 — 잘못 쓰면 로드가 조용히 실패하고
    /// 화면이 그대로 멈춘다. 상수로 묶으면 최소한 <b>한 곳에서만</b> 틀릴 수 있다.
    /// </para>
    /// <para>
    /// 이 상수가 Build Settings 의 등록 경로와 맞는지는 씬 테스트가 본다 — M5 가 등록을
    /// 빠뜨려 <i>"빌드는 성공하고 화면에는 클리어 색만"</i> 을 만든 자리가 정확히 여기다.
    /// </para>
    /// <para>
    /// <c>const</c> 만 둔다. 가변 정적 상태가 아니므로 초기화 메서드가 필요 없다 (RULE-01) —
    /// 필드를 더하고 싶어지면 그때 그 규칙을 다시 본다.
    /// </para>
    /// </summary>
    public static class SceneNames
    {
        /// <summary>메인 화면. 빌드의 첫 씬이다.</summary>
        public const string Main = "Main";

        /// <summary>스테이지. 3스테이지 전부가 이 한 씬 안에서 진행된다.</summary>
        public const string Stage = "Stage01";
    }
}
