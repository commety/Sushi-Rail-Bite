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
            _config = StageConfigTestFactory.Create(Speed, Interval, Length, _sushiData);

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
            Object.DestroyImmediate(_config);
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
