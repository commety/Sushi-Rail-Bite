using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
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
                                  placement, new[] { _slot }, _customerData);
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
        public IEnumerator Play_BeltViewFollowsModel()
        {
            yield return WaitSeconds(Interval * 3f);

            var sushi = _bootstrap.Belt.ActiveSushi[0];
            var view = _pool.transform.GetComponentInChildren<SushiItemView>(true);

            Assert.AreSame(sushi, view.Model, "뷰가 모델을 따라간다");
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
