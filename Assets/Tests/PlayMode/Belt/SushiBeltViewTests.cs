using System.Collections;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Data;
using SushiDefense.Run;
using UnityEngine;
using UnityEngine.TestTools;

namespace SushiDefense.Tests.PlayMode.Belt
{
    /// <summary>
    /// 풀 로직 자체는 EditMode 가 이미 검증한다. 여기서는 실물 <see cref="GameObject"/> 가
    /// 정말 빌려지고 돌아오는지, 좌표가 따라오는지만 본다 (<c>.claude/rules/tests.md</c> §1).
    /// </summary>
    public sealed class SushiBeltViewTests
    {
        private const float Speed = 10f;
        private const float Interval = 1f;
        private const float Length = 100f;

        private GameObject _prefab;
        private GameObject _poolHost;
        private GameObject _viewHost;
        private GameObject _startMarker;
        private GameObject _endMarker;
        private SushiData _sushiData;
        private StageConfig _config;
        private SushiPoolBehaviour _pool;
        private SushiBeltView _view;
        private SushiBelt _belt;

        [SetUp]
        public void SetUp()
        {
            // 디스크의 프리팹 애셋을 로드하지 않는다 — 테스트가 애셋에 묶이면 워크트리마다 깨진다.
            _prefab = new GameObject("SushiPrefab");
            _prefab.AddComponent<SushiItemView>();
            _prefab.SetActive(false);

            _poolHost = new GameObject("SushiPool");
            _pool = _poolHost.AddComponent<SushiPoolBehaviour>();
            _pool.Initialize(_prefab, 0);

            _startMarker = new GameObject("BeltStart");
            _startMarker.transform.position = Vector3.zero;
            _endMarker = new GameObject("BeltEnd");
            _endMarker.transform.position = new Vector3(10f, 0f, 0f);

            _sushiData = ScriptableObject.CreateInstance<SushiData>();
            _config = StageConfigTestFactory.Create(Speed, Interval, Length, _sushiData);

            _viewHost = new GameObject("SushiBeltView");
            _view = _viewHost.AddComponent<SushiBeltView>();
            _view.Initialize(_pool, _config, _startMarker.transform, _endMarker.transform);

            _belt = new SushiBelt(_config, SushiDeck.FromSpawnTable(_config).Cards,
                                  new SequenceNumberIssuer(),
                                  new SushiPool<SushiItem>(new SushiItemFactory()));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_viewHost);
            Object.DestroyImmediate(_poolHost);
            Object.DestroyImmediate(_startMarker);
            Object.DestroyImmediate(_endMarker);
            Object.DestroyImmediate(_prefab);
            Object.DestroyImmediate(_sushiData);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Bind_SushiSpawned_RentsViewFromPool()
        {
            _view.Bind(_belt);

            _belt.Tick(Interval);

            Assert.AreEqual(1, _pool.CountActive);
            Assert.AreEqual(1, _view.VisibleCount);
        }

        [Test]
        public void Bind_SushiSpawned_BindsModelToView()
        {
            _view.Bind(_belt);

            _belt.Tick(Interval);

            var view = _poolHost.GetComponentInChildren<SushiItemView>(true);
            Assert.AreSame(_belt.ActiveSushi[0], view.Model);
        }

        [Test]
        public void Bind_SushiRemoved_ReturnsViewToPool()
        {
            _view.Bind(_belt);
            _belt.Tick(Interval);

            _belt.Remove(_belt.ActiveSushi[0]);

            Assert.AreEqual(0, _pool.CountActive);
            Assert.AreEqual(1, _pool.CountAll);
            Assert.AreEqual(0, _view.VisibleCount);
        }

        [Test]
        public void Bind_SushiRemoved_DoesNotDestroyGameObject()
        {
            _view.Bind(_belt);
            _belt.Tick(Interval);
            var instance = _poolHost.GetComponentInChildren<SushiItemView>(true).gameObject;

            _belt.Remove(_belt.ActiveSushi[0]);

            Assert.IsTrue(instance != null, "반납은 파괴가 아니다");
            Assert.IsFalse(instance.activeSelf);
        }

        [Test]
        public void Bind_SushiRemoved_ClearsModelReference()
        {
            _view.Bind(_belt);
            _belt.Tick(Interval);
            var view = _poolHost.GetComponentInChildren<SushiItemView>(true);

            _belt.Remove(_belt.ActiveSushi[0]);

            Assert.IsNull(view.Model, "풀에 누운 뷰가 죽은 모델을 붙들면 안 된다");
        }

        [Test]
        public void Bind_ManySpawns_ReusesViewsAfterRemoval()
        {
            _view.Bind(_belt);

            for (var i = 0; i < 5; i++)
            {
                _belt.Tick(Interval);
                _belt.Remove(_belt.ActiveSushi[0]);
            }

            Assert.AreEqual(1, _pool.CountAll, "반납분을 재사용하면 인스턴스가 늘지 않는다");
        }

        [UnityTest]
        public IEnumerator LateUpdate_SushiAdvances_MovesViewTowardBeltEnd()
        {
            _view.Bind(_belt);
            _belt.Tick(Interval);
            var instance = _poolHost.GetComponentInChildren<SushiItemView>(true).transform;
            yield return null;
            var before = instance.position.x;

            _belt.Tick(1f);
            yield return null;

            Assert.Greater(instance.position.x, before);
        }

        [UnityTest]
        public IEnumerator LateUpdate_HalfwayAlongBelt_SitsBetweenMarkers()
        {
            _view.Bind(_belt);
            _belt.Tick(Interval);
            var instance = _poolHost.GetComponentInChildren<SushiItemView>(true).transform;

            // 벨트 길이 100 의 절반까지 흘린다.
            _belt.Tick(Length * 0.5f / Speed);
            yield return null;

            var expectedX = Mathf.Lerp(_startMarker.transform.position.x,
                                       _endMarker.transform.position.x, 0.5f);
            Assert.AreEqual(expectedX, instance.position.x, 0.01f);
        }

        [Test]
        public void Unbind_ThenBeltSpawns_DoesNotRentAnything()
        {
            _view.Bind(_belt);
            _belt.Tick(Interval);

            _view.Unbind();
            _belt.Tick(Interval);

            Assert.AreEqual(0, _pool.CountActive, "구독을 끊었으면 더 빌리지 않는다");
        }

        [Test]
        public void Unbind_ReturnsVisibleViews()
        {
            _view.Bind(_belt);
            _belt.Tick(Interval);

            _view.Unbind();

            Assert.AreEqual(0, _view.VisibleCount);
            Assert.AreEqual(0, _pool.CountActive);
        }

        [Test]
        public void OnDestroy_Unsubscribes_NoCallbackAfterTeardown()
        {
            _view.Bind(_belt);
            Object.DestroyImmediate(_viewHost);
            _viewHost = null;

            Assert.DoesNotThrow(() => _belt.Tick(Interval));
            Assert.AreEqual(0, _pool.CountActive);
        }
    }
}
