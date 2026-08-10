using System;
using System.Collections.Generic;

namespace SushiDefense.Belt
{
    /// <summary>
    /// 초밥 인스턴스 풀. 반납된 것을 먼저 재사용하고, 없을 때만 팩토리로 새로 만든다.
    ///
    /// <para>
    /// 벨트는 초밥을 계속 만들고 버리는 구조라, 매번 새로 생성하면 배포 타깃인 WebGL 에서
    /// GC 스파이크가 그대로 프레임 히칭이 된다 (<c>CLAUDE.md</c> §3.4 · §4.3). 그래서 대여·반납
    /// 경로에서는 할당을 만들지 않는다 — 내부 자료구조는 생성자에서 한 번만 잡는다.
    /// </para>
    /// </summary>
    /// <typeparam name="T">풀이 담는 인스턴스 타입.</typeparam>
    public sealed class SushiPool<T> where T : class
    {
        private readonly ISushiInstanceFactory<T> _factory;
        private readonly Stack<T> _inactive;
        private readonly HashSet<T> _active;

        /// <summary>대여를 기다리는 인스턴스 수.</summary>
        public int CountInactive => _inactive.Count;

        /// <summary>지금 대여 중인 인스턴스 수.</summary>
        public int CountActive => _active.Count;

        /// <summary>이 풀이 관리하는 전체 인스턴스 수.</summary>
        public int CountAll => CountActive + CountInactive;

        /// <summary>
        /// 팩토리를 물리고, 필요하면 <paramref name="prewarmCount"/> 만큼 미리 만들어 둔다.
        /// 프리웜 개수는 호출자가 준다 — 풀이 기본값을 들고 있으면 그게 밸런스 상수가 된다 (§3.1).
        /// </summary>
        public SushiPool(ISushiInstanceFactory<T> factory, int prewarmCount = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            if (prewarmCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prewarmCount));
            }

            _inactive = new Stack<T>(prewarmCount);
            _active = new HashSet<T>();

            for (var i = 0; i < prewarmCount; i++)
            {
                _inactive.Push(_factory.Create());
            }
        }

        /// <summary>인스턴스를 하나 빌린다. 재사용할 것이 없으면 새로 만든다.</summary>
        public T Rent()
        {
            var instance = _inactive.Count > 0 ? _inactive.Pop() : _factory.Create();
            _active.Add(instance);
            return instance;
        }

        /// <summary>
        /// 빌린 인스턴스를 돌려준다.
        ///
        /// <para>
        /// 이 풀에서 대여한 것이 아니거나 이미 반납한 인스턴스면 <b>예외를 던진다</b>.
        /// 조용히 넘기면 같은 인스턴스가 두 번 대여되어 화면에서 초밥이 겹친다.
        /// </para>
        /// </summary>
        public void Return(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (!_active.Remove(instance))
            {
                throw new ArgumentException(
                    "이 풀에서 대여하지 않았거나 이미 반납한 인스턴스입니다.", nameof(instance));
            }

            _inactive.Push(instance);
        }

        /// <summary>관리 중인 인스턴스를 전부 버린다. 대여 중인 것도 함께 정리한다.</summary>
        public void Clear()
        {
            foreach (var instance in _active)
            {
                _factory.Dispose(instance);
            }

            _active.Clear();

            while (_inactive.Count > 0)
            {
                _factory.Dispose(_inactive.Pop());
            }
        }
    }
}
