using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Effects;
using SushiDefense.Run;
using SushiDefense.Scoring;
using SushiDefense.Stages;
using UnityEngine;
using UnityEngine.TestTools;

namespace SushiDefense.Tests.PlayMode.Effects
{
    /// <summary>
    /// 이펙트가 <b>몇 개 · 어디에</b> 뜨는지, 그리고 <b>풀을 경유하는지</b>를 본다.
    /// 풀 로직 자체는 EditMode 의 <c>SushiPoolTests</c> 가 이미 덮으므로 여기서 다시
    /// 검증하지 않는다 (<c>.claude/rules/tests.md</c> §1).
    /// </summary>
    public sealed class EffectDirectorTests
    {
        private const float Lifetime = 0.05f;
        private static readonly Vector3 SeatPosition = new(7f, 2f, 0f);

        private readonly List<Object> _created = new();

        private EffectDirector _director;
        private SushiPoolBehaviour _pool;
        private ClaimCoordinator _coordinator;
        private StageController _stage;
        private CustomerLogic _customer;
        private SushiItem _sushi;
        private TableSlotView _slot;

        [SetUp]
        public void SetUp()
        {
            var prefab = NewObject("PopEffect");
            prefab.AddComponent<SpriteRenderer>();
            var view = prefab.AddComponent<PopEffectView>();
            SetField(view, "_lifetimeSeconds", Lifetime);
            prefab.SetActive(false);

            var poolHost = NewObject("EffectPool");
            _pool = poolHost.AddComponent<SushiPoolBehaviour>();
            _pool.Initialize(prefab, 0);

            var stageConfig = StageConfigTestFactory.Create(10f, 1f, 100f, NewSushiData());
            _created.Add(stageConfig);

            var belt = new SushiBelt(stageConfig, SushiDeck.FromSpawnTable(stageConfig).Cards,
                                     new SequenceNumberIssuer(),
                                     new SushiPool<SushiItem>(new SushiItemFactory()));
            var revenue = new RevenueLedger();
            _coordinator = new ClaimCoordinator(belt, stageConfig, revenue, new RecruitWallet(0));
            _stage = new StageController(_coordinator, revenue, stageConfig);

            var customerData = ScriptableObject.CreateInstance<CustomerData>();
            _created.Add(customerData);
            _customer = new CustomerLogic(new CustomerRuntimeState(customerData, 0), 0f);
            _sushi = new SushiItem(NewSushiData(), 0);

            _slot = NewSeat();

            var go = NewObject("EffectDirector");
            _director = go.AddComponent<EffectDirector>();
            SetField(_director, "_pool", _pool);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
            {
                Object.DestroyImmediate(o);
            }

            _created.Clear();
        }

        [Test]
        public void SushiEaten_Once_SpawnsOneEffect()
        {
            Bind();

            RaiseEaten();

            Assert.AreEqual(1, _director.SpawnedCount);
            Assert.AreEqual(1, _pool.CountActive);
        }

        [Test]
        public void SushiEaten_Twice_SpawnsTwoEffects()
        {
            Bind();

            RaiseEaten();
            RaiseEaten();

            Assert.AreEqual(2, _director.SpawnedCount);
        }

        [Test]
        public void SushiEaten_SpawnsAtOccupiedSeat()
        {
            // 초밥은 이 시점에 이미 벨트에서 빠졌다. 플레이어가 봐야 할 것은 "누가 먹었나" 다.
            Bind();

            RaiseEaten();

            Assert.AreEqual(SeatPosition, ActiveEffect().transform.position);
        }

        [Test]
        public void Bind_Twice_DoesNotDoubleSpawn()
        {
            // Build() 는 스테이지가 넘어갈 때마다 다시 불린다.
            Bind();
            Bind();

            RaiseEaten();

            Assert.AreEqual(1, _director.SpawnedCount);
        }

        [Test]
        public void Unbind_ThenEaten_DoesNotSpawn()
        {
            Bind();

            _director.Unbind();
            RaiseEaten();

            Assert.AreEqual(0, _director.SpawnedCount);
        }

        [Test]
        public void StageCleared_SpawnsEffect()
        {
            Bind();

            Raise(_stage, "OutcomeDecided", StageOutcome.Cleared);

            Assert.Greater(_director.SpawnedCount, 0);
        }

        [Test]
        public void Bind_WithoutPool_DoesNotThrow()
        {
            // 이펙트는 로직의 전제 조건이 아니다.
            var bare = NewObject("EffectDirector_NoPool").AddComponent<EffectDirector>();

            Assert.DoesNotThrow(() =>
            {
                bare.Bind(_coordinator, _stage, new[] { _slot });
                RaiseEaten();
            });
        }

        [UnityTest]
        public IEnumerator Play_AfterLifetime_ReturnsToPool()
        {
            Bind();
            RaiseEaten();

            yield return new WaitForSeconds(Lifetime * 3f);

            Assert.AreEqual(0, _pool.CountActive, "수명이 끝나면 풀로 돌아가야 한다");
            Assert.AreEqual(1, _pool.CountAll, "반납은 파괴가 아니다");
        }

        [UnityTest]
        public IEnumerator Play_AfterReturn_ReusesSameInstance()
        {
            // 풀을 경유하는지 확인하는 지점이다. CountAll 이 늘지 않았다는 것만으로는
            // 상수를 돌려주는 구현에서도 통과한다 — 인스턴스 참조가 같아야 한다.
            Bind();
            RaiseEaten();
            var first = ActiveEffect();

            yield return new WaitForSeconds(Lifetime * 3f);
            RaiseEaten();

            // 인스턴스 수를 함께 본다. 참조만 비교하면 반납이 안 된 채 두 개가 떠 있어도
            // "첫 번째 활성" 이 여전히 first 라 통과한다 — 실제로 주입해 보고 알았다.
            Assert.AreEqual(1, _pool.CountAll, "재사용했다면 인스턴스가 늘지 않는다");
            Assert.AreSame(first, ActiveEffect());
        }

        private void Bind()
        {
            _director.Bind(_coordinator, _stage, new[] { _slot });
        }

        private void RaiseEaten()
        {
            Raise(_coordinator, "SushiEaten", _customer, _sushi);
        }

        private GameObject ActiveEffect()
        {
            foreach (var view in _pool.GetComponentsInChildren<PopEffectView>(true))
            {
                if (view.gameObject.activeSelf)
                {
                    return view.gameObject;
                }
            }

            Assert.Fail("떠 있는 이펙트가 없다");
            return null;
        }

        private TableSlotView NewSeat()
        {
            var seatGo = NewObject("Seat");
            seatGo.AddComponent<SpriteRenderer>();
            var seatVisual = seatGo.AddComponent<CustomerView>();

            var slotGo = NewObject("TableSlot");
            slotGo.transform.position = SeatPosition;
            var slot = slotGo.AddComponent<TableSlotView>();
            slot.Initialize(0, 0f, seatVisual);
            slot.Occupy(_customer, _coordinator);
            return slot;
        }

        private static void Raise(object target, string eventName, params object[] args)
        {
            var field = target.GetType().GetField(
                eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"이벤트 백킹 필드 '{eventName}' 을 찾지 못했습니다.");

            ((System.Delegate)field.GetValue(target))?.DynamicInvoke(args);
        }

        private static void SetField(Object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"직렬화 필드 '{name}' 을 찾지 못했습니다.");
            field.SetValue(target, value);
        }

        private SushiData NewSushiData()
        {
            var data = ScriptableObject.CreateInstance<SushiData>();
            _created.Add(data);
            return data;
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }
    }
}
