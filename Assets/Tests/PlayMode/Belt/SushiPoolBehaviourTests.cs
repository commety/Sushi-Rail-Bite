using NUnit.Framework;
using SushiDefense.Belt;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode.Belt
{
    /// <summary>
    /// 풀 로직 자체는 EditMode(<c>SushiPoolTests</c>)가 이미 검증한다. 여기서는 실물
    /// <see cref="GameObject"/> 가 정말 재사용되고 활성 상태가 오가는지만 본다
    /// (<c>.claude/rules/tests.md</c> §1 — PlayMode 는 최소한으로).
    /// </summary>
    public sealed class SushiPoolBehaviourTests
    {
        private GameObject _host;
        private GameObject _prefab;
        private SushiPoolBehaviour _pool;

        [SetUp]
        public void SetUp()
        {
            // 디스크의 프리팹 애셋을 로드하지 않는다 — 테스트가 애셋에 묶이면 워크트리마다 깨진다.
            _prefab = new GameObject("SushiPrefab");
            _prefab.SetActive(false);

            _host = new GameObject("SushiPool");
            _pool = _host.AddComponent<SushiPoolBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_prefab);
        }

        [Test]
        public void Rent_EmptyPool_InstantiatesNewGameObject()
        {
            _pool.Initialize(_prefab, 0);

            var instance = _pool.Rent();

            Assert.IsNotNull(instance);
            Assert.AreNotSame(_prefab, instance);
            Assert.AreEqual(1, _pool.CountAll);
        }

        [Test]
        public void Rent_AfterReturn_ReturnsSameInstanceReference()
        {
            _pool.Initialize(_prefab, 0);
            var first = _pool.Rent();
            _pool.Return(first);

            var second = _pool.Rent();

            Assert.AreSame(first, second);
        }

        [Test]
        public void Rent_AfterReturn_DoesNotIncreaseTotalObjectCount()
        {
            _pool.Initialize(_prefab, 0);
            _pool.Return(_pool.Rent());

            _pool.Rent();

            Assert.AreEqual(1, _pool.CountAll);
        }

        [Test]
        public void Return_Instance_DeactivatesGameObject()
        {
            _pool.Initialize(_prefab, 0);
            var instance = _pool.Rent();

            _pool.Return(instance);

            Assert.IsFalse(instance.activeSelf);
        }

        [Test]
        public void Rent_ReusedInstance_IsActiveAgain()
        {
            _pool.Initialize(_prefab, 0);
            _pool.Return(_pool.Rent());

            var reused = _pool.Rent();

            Assert.IsTrue(reused.activeSelf);
        }

        [Test]
        public void Initialize_WithPrewarm_CreatesInstancesUpFront()
        {
            _pool.Initialize(_prefab, 3);

            Assert.AreEqual(3, _pool.CountAll);
            Assert.AreEqual(0, _pool.CountActive);
        }

        [Test]
        public void Initialize_WithPrewarm_LeavesInstancesInactive()
        {
            _pool.Initialize(_prefab, 1);

            var instance = _pool.Rent();

            Assert.IsTrue(instance.activeSelf, "대여한 뒤에는 활성이어야 한다");
            _pool.Return(instance);
            Assert.IsFalse(instance.activeSelf, "반납하면 비활성이어야 한다");
        }
    }
}
