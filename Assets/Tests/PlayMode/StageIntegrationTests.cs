using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Stages;
using UnityEngine;
using UnityEngine.TestTools;

namespace SushiDefense.Tests.PlayMode
{
    /// <summary>
    /// M1 완료 판정의 통합 검증. 씬 애셋에 의존하지 않고 같은 구성을 코드로 세운다 —
    /// 씬 파일에 묶이면 씬을 손볼 때마다 테스트가 깨진다.
    /// </summary>
    public sealed class StageIntegrationTests
    {
        private const float Speed = 10f;
        private const float Interval = 0.2f;
        private const float Length = 30f;
        private const float Reach = 8f;
        private const float TableBeltPosition = 12f;

        private readonly List<GameObject> _objects = new();

        /// <summary>
        /// 이 테스트가 만든 모든 스테이지 설정. 스테이지 교체 테스트가 <c>_config</c> 를
        /// 갈아 끼우므로 버려진 것도 여기 남아야 정리된다 — <c>ScriptableObject</c> 는
        /// 스코프를 벗어나도 사라지지 않는다.
        /// </summary>
        private readonly List<StageConfig> _configs = new();

        private SushiData _sushiData;
        private CustomerData _customerData;
        private StageConfig _config;
        private SushiPoolBehaviour _pool;
        private SushiBeltView _beltView;
        private TableSlotView _slot;
        private StageBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _sushiData = ScriptableObject.CreateInstance<SushiData>();
            _customerData = ScriptableObject.CreateInstance<CustomerData>();
            SetCustomerStats(reach: Reach, maxSaturation: 99);
            _config = NewStageConfig();

            var prefab = NewObject("SushiPrefab");
            prefab.AddComponent<SushiItemView>();
            prefab.SetActive(false);

            _pool = NewObject("SushiPool").AddComponent<SushiPoolBehaviour>();
            _pool.Initialize(prefab, 0);

            var beltStart = NewObject("BeltStart").transform;
            var beltEnd = NewObject("BeltEnd").transform;
            beltEnd.position = new Vector3(30f, 0f, 0f);

            _beltView = NewObject("BeltView").AddComponent<SushiBeltView>();

            var seatVisual = NewObject("SeatVisual").AddComponent<CustomerView>();
            _slot = NewObject("TableSlot").AddComponent<TableSlotView>();
            _slot.Initialize(slotIndex: 0, TableBeltPosition, seatVisual);

            var placement = NewObject("Placement").AddComponent<CustomerPlacementController>();

            _bootstrap = NewObject("StageBootstrap").AddComponent<StageBootstrap>();
            _bootstrap.Initialize(_config, _pool, _beltView, beltStart, beltEnd,
                                  placement, new[] { _slot }, new[] { _customerData });
            _bootstrap.Build();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var target in _objects)
            {
                Object.DestroyImmediate(target);
            }

            _objects.Clear();
            Object.DestroyImmediate(_sushiData);
            Object.DestroyImmediate(_customerData);

            foreach (var config in _configs)
            {
                Object.DestroyImmediate(config);
            }

            _configs.Clear();
        }

        [UnityTest]
        public IEnumerator Play_SushiSpawnsAndMovesAlongBelt()
        {
            // 스폰 간격(0.2초)보다 넉넉히 기다린다 — 프레임 하나로는 간격이 안 찬다.
            yield return WaitSeconds(Interval * 3f);

            Assert.IsNotEmpty(_bootstrap.Belt.ActiveSushi, "재생하면 초밥이 나온다");

            var sushi = _bootstrap.Belt.ActiveSushi[0];
            var before = sushi.BeltPosition;
            yield return null;

            Assert.Greater(sushi.BeltPosition, before, "초밥이 라인을 따라 이동한다");
        }

        [UnityTest]
        public IEnumerator Play_SushiReachingEnd_ReturnsToPoolNotDestroyed()
        {
            yield return WaitSeconds(Interval * 3f);
            var firstInstance = _pool.transform.GetComponentInChildren<SushiItemView>(true).gameObject;

            // 벨트 길이 30 / 속도 10 → 통과에 3초. 첫 초밥이 끝점을 지나고도 남게 기다린다.
            yield return WaitSeconds(4f);
            var afterFirstWave = _pool.CountAll;

            // 정상 상태(벨트 위 동시 존재 수)에 이른 뒤로는 반납분이 재사용되므로
            // 인스턴스가 더 늘지 않아야 한다. 파괴 후 재생성이라면 여기서 계속 는다.
            yield return WaitSeconds(4f);

            Assert.IsTrue(firstInstance != null, "끝점 도달은 파괴가 아니라 반납이다");
            Assert.LessOrEqual(_pool.CountAll, afterFirstWave + 1,
                               "반납분을 재사용하므로 인스턴스가 계속 늘지 않는다");
        }

        [UnityTest]
        public IEnumerator Play_CustomerPlacedOnSlot_ClaimsSushiInReach()
        {
            var claims = RecordClaims();
            Assert.IsTrue(_bootstrap.Placement.CanPlace(_customerData, _slot.SlotIndex));

            _bootstrap.Placement.Place(_customerData, _slot.SlotIndex, _slot.BeltPosition);
            yield return WaitSeconds(3f);

            Assert.IsNotEmpty(claims, "범위에 들어온 초밥을 집는다");
        }

        [UnityTest]
        public IEnumerator Play_MultipleSushiInReach_ClaimsLowestSequenceFirst()
        {
            var claims = RecordClaims();
            _bootstrap.Placement.Place(_customerData, _slot.SlotIndex, _slot.BeltPosition);

            yield return WaitSeconds(3f);

            Assert.IsNotEmpty(claims);
            for (var i = 1; i < claims.Count; i++)
            {
                Assert.Greater(claims[i], claims[i - 1],
                               "먼저 나온 초밥이 먼저 처리된다");
            }
        }

        [UnityTest]
        public IEnumerator Play_CustomerPlacedMidStage_StartsClaiming()
        {
            yield return WaitSeconds(1.5f);
            var claims = RecordClaims();
            Assert.IsEmpty(claims, "아직 손님이 없다");

            _bootstrap.Placement.Place(_customerData, _slot.SlotIndex, _slot.BeltPosition);
            yield return WaitSeconds(3f);

            Assert.IsNotEmpty(claims, "진행 중에 앉은 손님도 집기 시작한다");
        }

        [UnityTest]
        public IEnumerator Play_NoFrameWithClaimableSushiUnclaimed()
        {
            // 범위 안에 집을 수 있는 초밥을 두고 지나보내는 프레임이 0 이어야 한다
            // (CLAUDE.md §1.1-3a — 구경하는 손님은 버그다).
            var customer = _bootstrap.Placement.Place(_customerData, _slot.SlotIndex,
                                                      _slot.BeltPosition);

            for (var frame = 0; frame < 180; frame++)
            {
                yield return null;

                if (!customer.CanAcceptSushi)
                {
                    continue;
                }

                foreach (var sushi in _bootstrap.Coordinator.CandidatesOf(customer))
                {
                    Assert.AreNotEqual(SushiState.OnBelt, sushi.State,
                                       $"{frame} 프레임에서 집을 수 있는 초밥을 그냥 두었다");
                }
            }
        }

        [UnityTest]
        public IEnumerator Play_SushiEaten_AccumulatesRevenueAndCurrency()
        {
            _bootstrap.Placement.Place(_customerData, _slot.SlotIndex, _slot.BeltPosition);

            yield return WaitSeconds(4f);

            Assert.Greater(_bootstrap.Revenue.Total, 0, "먹은 초밥이 매출로 쌓인다");
            Assert.AreEqual(_bootstrap.Revenue.Total / 10, _bootstrap.Wallet.Balance,
                            "영입 재화는 매출의 1/10 이다 (초기 예산 0)");
        }

        // ── 스테이지 리셋 (착수 시 확정 — 이월 없음) ────────────

        [Test]
        public void Build_CalledTwice_ResetsRevenueAndWallet()
        {
            _bootstrap.Revenue.Add(5000);

            _bootstrap.Build();

            Assert.AreEqual(0, _bootstrap.Revenue.Total);
            Assert.AreEqual(_config.InitialRecruitBudget, _bootstrap.Wallet.Balance);
        }

        [UnityTest]
        public IEnumerator Build_CalledTwice_ResetsSequenceNumbers()
        {
            // 순차번호는 스테이지마다 0 부터다 (착수 시 확정). 이어지면 스테이지 2·3 의
            // 배정 결과를 재현하는 데 앞 스테이지 이력이 필요해진다.
            yield return WaitSeconds(Interval * 4f);
            Assert.Greater(_bootstrap.Belt.ActiveSushi[0].SequenceNumber, -1);

            _bootstrap.Build();
            yield return WaitSeconds(Interval * 1.5f);

            Assert.AreEqual(0, _bootstrap.Belt.ActiveSushi[0].SequenceNumber,
                            "새 스테이지의 첫 초밥은 0 번이다");

            var customer = _bootstrap.Placement.Place(_customerData, _slot.SlotIndex,
                                                      _slot.BeltPosition);
            Assert.AreEqual(0, customer.State.SequenceNumber,
                            "새 스테이지의 첫 손님도 0 번이다");
        }

        [UnityTest]
        public IEnumerator Play_BeltViewFollowsModel()
        {
            yield return WaitSeconds(Interval * 3f);

            var sushi = _bootstrap.Belt.ActiveSushi[0];
            var view = _pool.transform.GetComponentInChildren<SushiItemView>(true);

            Assert.AreSame(sushi, view.Model, "뷰가 모델을 따라간다");
        }

        // ── 클리어 · 재시도 (M3) ────────────────────────────────

        [UnityTest]
        public IEnumerator Play_TargetReached_StageStopsAndRevenueFreezes()
        {
            // 초밥 하나(기본 가격 100)만 먹어도 닿는 목표로 다시 세운다.
            StageConfigTestFactory.SetInt(_config, "_targetRevenue", 100);
            _bootstrap.Build();
            _bootstrap.Placement.Place(_customerData, _slot.SlotIndex, _slot.BeltPosition);

            yield return WaitUntilDecided(6f);

            Assert.AreEqual(StageOutcome.Cleared, _bootstrap.Stage.Outcome);

            var frozen = _bootstrap.Revenue.Total;
            yield return WaitSeconds(2f);

            Assert.AreEqual(frozen, _bootstrap.Revenue.Total,
                            "판정 뒤에도 벨트가 돌면 매출이 계속 오른다");
        }

        /// <summary>
        /// <b>재시도의 전부다</b> — 덱은 남고 매출은 0 이 된다. 하나만 보면 런 전체를 새로
        /// 만드는 구현도, 아무것도 리셋하지 않는 구현도 통과한다.
        /// </summary>
        [Test]
        public void Play_Retry_KeepsDeckAndResetsRevenue()
        {
            var reward = ScriptableObject.CreateInstance<SushiData>();
            try
            {
                Assert.IsTrue(_bootstrap.Run.Sushi.TryAdd(reward));
                _bootstrap.Revenue.Add(500);

                _bootstrap.Retry();

                Assert.IsTrue(_bootstrap.Run.Sushi.Contains(reward), "재시도에 덱이 사라졌다");
                Assert.AreEqual(0, _bootstrap.Revenue.Total);
            }
            finally
            {
                Object.DestroyImmediate(reward);
            }
        }

        [Test]
        public void Play_Retry_IncrementsAttemptAndKeepsStageNumber()
        {
            _bootstrap.Retry();

            Assert.AreEqual(1, _bootstrap.Run.StageNumber, "실패해도 같은 스테이지에 머문다");
            Assert.AreEqual(2, _bootstrap.Run.AttemptNumber);
        }

        [Test]
        public void Build_CalledTwice_KeepsTheSameRun()
        {
            var before = _bootstrap.Run;

            _bootstrap.Build();

            Assert.AreSame(before, _bootstrap.Run, "런은 Build() 바깥에 산다");
        }

        // ── 자리 정의 바인딩 (step-07) ──────────────────────────
        //
        // 이 단계 전까지 StageConfig.TableSlots 는 **프로덕션 코드에서 아무도 읽지 않았다.**
        // 자리 좌표는 씬의 TableSlotView 에 손으로 박혀 있었고, 스테이지 1 에서는 우연히
        // 값이 같아 문제가 없었다. 스테이지 2·3 이 자리 4개를 쓰는 순간 "설정을 고쳐도
        // 화면이 안 바뀌는" 상태가 된다.

        /// <summary>
        /// 씬에 박힌 값과 <b>다른 값</b>을 정의로 준다. 같은 값을 주면 바인딩을 아예 안 하는
        /// 구현도 통과한다. <c>SlotIndex</c> 도 함께 보는 이유는, 벨트 좌표만 맞고 인덱스가
        /// 어긋난 채로 배정이 도는 상태가 숨기 때문이다.
        /// </summary>
        [Test]
        public void Build_ConfigWithThreeSlotDefinitions_BindsBeltPositions()
        {
            var slots = NewSlots(3);
            DefineSlots(3);

            RebuildWith(slots);

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(DefinedIndex(i), slots[i].SlotIndex, $"{i}번 자리의 번호");
                Assert.AreEqual(DefinedBeltPosition(i), slots[i].BeltPosition, $"{i}번 자리의 벨트 좌표");
            }
        }

        [Test]
        public void Build_ConfigWithFewerSlotsThanScene_DeactivatesExtras()
        {
            var slots = NewSlots(3);
            DefineSlots(2);

            RebuildWith(slots);

            Assert.IsTrue(slots[0].gameObject.activeSelf);
            Assert.IsTrue(slots[1].gameObject.activeSelf);
            Assert.IsFalse(slots[2].gameObject.activeSelf, "정의에 없는 자리는 꺼진다");
        }

        /// <summary>
        /// <b>빈 정의는 no-op 이다.</b> 자리 정의 없이 세우는 하네스가 이 파일의 다른 테스트
        /// 전부를 포함해 살아 있는 이유이며, 여기서 전부 꺼 버리면 그 구성이 통째로 죽는다.
        /// </summary>
        [Test]
        public void Build_ConfigWithNoSlotDefinitions_LeavesSceneSlotsUntouched()
        {
            var slots = NewSlots(3);

            RebuildWith(slots);

            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(SceneIndex(i), slots[i].SlotIndex, "씬에 박힌 번호가 그대로여야 한다");
                Assert.AreEqual(SceneBeltPosition(i), slots[i].BeltPosition);
                Assert.IsTrue(slots[i].gameObject.activeSelf);
            }
        }

        /// <summary>
        /// 스테이지 교체의 핵심 — <b>줄이는 방향과 늘리는 방향을 모두</b> 본다. 한 방향만
        /// 보면 비활성화만 하고 다시 켜지 않는 구현이 통과한다.
        /// </summary>
        [Test]
        public void Build_Rebuilt_WithDifferentSlotCount_UpdatesActiveSlots()
        {
            var slots = NewSlots(4);
            DefineSlots(4);
            RebuildWith(slots);
            Assert.IsTrue(slots[3].gameObject.activeSelf, "전제가 깨졌다 — 4자리 구성에서 전부 켜져 있어야 한다");

            _config = NewStageConfig();
            DefineSlots(2);
            RebuildWith(slots);

            Assert.IsFalse(slots[2].gameObject.activeSelf);
            Assert.IsFalse(slots[3].gameObject.activeSelf);

            _config = NewStageConfig();
            DefineSlots(4);
            RebuildWith(slots);

            Assert.IsTrue(slots[2].gameObject.activeSelf, "다시 늘어난 자리가 켜져야 한다");
            Assert.IsTrue(slots[3].gameObject.activeSelf);
        }

        /// <summary>씬에 손으로 박아 둔 자리를 만든다. 정의값과 <b>겹치지 않는</b> 값을 쓴다.</summary>
        private TableSlotView[] NewSlots(int count)
        {
            var slots = new TableSlotView[count];
            for (var i = 0; i < count; i++)
            {
                var seat = NewObject($"Seat{i}").AddComponent<CustomerView>();
                var slot = NewObject($"Slot{i}").AddComponent<TableSlotView>();
                slot.Initialize(SceneIndex(i), SceneBeltPosition(i), seat);
                slots[i] = slot;
            }

            return slots;
        }

        /// <summary>
        /// 스테이지를 갈아 끼우는 테스트가 쓸 새 설정. 버린 설정도 <c>TearDown</c> 이
        /// 정리하도록 모아 둔다 — <c>ScriptableObject</c> 는 스코프를 벗어나도 사라지지 않는다.
        /// </summary>
        private StageConfig NewStageConfig()
        {
            var created = StageConfigTestFactory.Create(Speed, Interval, Length, _sushiData);
            _configs.Add(created);
            return created;
        }

        private void DefineSlots(int count)
        {
            for (var i = 0; i < count; i++)
            {
                StageConfigTestFactory.AddTableSlot(_config, DefinedIndex(i), DefinedBeltPosition(i),
                                                    new Vector2(i, -2f));
            }
        }

        private void RebuildWith(TableSlotView[] slots)
        {
            var placement = NewObject("PlacementForSlots").AddComponent<CustomerPlacementController>();
            var beltStart = NewObject("BeltStartForSlots").transform;
            var beltEnd = NewObject("BeltEndForSlots").transform;

            _bootstrap.Initialize(_config, _pool, _beltView, beltStart, beltEnd,
                                  placement, slots, new[] { _customerData });
            _bootstrap.Build();
        }

        private static int SceneIndex(int i) => 100 + i;

        private static float SceneBeltPosition(int i) => 1f + i;

        private static int DefinedIndex(int i) => i;

        private static float DefinedBeltPosition(int i) => 20f + i * 4f;

        private IEnumerator WaitUntilDecided(float timeoutSeconds)
        {
            var elapsed = 0f;
            while (elapsed < timeoutSeconds && _bootstrap.Stage.IsRunning)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsFalse(_bootstrap.Stage.IsRunning, "제한 시간 안에 판정이 나지 않았다");
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private List<int> RecordClaims()
        {
            var claims = new List<int>();
            _bootstrap.Coordinator.SushiClaimed += (_, sushi) => claims.Add(sushi.SequenceNumber);
            return claims;
        }

        private void SetCustomerStats(float reach, int maxSaturation)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(_customerData);
            serialized.FindProperty("_reach").floatValue = reach;
            serialized.FindProperty("_maxSaturation").intValue = maxSaturation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        private GameObject NewObject(string name)
        {
            var created = new GameObject(name);
            _objects.Add(created);
            return created;
        }
    }
}
