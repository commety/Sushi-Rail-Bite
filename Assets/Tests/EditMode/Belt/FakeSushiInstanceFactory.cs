using System.Collections.Generic;
using SushiDefense.Belt;

namespace SushiDefense.Tests.EditMode.Belt
{
    /// <summary>
    /// 손으로 쓴 팩토리 스텁. 생성·폐기 호출을 세어 "재사용됐는가"를 <b>호출 횟수로</b> 검증한다 —
    /// 인스턴스가 같은지만 보면 풀이 몰래 새로 만들고 있어도 통과할 수 있다.
    /// </summary>
    internal sealed class FakeSushiInstanceFactory : ISushiInstanceFactory<object>
    {
        public int CreateCallCount { get; private set; }

        public List<object> Disposed { get; } = new();

        public object Create()
        {
            CreateCallCount++;
            return new object();
        }

        public void Dispose(object instance)
        {
            Disposed.Add(instance);
        }
    }
}
