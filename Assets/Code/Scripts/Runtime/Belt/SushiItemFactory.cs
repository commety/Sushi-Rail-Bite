namespace SushiDefense.Belt
{
    /// <summary>
    /// <see cref="SushiPool{T}"/> 를 채울 순수 상태 객체 팩토리.
    ///
    /// <para>
    /// 화면 오브젝트와 무관하다 — <c>GameObject</c> 풀은 <c>Presentation</c> 의
    /// <c>SushiPoolBehaviour</c> 가 따로 갖는다 (작업서 D2). 여기서 재사용하는 것은
    /// 스폰마다 <c>new</c> 하면 WebGL 에서 쌓이는 상태 객체 쪽이다.
    /// </para>
    /// </summary>
    public sealed class SushiItemFactory : ISushiInstanceFactory<SushiItem>
    {
        /// <summary>아직 스폰되지 않은 상태의 순차번호. 대여 직후 벨트가 덮어쓴다.</summary>
        private const int UnassignedSequenceNumber = -1;

        /// <summary>빈 상태로 만든다. 실제 데이터·순차번호는 대여 직후 벨트가 채운다.</summary>
        public SushiItem Create()
        {
            return new SushiItem(null, UnassignedSequenceNumber);
        }

        /// <summary>순수 객체라 따로 해제할 자원이 없다. GC 가 정리한다.</summary>
        public void Dispose(SushiItem instance)
        {
        }
    }
}
