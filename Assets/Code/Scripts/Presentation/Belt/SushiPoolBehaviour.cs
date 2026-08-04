using System;
using UnityEngine;

namespace SushiDefense.Belt
{
    /// <summary>
    /// <see cref="SushiPool{T}"/> 에 실제 <see cref="GameObject"/> 생성을 공급하는 지점.
    ///
    /// <para>
    /// 프로덕션 코드에서 <c>Instantiate</c>/<c>Destroy</c> 를 부르는 곳은 여기뿐이다
    /// (<c>CLAUDE.md</c> §3.4). 풀 로직 자체는 갖지 않고 전부 위임한다 — 로직이 이 클래스 안으로
    /// 들어오면 EditMode 로 검증할 수 없게 된다 (§3.2).
    /// </para>
    /// </summary>
    public sealed class SushiPoolBehaviour : MonoBehaviour, ISushiInstanceFactory<GameObject>
    {
        [SerializeField] private GameObject _sushiPrefab;
        [SerializeField] private Transform _inactiveParent;
        [SerializeField, Min(0)] private int _prewarmCount;

        private SushiPool<GameObject> _pool;

        /// <summary>이 풀이 관리하는 전체 인스턴스 수. 재사용 여부를 밖에서 확인할 때 쓴다.</summary>
        public int CountAll => _pool?.CountAll ?? 0;

        /// <summary>지금 대여 중인 인스턴스 수.</summary>
        public int CountActive => _pool?.CountActive ?? 0;

        private void Awake()
        {
            // 프리팹이 안 물린 채로 프리웜하면 Instantiate(null) 로 죽는다. 인스펙터 설정이
            // 끝나지 않은 상태에서도 컴포넌트가 살아 있어야 하므로 조용히 건너뛰고,
            // Initialize 가 나중에 풀을 세우게 둔다.
            if (_sushiPrefab != null)
            {
                BuildPool(_prewarmCount);
            }
        }

        /// <summary>
        /// 인스펙터 없이 풀을 세운다. 테스트와 부트스트랩용 진입점이며, 이미 만들어진 풀이 있으면
        /// 먼저 정리한다.
        /// </summary>
        public void Initialize(GameObject prefab, int prewarmCount)
        {
            _pool?.Clear();
            _sushiPrefab = prefab;
            _prewarmCount = prewarmCount;
            BuildPool(prewarmCount);
        }

        private void BuildPool(int prewarmCount)
        {
            if (_inactiveParent == null)
            {
                _inactiveParent = transform;
            }

            _pool = new SushiPool<GameObject>(this, prewarmCount);
        }

        /// <summary>인스턴스를 하나 빌려 활성화한다.</summary>
        public GameObject Rent()
        {
            var instance = RequirePool().Rent();
            instance.SetActive(true);
            return instance;
        }

        /// <summary>인스턴스를 비활성화해 풀에 돌려준다.</summary>
        public void Return(GameObject instance)
        {
            RequirePool().Return(instance);
            instance.SetActive(false);
            instance.transform.SetParent(_inactiveParent, false);
        }

        /// <summary>재사용할 것이 없을 때 풀이 부른다. 프리팹을 복제해 비활성 상태로 만든다.</summary>
        public GameObject Create()
        {
            var instance = Instantiate(_sushiPrefab, _inactiveParent);
            instance.SetActive(false);
            return instance;
        }

        /// <summary>풀 정리 시에만 불린다. 대여·반납 경로에서는 불리지 않는다.</summary>
        public void Dispose(GameObject instance)
        {
            Destroy(instance);
        }

        private SushiPool<GameObject> RequirePool()
        {
            if (_pool == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(SushiPoolBehaviour)} 가 초기화되지 않았습니다 — 프리팹을 인스펙터에 물리거나 {nameof(Initialize)} 를 부르세요.");
            }

            return _pool;
        }
    }
}
