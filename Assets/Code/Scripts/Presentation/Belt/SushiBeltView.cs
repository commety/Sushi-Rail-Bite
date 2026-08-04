using System;
using System.Collections.Generic;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Belt
{
    /// <summary>
    /// 벨트 로직의 화면 대응물. <see cref="SushiBelt"/> 이벤트를 구독해 풀에서 뷰를 빌리고
    /// 돌려주며, 매 프레임 위치만 반영한다. <b>판정이 없다.</b>
    ///
    /// <para>
    /// 시뮬레이션을 굴리지 않는다 — <c>ClaimCoordinator.Tick</c> 은 씬 진입점이 부른다.
    /// 뷰가 시간을 흘리면 "그리는 일" 과 "판정을 돌리는 일" 이 한 컴포넌트에 섞이고,
    /// 테스트가 프레임 타이밍에 묶인다 (<c>CLAUDE.md</c> §3.2).
    /// </para>
    /// </summary>
    public sealed class SushiBeltView : MonoBehaviour
    {
        [SerializeField] private SushiPoolBehaviour _viewPool;
        [SerializeField] private StageConfig _stageConfig;
        [SerializeField] private Transform _beltStart;
        [SerializeField] private Transform _beltEnd;

        private readonly Dictionary<SushiItem, SushiItemView> _views = new();
        private SushiBelt _belt;

        /// <summary>지금 화면에 떠 있는 초밥 뷰 수. 재사용 여부를 밖에서 확인할 때 쓴다.</summary>
        public int VisibleCount => _views.Count;

        /// <summary>
        /// 인스펙터 없이 참조를 물린다. 테스트와 씬 부트스트랩용 진입점이며,
        /// <c>SushiPoolBehaviour.Initialize</c> 와 같은 역할이다.
        /// </summary>
        public void Initialize(SushiPoolBehaviour viewPool, StageConfig stageConfig,
                               Transform beltStart, Transform beltEnd)
        {
            _viewPool = viewPool;
            _stageConfig = stageConfig;
            _beltStart = beltStart;
            _beltEnd = beltEnd;
        }

        /// <summary>씬 진입점이 로직을 물려 준다. 뷰가 로직을 만들지 않는다.</summary>
        public void Bind(SushiBelt belt)
        {
            Unbind();

            _belt = belt ?? throw new ArgumentNullException(nameof(belt));
            _belt.SushiSpawned += OnSushiSpawned;
            _belt.SushiRemoved += OnSushiRemoved;
        }

        /// <summary>구독을 끊고 떠 있는 뷰를 전부 반납한다.</summary>
        public void Unbind()
        {
            if (_belt == null)
            {
                return;
            }

            _belt.SushiSpawned -= OnSushiSpawned;
            _belt.SushiRemoved -= OnSushiRemoved;
            _belt = null;

            foreach (var view in _views.Values)
            {
                view.Release();
                _viewPool.Return(view.gameObject);
            }

            _views.Clear();
        }

        private void LateUpdate()
        {
            SyncPositions();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// 벨트 좌표를 화면 좌표로 옮긴다. <b>변환은 이 메서드 한곳에서만</b> 한다 —
        /// 여러 곳에서 변환하면 씬에서 벨트를 옮길 때 어긋난다.
        /// </summary>
        private void SyncPositions()
        {
            if (_belt == null)
            {
                return;
            }

            var active = _belt.ActiveSushi;
            for (var i = 0; i < active.Count; i++)
            {
                var item = active[i];
                if (_views.TryGetValue(item, out var view))
                {
                    view.transform.position = ToWorldPosition(item.BeltPosition);
                }
            }
        }

        private Vector3 ToWorldPosition(float beltPosition)
        {
            var progress = beltPosition / _stageConfig.BeltLength;
            return Vector3.LerpUnclamped(_beltStart.position, _beltEnd.position, progress);
        }

        private void OnSushiSpawned(SushiItem item)
        {
            var instance = _viewPool.Rent();
            if (!instance.TryGetComponent<SushiItemView>(out var view))
            {
                throw new InvalidOperationException(
                    $"초밥 프리팹에 {nameof(SushiItemView)} 가 없습니다: {instance.name}");
            }

            view.Bind(item);
            view.transform.position = ToWorldPosition(item.BeltPosition);
            _views[item] = view;
        }

        private void OnSushiRemoved(SushiItem item)
        {
            if (!_views.TryGetValue(item, out var view))
            {
                return;
            }

            _views.Remove(item);
            view.Release();
            _viewPool.Return(view.gameObject);
        }
    }
}
