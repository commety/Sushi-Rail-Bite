using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Scoring;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode.Customers
{
    /// <summary>
    /// 배치 <b>규칙</b>은 EditMode 의 <c>CustomerPlacementServiceTests</c> 가 검증한다.
    /// 여기서는 조작이 화면 상태(자리 점유·시각 표현)로 옮겨지는지만 본다.
    /// </summary>
    public sealed class CustomerPlacementControllerTests
    {
        private const float Speed = 10f;
        private const float Interval = 1f;
        private const float Length = 100f;

        private readonly List<GameObject> _objects = new();

        private SushiData _sushiData;
        private CustomerData _customerData;
        private StageConfig _config;
        private SushiBelt _belt;
        private ClaimCoordinator _coordinator;
        private CustomerPlacementService _service;
        private CustomerPlacementController _controller;
        private TableSlotView _slot;
        private CustomerView _seatVisual;

        [SetUp]
        public void SetUp()
        {
            _sushiData = ScriptableObject.CreateInstance<SushiData>();
            _customerData = ScriptableObject.CreateInstance<CustomerData>();
            _config = StageConfigTestFactory.Create(Speed, Interval, Length, _sushiData);

            _belt = new SushiBelt(_config, SushiDeck.FromSpawnTable(_config).Cards,
                                  new SequenceNumberIssuer(),
                                  new SushiPool<SushiItem>(new SushiItemFactory()));
            // 배치 규칙만 보는 테스트라 비용 검사가 끼어들지 않게 예산을 넉넉히 준다.
            var wallet = new RecruitWallet(100000);
            _coordinator = new ClaimCoordinator(_belt, _config, new RevenueLedger(), wallet);
            _service = new CustomerPlacementService(_coordinator, _config,
                                                    new SequenceNumberIssuer(), wallet);

            _seatVisual = NewObject("SeatVisual").AddComponent<CustomerView>();
            _slot = NewObject("TableSlot").AddComponent<TableSlotView>();
            _slot.Initialize(slotIndex: 0, beltPosition: 5f, _seatVisual);

            _controller = NewObject("Placement").AddComponent<CustomerPlacementController>();
            _controller.Initialize(new[] { _slot }, new[] { _customerData });
            _controller.Bind(_service, _coordinator);
        }

        [TearDown]
        public void TearDown()
        {
            _coordinator.Dispose();

            foreach (var target in _objects)
            {
                Object.DestroyImmediate(target);
            }

            _objects.Clear();
            Object.DestroyImmediate(_sushiData);
            Object.DestroyImmediate(_customerData);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Initialize_EmptySlot_SeatVisualStartsHidden()
        {
            Assert.IsFalse(_seatVisual.gameObject.activeSelf);
            Assert.IsFalse(_slot.IsOccupied);
        }

        [Test]
        public void TryPlaceAt_EmptySlot_OccupiesAndShowsVisual()
        {
            var placed = _controller.TryPlaceAt(_slot);

            Assert.IsTrue(placed);
            Assert.IsTrue(_slot.IsOccupied);
            Assert.IsTrue(_seatVisual.gameObject.activeSelf);
            Assert.IsNotNull(_seatVisual.Logic);
        }

        [Test]
        public void TryPlaceAt_UsesSlotBeltPosition()
        {
            _controller.TryPlaceAt(_slot);

            Assert.AreEqual(_slot.BeltPosition, _seatVisual.Logic.BeltPosition);
        }

        [Test]
        public void TryPlaceAt_OccupiedSlot_ReturnsFalse()
        {
            _controller.TryPlaceAt(_slot);

            Assert.IsFalse(_controller.TryPlaceAt(_slot));
            Assert.AreEqual(1, _service.PlacedCount);
        }

        [Test]
        public void TryPlaceAt_RegistersWithCoordinator()
        {
            _controller.TryPlaceAt(_slot);

            Assert.AreEqual(1, _coordinator.Customers.Count);
        }

        [Test]
        public void RemoveAt_PlacedSlot_VacatesAndHidesVisual()
        {
            _controller.TryPlaceAt(_slot);

            var removed = _controller.RemoveAt(_slot);

            Assert.IsTrue(removed);
            Assert.IsFalse(_slot.IsOccupied);
            Assert.IsFalse(_seatVisual.gameObject.activeSelf);
            Assert.IsEmpty(_coordinator.Customers);
        }

        [Test]
        public void RemoveAt_EmptySlot_ReturnsFalse()
        {
            Assert.IsFalse(_controller.RemoveAt(_slot));
        }

        [Test]
        public void TryPlaceAt_AfterRemove_PlacesAgain()
        {
            _controller.TryPlaceAt(_slot);
            _controller.RemoveAt(_slot);

            Assert.IsTrue(_controller.TryPlaceAt(_slot));
        }

        private GameObject NewObject(string name)
        {
            var created = new GameObject(name);
            _objects.Add(created);
            return created;
        }
    }
}
