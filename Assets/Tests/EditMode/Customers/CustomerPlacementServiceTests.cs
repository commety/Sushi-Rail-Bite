using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
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

        /// <summary>스폰 지점(0)이 이미 집기 범위 안인 자리.</summary>
        private const float NearTable = 2f;

        private readonly List<Object> _disposables = new();

        private StageConfig _config;
        private CustomerData _customerData;
        private SushiBelt _belt;
        private ClaimCoordinator _coordinator;
        private CustomerPlacementService _service;
        private List<int> _claimedByCustomer;

        [SetUp]
        public void SetUp()
        {
            _customerData = ScriptableObject.CreateInstance<CustomerData>();
            SerializedFieldSetter.SetFloat(_customerData, "_reach", Reach);
            SerializedFieldSetter.SetInt(_customerData, "_maxSaturation", 5);

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
            Assert.IsTrue(_service.CanPlace(0));
        }

        [Test]
        public void CanPlace_OccupiedSlot_ReturnsFalse()
        {
            _service.Place(_customerData, 0, NearTable);

            Assert.IsFalse(_service.CanPlace(0));
        }

        [Test]
        public void CanPlace_AtMaxPlacedCustomers_ReturnsFalse()
        {
            Rebuild(maxPlacedCustomers: 2);
            _service.Place(_customerData, 0, NearTable);
            _service.Place(_customerData, 1, NearTable);

            Assert.IsFalse(_service.CanPlace(2));
        }

        [Test]
        public void CanPlace_AfterRemove_ReturnsTrueAgain()
        {
            _service.Place(_customerData, 0, NearTable);
            _service.Remove(0);

            Assert.IsTrue(_service.CanPlace(0));
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

        // ── M1 범위: 영입 비용을 보지 않는다 ────────────────────

        [Test]
        public void Place_HugeRecruitCost_StillPlaces()
        {
            // M1 의 배치는 무료다. 비용이 생기는 것은 M2 이며, 이 테스트가 그때
            // "비용을 조용히 무시하는" 구현을 잡아 준다.
            SerializedFieldSetter.SetInt(_customerData, "_recruitCost", 99999);

            Assert.IsNotNull(_service.Place(_customerData, 0, NearTable));
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

        private void Rebuild(int maxPlacedCustomers)
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
                .WithSpawnEntry(StageConfigBuilder.CreateSushi(_disposables), 1)
                .Build();

            _belt = new SushiBelt(_config, new SequenceNumberIssuer(),
                                  new SushiPool<SushiItem>(new SushiItemFactory()));
            _coordinator = new ClaimCoordinator(_belt, _config);
            _service = new CustomerPlacementService(_coordinator, _config, new SequenceNumberIssuer());

            _claimedByCustomer = new List<int>();
            _coordinator.SushiClaimed += (customer, _) =>
                _claimedByCustomer.Add(customer.State.SequenceNumber);
        }
    }
}
