namespace SushiDefense.Run
{
    /// <summary>
    /// 결정적 난수원. <b>주입해서 쓴다.</b>
    ///
    /// <para>
    /// <c>UnityEngine.Random</c>·<c>System.Random</c> 을 직접 부르면 시드를 잡을 수 없어
    /// 같은 런이 재현되지 않고, 보상 테스트가 정확한 값 비교가 아니라 통계 검증이 된다.
    /// </para>
    /// <para>
    /// <b>배정 경로에는 이 인터페이스도 들어가지 않는다.</b> 누가 무엇을 먹는지는 순차번호가
    /// 모든 동률을 끝낸다 (<c>CLAUDE.md</c> §1.1-3b). 난수가 정당한 곳은 보상 추첨뿐이며,
    /// 그 경계는 <c>tests/preflight.sh</c> 의 허용 목록이 지킨다.
    /// </para>
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>
        /// <c>[0, exclusiveMax)</c> 범위의 정수.
        /// <paramref name="exclusiveMax"/> 가 1 미만이면 예외다.
        /// </summary>
        int Next(int exclusiveMax);
    }
}
