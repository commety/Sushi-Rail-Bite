using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Scoring;
using SushiDefense.Stages;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Customers
{
    public sealed class CustomerPlacementServiceTests
    {
        private const float Interval = 1f;
        private const float Speed = 10f;
        private const float Length = 100f;
        private const float Reach = 12f;

        /// <summary>비용 검사가 끼어들지 않을 만큼 넉넉한 예산. 배치 규칙만 보는 테스트용.</summary>
        private const int AmpleBudget = 100000;

        /// <summary>스폰 지점(0)이 이미 집기 범위 안인 자리.</summary>
        private const float NearTable = 2f;

        private readonly List<Object> _disposables = new();

        private StageConfig _config;
        private CustomerData _customerData;
        private SushiBelt _belt;
        private ClaimCoordinator _coordinator;
        private RecruitWallet _wallet;
        private PauseState _pause;
        private CustomerPlacementService _service;
        private List<int> _claimedByCustomer;

        [SetUp]
        public void SetUp()
        {
            _customerData = ScriptableObject.CreateInstance<CustomerData>();
            SerializedFieldSetter.SetFloat(_customerData, "_reach", Reach);
            SerializedFieldSetter.SetInt(_customerData, "_maxSaturation", 5);

            // 덱 가격(SushiData 기본값 = 하한 100)을 품는 대역. 이 파일은 **배치**를
            // 검증하므로 즉시 확정이 전제다. 대역 밖으로 두면 배정이 이탈 직전까지
            // 밀려, 배치가 실패한 건지 아직 기다리는 건지 구분되지 않는다.
            SerializedFieldSetter.SetTargetingBand(_customerData, 100, 300);

            Rebuild(maxPlacedCustomers: 4);
        }

        [TearDown]
        public void TearDown()
        {
            _coordinator.Dispose();
            Object.DestroyImmediate(_customerData);
            Object.DestroyImmediate(_config);

            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        // ── 배치 가능 판정 ──────────────────────────────────────

        [Test]
        public void CanPlace_EmptySlot_ReturnsTrue()
        {
            Assert.IsTrue(_service.CanPlace(_customerData, 0));
        }

        /// <summary>
        /// <b>멈춘 판에는 앉히지 못한다.</b> 메뉴를 열어 놓고 손님을 앉힐 수 있으면 제한
        /// 시간을 세워 둔 채 판을 짜는 것이 되고, 실패 창 위에서도 앉을 수 있으면 끝난
        /// 판에서 영입 재화가 나간다 — 둘 다 실플레이에서 나왔다.
        ///
        /// <para>
        /// <b>«멈추면 false» 만 보지 않는다.</b> 그것만 보면 항상 <c>false</c> 를 돌려주는
        /// 구현도 통과한다 — 같은 자리·같은 손님으로 <b>멈추기 전에는 참</b>임을 함께 박는다.
        /// </para>
        /// </summary>
        [Test]
        public void CanPlace_WhilePaused_ReturnsFalse()
        {
            Assert.IsTrue(_service.CanPlace(_customerData, 0), "전제: 멈추기 전에는 놓을 수 있다");

            _pause.Pause();

            Assert.IsFalse(_service.CanPlace(_customerData, 0));
        }

        /// <summary>다시 흐르면 되돌아온다 — 멈춤은 상태이지 소모가 아니다.</summary>
        [Test]
        public void CanPlace_AfterResume_ReturnsTrueAgain()
        {
            _pause.Pause();
            _pause.Resume();

            Assert.IsTrue(_service.CanPlace(_customerData, 0));
        }

        /// <summary>
        /// 확정 경로도 함께 막힌다. <see cref="CustomerPlacementService.CanPlace"/> 만 고치고
        /// <c>TryPlace</c> 가 다른 길로 가면 화면만 «못 놓는 것처럼» 보이고 실제로는 앉는다.
        /// </summary>
        [Test]
        public void TryPlace_WhilePaused_PlacesNothing()
        {
            _pause.Pause();

            Assert.IsNull(_service.TryPlace(_customerData, 0, NearTable));
            Assert.AreEqual(0, _service.PlacedCount);
        }

        [Test]
        public void CanPlace_OccupiedSlot_ReturnsFalse()
        {
            _service.Place(_customerData, 0, NearTable);

            Assert.IsFalse(_service.CanPlace(_customerData, 0));
        }

        [Test]
        public void CanPlace_AtMaxPlacedCustomers_ReturnsFalse()
        {
            Rebuild(maxPlacedCustomers: 2);
            _service.Place(_customerData, 0, NearTable);
            _service.Place(_customerData, 1, NearTable);

            Assert.IsFalse(_service.CanPlace(_customerData, 2));
        }

        [Test]
        public void CanPlace_AfterRemove_ReturnsTrueAgain()
        {
            _service.Place(_customerData, 0, NearTable);
            _service.Remove(0);

            Assert.IsTrue(_service.CanPlace(_customerData, 0));
        }

        // ── 한도는 머릿수가 아니라 인구수를 센다 ────────────────
        //
        // 인구수가 전부 1이면 새 식(합계 + 인구수 ≤ 상한)과 옛 식(머릿수 < 상한)이
        // 정확히 같다. 위쪽 테스트들이 전부 통과한 채로 규칙이 바뀌므로, 인구수 2를
        // 실제로 쓰는 아래 테스트들이 없으면 아무것도 검증되지 않는다.

        [Test]
        public void CanPlace_PopulationTwoWithTwoHeadroom_ReturnsTrue()
        {
            Rebuild(maxPlacedCustomers: 2);

            Assert.IsTrue(_service.CanPlace(HeavyCustomer(), 0));
        }

        [Test]
        public void CanPlace_PopulationTwoWithOneHeadroom_ReturnsFalse()
        {
            // 잔액은 넉넉하고 자리도 비어 있다. 막는 것이 한도임을 확정한다.
            Rebuild(maxPlacedCustomers: 2);
            _service.Place(_customerData, 0, NearTable);

            var heavy = HeavyCustomer();

            Assert.IsFalse(_service.CanPlace(heavy, 1), "한도 2에 인구수 1+2 는 넘는다");
            Assert.IsNull(_service.OccupantOf(1), "자리는 비어 있다 — 점유가 막은 것이 아니다");
            Assert.IsTrue(_wallet.CanAfford(heavy.RecruitCost), "잔액이 막은 것도 아니다");
        }

        [Test]
        public void CanPlace_PopulationOneWithOneHeadroom_ReturnsTrue()
        {
            // 반례. 위 테스트가 «자리가 하나 남으면 무조건 거부» 로도 통과하지 않게 한다.
            Rebuild(maxPlacedCustomers: 2);
            _service.Place(_customerData, 0, NearTable);

            Assert.IsTrue(_service.CanPlace(_customerData, 1));
        }

        [Test]
        public void PlacedPopulation_TwoOnesAndOneTwo_ReturnsFour()
        {
            // 구체값을 박는다. 상수 3(머릿수)을 돌려주는 구현이 통과하지 못한다.
            Rebuild(maxPlacedCustomers: 10);
            _service.Place(_customerData, 0, NearTable);
            _service.Place(_customerData, 1, NearTable);
            _service.Place(HeavyCustomer(), 2, NearTable);

            Assert.AreEqual(4, _service.PlacedPopulation);
        }

        [Test]
        public void PlacedCount_PopulationTwoPlaced_StillCountsHeads()
        {
            // 머릿수를 세던 자리는 그대로다. AudioDirector 가 이 값의 증가로 배치음을
            // 내는데, 의미를 갈아끼우면 소리는 그대로 한 번 나서 아무 테스트도 죽지 않는다.
            Rebuild(maxPlacedCustomers: 10);
            _service.Place(HeavyCustomer(), 0, NearTable);

            Assert.AreEqual(1, _service.PlacedCount);
            Assert.AreEqual(2, _service.PlacedPopulation);
        }

        [Test]
        public void Remove_PopulationTwo_FreesTwo()
        {
            Rebuild(maxPlacedCustomers: 2);
            _service.Place(HeavyCustomer(), 0, NearTable);

            _service.Remove(0);

            Assert.AreEqual(0, _service.PlacedPopulation);
            Assert.IsTrue(_service.CanPlace(HeavyCustomer(), 1), "둘이 통째로 돌아와야 한다");
        }

        // ── 배치 확정 ───────────────────────────────────────────

        [Test]
        public void Place_EmptySlot_ReturnsCustomerWithSequenceNumber()
        {
            var customer = _service.Place(_customerData, 0, NearTable);

            Assert.IsNotNull(customer);
            Assert.AreEqual(0, customer.State.SequenceNumber);
            Assert.AreEqual(NearTable, customer.BeltPosition);
            Assert.AreEqual(1, _service.PlacedCount);
        }

        [Test]
        public void Place_Twice_AssignsIncreasingCustomerSequenceNumbers()
        {
            var first = _service.Place(_customerData, 0, NearTable);
            var second = _service.Place(_customerData, 1, NearTable);

            Assert.AreEqual(0, first.State.SequenceNumber);
            Assert.AreEqual(1, second.State.SequenceNumber);
        }

        [Test]
        public void Place_OccupiedSlot_Throws()
        {
            _service.Place(_customerData, 0, NearTable);

            Assert.Throws<System.InvalidOperationException>(
                () => _service.Place(_customerData, 0, NearTable));
        }

        [Test]
        public void Place_BeyondMax_Throws()
        {
            Rebuild(maxPlacedCustomers: 1);
            _service.Place(_customerData, 0, NearTable);

            Assert.Throws<System.InvalidOperationException>(
                () => _service.Place(_customerData, 1, NearTable));
        }

        [Test]
        public void Place_RegistersWithCoordinator()
        {
            var customer = _service.Place(_customerData, 0, NearTable);

            Assert.Contains(customer, (System.Collections.ICollection)_coordinator.Customers);
        }

        [Test]
        public void TryPlace_OccupiedSlot_ReturnsNullWithoutThrowing()
        {
            _service.Place(_customerData, 0, NearTable);

            Assert.IsNull(_service.TryPlace(_customerData, 0, NearTable));
        }

        [Test]
        public void TryPlace_EmptySlot_PlacesCustomer()
        {
            Assert.IsNotNull(_service.TryPlace(_customerData, 0, NearTable));
        }

        // ── 진행 중 배치 (Q4) ───────────────────────────────────

        [Test]
        public void Place_MidStage_ParticipatesInNextResolve()
        {
            _coordinator.Tick(Interval);
            _coordinator.Tick(Interval);
            Assert.IsEmpty(_claimedByCustomer, "아직 손님이 없다");

            var latecomer = _service.Place(_customerData, 0, NearTable);
            _coordinator.Tick(Interval);

            Assert.IsNotEmpty(_claimedByCustomer);
            Assert.AreEqual(latecomer.State.SequenceNumber, _claimedByCustomer[0]);
        }

        [Test]
        public void Place_MidStage_DoesNotReassignExistingClaims()
        {
            var veteran = _service.Place(_customerData, 0, NearTable);
            _coordinator.Tick(Interval);
            var claimsBefore = new List<int>(_claimedByCustomer);

            _service.Place(_customerData, 1, NearTable);
            _coordinator.Tick(Interval);

            for (var i = 0; i < claimsBefore.Count; i++)
            {
                Assert.AreEqual(claimsBefore[i], _claimedByCustomer[i],
                                "이미 확정된 배정이 바뀌면 안 된다");
            }

            Assert.AreEqual(veteran.State.SequenceNumber, _claimedByCustomer[0]);
        }

        // ── 영입 비용 · 잔액 ────────────────────────────────────

        [Test]
        public void Place_SufficientCurrency_DeductsRecruitCost()
        {
            Rebuild(maxPlacedCustomers: 4, initialBudget: 100);
            SerializedFieldSetter.SetInt(_customerData, "_recruitCost", 30);

            _service.Place(_customerData, 0, NearTable);

            Assert.AreEqual(70, _wallet.Balance);
        }

        [Test]
        public void Place_InsufficientCurrency_Rejected()
        {
            Rebuild(maxPlacedCustomers: 4, initialBudget: 10);
            SerializedFieldSetter.SetInt(_customerData, "_recruitCost", 50);

            Assert.IsFalse(_service.CanPlace(_customerData, 0));
            Assert.Throws<System.InvalidOperationException>(
                () => _service.Place(_customerData, 0, NearTable));
        }

        [Test]
        public void TryPlace_InsufficientCurrency_ReturnsNullAndKeepsBalance()
        {
            Rebuild(maxPlacedCustomers: 4, initialBudget: 10);
            SerializedFieldSetter.SetInt(_customerData, "_recruitCost", 50);

            Assert.IsNull(_service.TryPlace(_customerData, 0, NearTable));
            Assert.AreEqual(10, _wallet.Balance);
            Assert.AreEqual(0, _service.PlacedCount);
        }

        [Test]
        public void TryPlace_InsufficientCurrency_DoesNotConsumeSequenceNumber()
        {
            // 순서 계약 — 잔액 차감이 번호 발급보다 먼저다. 번호를 먼저 발급하면
            // 실패한 배치가 순차번호를 태워 이후 배정 결과가 달라진다.
            Rebuild(maxPlacedCustomers: 4, initialBudget: 50);
            SerializedFieldSetter.SetInt(_customerData, "_recruitCost", 999);
            _service.TryPlace(_customerData, 0, NearTable);

            SerializedFieldSetter.SetInt(_customerData, "_recruitCost", 0);
            var placed = _service.Place(_customerData, 1, NearTable);

            Assert.AreEqual(0, placed.State.SequenceNumber, "실패한 배치가 번호를 태우면 안 된다");
        }

        [Test]
        public void Place_ExactBalance_Succeeds()
        {
            Rebuild(maxPlacedCustomers: 4, initialBudget: 50);
            SerializedFieldSetter.SetInt(_customerData, "_recruitCost", 50);

            Assert.IsNotNull(_service.Place(_customerData, 0, NearTable));
            Assert.AreEqual(0, _wallet.Balance);
        }

        [Test]
        public void Place_ZeroCost_SucceedsWithEmptyWallet()
        {
            Rebuild(maxPlacedCustomers: 4, initialBudget: 0);
            SerializedFieldSetter.SetInt(_customerData, "_recruitCost", 0);

            Assert.IsNotNull(_service.Place(_customerData, 0, NearTable));
        }

        [Test]
        public void CanPlace_InsufficientCurrency_DoesNotDeduct()
        {
            // 검사에 부수효과가 없다. 있으면 TryPlace 가 두 번 차감한다.
            Rebuild(maxPlacedCustomers: 4, initialBudget: 40);
            SerializedFieldSetter.SetInt(_customerData, "_recruitCost", 30);

            _service.CanPlace(_customerData, 0);
            _service.CanPlace(_customerData, 0);

            Assert.AreEqual(40, _wallet.Balance);
        }

        [Test]
        public void Remove_PlacedCustomer_DoesNotRefund()
        {
            // 환불하지 않는다. 배치·해제를 반복해 재화를 되찾는 경로를 만들지 않는다.
            Rebuild(maxPlacedCustomers: 4, initialBudget: 100);
            SerializedFieldSetter.SetInt(_customerData, "_recruitCost", 30);
            _service.Place(_customerData, 0, NearTable);

            _service.Remove(0);

            Assert.AreEqual(70, _wallet.Balance);
        }

        // ── 배치 취소 ───────────────────────────────────────────

        [Test]
        public void Remove_PlacedSlot_FreesSlotAndStopsClaiming()
        {
            _service.Place(_customerData, 0, NearTable);

            var removed = _service.Remove(0);
            _coordinator.Tick(Interval);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, _service.PlacedCount);
            Assert.IsEmpty(_coordinator.Customers);
            Assert.IsEmpty(_claimedByCustomer);
        }

        [Test]
        public void Remove_EmptySlot_ReturnsFalse()
        {
            Assert.IsFalse(_service.Remove(3));
        }

        [Test]
        public void OccupantOf_PlacedSlot_ReturnsCustomer()
        {
            var customer = _service.Place(_customerData, 2, NearTable);

            Assert.AreSame(customer, _service.OccupantOf(2));
            Assert.IsNull(_service.OccupantOf(0));
        }

        /// <summary>
        /// 인구수 2인 손님. <see cref="_customerData"/> 를 재활용하지 않는 이유는 그것이
        /// 스위트 전체가 공유하는 «인구수 1» 대조군이기 때문이다 — 값을 바꾸면 위쪽
        /// 테스트들이 조용히 다른 것을 검증하게 된다.
        /// </summary>
        private CustomerData HeavyCustomer()
        {
            var data = ScriptableObject.CreateInstance<CustomerData>();
            SerializedFieldSetter.SetFloat(data, "_reach", Reach);
            SerializedFieldSetter.SetInt(data, "_maxSaturation", 5);
            SerializedFieldSetter.SetTargetingBand(data, 100, 300);
            SerializedFieldSetter.SetInt(data, "_population", 2);

            _disposables.Add(data);
            return data;
        }

        private void Rebuild(int maxPlacedCustomers, int initialBudget = AmpleBudget)
        {
            _coordinator?.Dispose();
            if (_config != null)
            {
                Object.DestroyImmediate(_config);
            }

            _config = new StageConfigBuilder()
                .WithBeltSpeed(Speed)
                .WithSpawnInterval(Interval)
                .WithBeltLength(Length)
                .WithRecognitionLatch(0f)
                .WithMaxPlacedCustomers(maxPlacedCustomers)
                .WithSpawnEntry(StageConfigBuilder.CreateSushi(_disposables))
                .Build();

            _belt = new SushiBelt(_config, SushiDeck.FromSpawnTable(_config).Cards,
                                  new SequenceNumberIssuer(),
                                  new SushiPool<SushiItem>(new SushiItemFactory()));
            _pause = new PauseState();
            _wallet = new RecruitWallet(initialBudget);
            _coordinator = new ClaimCoordinator(_belt, _config, new RevenueLedger(), _wallet);
            _service = new CustomerPlacementService(_coordinator, _config,
                                                    new SequenceNumberIssuer(), _wallet, _pause);

            _claimedByCustomer = new List<int>();
            _coordinator.SushiClaimed += (customer, _) =>
                _claimedByCustomer.Add(customer.State.SequenceNumber);
        }
    }
}
