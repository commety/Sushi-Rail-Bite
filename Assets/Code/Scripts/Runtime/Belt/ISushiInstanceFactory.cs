namespace SushiDefense.Belt
{
    /// <summary>
    /// 풀이 채울 인스턴스를 만드는 쪽.
    ///
    /// <para>
    /// 이 인터페이스가 있는 이유는 <c>SushiPool</c> 을 Unity 오브젝트 생성에서 떼어내기 위해서다.
    /// 실제 구현은 <c>Presentation</c> 에 있고(step-06), <c>Runtime</c> 은 인스턴스가 어떻게
    /// 만들어지는지 모른 채로 남는다 — 덕분에 풀 로직이 EditMode 로 검증된다 (<c>CLAUDE.md</c> §3.2).
    /// </para>
    /// </summary>
    /// <typeparam name="T">풀이 담는 인스턴스 타입.</typeparam>
    public interface ISushiInstanceFactory<T> where T : class
    {
        /// <summary>새 인스턴스를 만든다. 풀에 재사용할 것이 없을 때만 불린다.</summary>
        T Create();

        /// <summary>
        /// 인스턴스를 완전히 버린다. <b>대여·반납 경로에서는 불리지 않는다</b> —
        /// 풀을 정리할 때만 불린다.
        /// </summary>
        void Dispose(T instance);
    }
}
