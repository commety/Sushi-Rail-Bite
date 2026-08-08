using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Scoring;
using SushiDefense.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.Customers
{
    /// <summary>
    /// 손패는 <b>명부를 카드로 늘어놓고 드롭 지점을 자리로 옮기기만</b> 한다.
    ///
    /// <para>
    /// 놓을 수 있는지는 <c>CustomerPlacementService</c> 가, 어느 자리인지는
    /// <c>SlotPicker</c> 가 답한다 — 여기서 되물으면 두 판정이 어긋난다.
    /// </para>
    /// </summary>
    public sealed class CustomerHandViewTests
    {
        private const float DropRadius = 1.2f;

        private readonly List<Object> _garbage = new();

        private StageConfig _config;
        private RecruitWallet _wallet;
        private ClaimCoordinator _coordinator;
        private CustomerPlacementService _placement;
        private CustomerPlacementController _controller;
        private CustomerHandView _hand;
        private TableSlotView[] _slots;
        private CustomerData _cheap;
        private CustomerData _pricey;

        [SetUp]
        public void SetUp()
        {
            var sushi = NewAsset<SushiData>();
            _config = StageConfigTestFactory.Create(1f, 10f, 20f, sushi);
            _garbage.Add(_config);
            StageConfigTestFactory.AddTableSlot(_config, 0, 2f, new Vector2(-4f, -2f));
            StageConfigTestFactory.AddTableSlot(_config, 1, 6f, new Vector2(4f, -2f));
            StageConfigTestFactory.SetInt(_config, "_maxPlacedCustomers", 2);
            StageConfigTestFactory.SetInt(_config, "_initialRecruitBudget", 50);

            _cheap = NewCustomer("기본", 20);
            _pricey = NewCustomer("먹보", 60);

            _slots = new[] { NewSlot(0, 2f, new Vector2(-4f, -2f)), NewSlot(1, 6f, new Vector2(4f, -2f)) };

            var belt = new SushiBelt(_config, new[] { sushi }, new SequenceNumberIssuer(),
                                     new SushiPool<SushiItem>(new SushiItemFactory()));
            _wallet = new RecruitWallet(_config.InitialRecruitBudget);
            _coordinator = new ClaimCoordinator(belt, _config, new RevenueLedger(), _wallet);
            _placement = new CustomerPlacementService(_coordinator, _config,
                                                      new SequenceNumberIssuer(), _wallet);

            _controller = NewObject("Placement").AddComponent<CustomerPlacementController>();
            _controller.Initialize(_slots, new[] { _cheap, _pricey });
            _controller.Bind(_placement, _coordinator);

            _hand = NewHand(cardCount: 3);
            _hand.Initialize(_controller, NewCamera(), _slots);
        }

        [TearDown]
        public void TearDown()
        {
            _coordinator?.Dispose();

            foreach (var item in _garbage)
            {
                Object.DestroyImmediate(item);
            }

            _garbage.Clear();
        }

        // ── 드롭 ───────────────────────────────────────────────────────────

        [Test]
        public void DropAt_EmptySlotInRange_PlacesCustomer()
        {
            var placed = _hand.DropAt(_cheap, _slots[0].transform.position);

            Assert.IsTrue(placed);
            Assert.AreEqual(1, _placement.PlacedCount);
            Assert.AreEqual(1, _hand.PlacedCount);
        }

        [Test]
        public void DropAt_FarFromEverySlot_DoesNotPlace()
        {
            Assert.IsFalse(_hand.DropAt(_cheap, new Vector2(100f, 100f)));
            Assert.AreEqual(0, _placement.PlacedCount);
        }

        [Test]
        public void DropAt_OccupiedSlot_DoesNotPlace()
        {
            _hand.DropAt(_cheap, _slots[0].transform.position);

            var again = _hand.DropAt(_cheap, _slots[0].transform.position);

            Assert.IsFalse(again);
            Assert.AreEqual(1, _placement.PlacedCount);
        }

        /// <summary>
        /// 잔액은 <b>서비스가</b> 본다. 손패가 되물으면 두 판정이 어긋난다.
        /// </summary>
        [Test]
        public void DropAt_InsufficientBalance_DoesNotPlace()
        {
            _wallet.TrySpend(_wallet.Balance);

            Assert.IsFalse(_hand.DropAt(_cheap, _slots[0].transform.position));
            Assert.AreEqual(0, _placement.PlacedCount);
        }

        [Test]
        public void DropAt_NullCustomer_DoesNotPlace()
        {
            Assert.IsFalse(_hand.DropAt(null, _slots[0].transform.position));
            Assert.AreEqual(0, _placement.PlacedCount);
        }

        // ── 손패 ───────────────────────────────────────────────────────────

        [Test]
        public void Bind_Roster_ShowsOneCardPerMember()
        {
            _hand.Bind(new[] { _cheap, _pricey });

            Assert.AreEqual(2, _hand.ShownCardCount);
        }

        /// <summary>
        /// <b>이 단계의 실제 이유다.</b> M3~M5 내내 손님 보상은 HUD 한 줄로만 존재했고,
        /// 영입한 손님을 고를 방법이 없었다.
        /// </summary>
        [Test]
        public void Bind_GrownRoster_ShowsNewCard()
        {
            _hand.Bind(new[] { _cheap });

            _hand.Bind(new[] { _cheap, _pricey });

            Assert.AreEqual(2, _hand.ShownCardCount);
            Assert.AreSame(_pricey, _hand.CustomerAt(1));
        }

        /// <summary>
        /// 줄어드는 방향으로 본다. 늘어나는 방향은 덮어써지므로 남는 카드를 끄지 않는
        /// 구현도 통과한다.
        /// </summary>
        [Test]
        public void Bind_ShrunkRoster_DoesNotLeaveStaleCards()
        {
            _hand.Bind(new[] { _cheap, _pricey });

            _hand.Bind(new[] { _cheap });

            Assert.AreEqual(1, _hand.ShownCardCount);
            Assert.IsNull(_hand.CustomerAt(1));
        }

        [Test]
        public void Bind_RosterLargerThanCards_ShowsWhatFits()
        {
            var many = new[] { _cheap, _pricey, NewCustomer("소식", 40), NewCustomer("여분", 10) };

            _hand.Bind(many);

            Assert.AreEqual(3, _hand.ShownCardCount);
        }

        /// <summary>
        /// 놓을 수 없는 카드는 <b>끌기 전에</b> 알 수 있어야 한다. 놓아 보고 실패하는 것보다
        /// 낫다.
        /// </summary>
        [Test]
        public void Refresh_WhenBalanceTooLow_MarksExpensiveCardUnavailable()
        {
            _hand.Bind(new[] { _cheap, _pricey });
            _wallet.TrySpend(_wallet.Balance - 30);

            _hand.Refresh();

            Assert.IsTrue(_hand.IsAvailableAt(0), "20 짜리는 아직 놓을 수 있다");
            Assert.IsFalse(_hand.IsAvailableAt(1), "60 짜리는 잔액 30 으로 못 놓는다");
        }

        /// <summary>
        /// 한도만 채우고 <b>잔액은 넉넉히 둔다</b>. 둘을 동시에 막으면 잔액만 보는 구현도
        /// 통과한다.
        /// </summary>
        [Test]
        public void Refresh_WhenLimitReached_MarksEveryCardUnavailable()
        {
            _hand.Bind(new[] { _cheap, _pricey });
            _placement.Place(_cheap, 0, 2f);
            _placement.Place(_cheap, 1, 6f);

            _hand.Refresh();

            Assert.AreEqual(2, _placement.PlacedCount);
            Assert.IsFalse(_hand.IsAvailableAt(0));
            Assert.IsFalse(_hand.IsAvailableAt(1));
        }

        // ── 우회로를 타지 않는 테스트 ────────────────────────────────────

        /// <summary>
        /// 씬 진입점은 카메라를 들고 있지 않아 <c>null</c> 을 넘긴다. 그대로 대입하면
        /// <c>Awake</c> 가 잡아 둔 참조가 날아가 <b>드래그가 통째로 죽는데</b>, 다른 테스트는
        /// <c>DropAt</c> 을 직접 불러 좌표 변환을 지나치므로 브라우저에서야 드러난다.
        /// M5 에서 같은 사고가 실제로 났다.
        /// </summary>
        [Test]
        public void Initialize_WithNullCamera_KeepsResolvedCamera()
        {
            var mainCamera = NewObject("Main Camera");
            mainCamera.tag = "MainCamera";
            mainCamera.AddComponent<Camera>();

            var hand = NewHand(cardCount: 1);
            hand.Initialize(_controller, null, _slots);

            Assert.IsNotNull(hand.WorldCamera, "씬 진입점이 넘긴 null 이 카메라를 지웠다");
        }

        // ── 조립 ───────────────────────────────────────────────────────────

        private CustomerHandView NewHand(int cardCount)
        {
            var root = NewObject("CustomerHand");
            root.AddComponent<RectTransform>();

            var drags = new CustomerCardDrag[cardCount];
            for (var i = 0; i < cardCount; i++)
            {
                var card = new GameObject($"Card{i}", typeof(RectTransform));
                card.transform.SetParent(root.transform, false);

                new GameObject("Icon", typeof(RectTransform)).transform
                    .SetParent(card.transform, false);

                card.AddComponent<Image>();
                card.AddComponent<Button>();
                card.AddComponent<CardView>();
                drags[i] = card.AddComponent<CustomerCardDrag>();
            }

            var hand = root.AddComponent<CustomerHandView>();
            hand.InitializeCards(drags);
            return hand;
        }

        private Camera NewCamera()
        {
            return NewObject("Drag Camera").AddComponent<Camera>();
        }

        private TableSlotView NewSlot(int index, float beltPosition, Vector2 position)
        {
            var go = NewObject($"TableSlot{index}");
            go.transform.position = position;

            var slot = go.AddComponent<TableSlotView>();
            slot.Initialize(index, beltPosition, null);
            return slot;
        }

        private CustomerData NewCustomer(string displayName, int recruitCost)
        {
            var customer = NewAsset<CustomerData>();

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(customer);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_recruitCost").intValue = recruitCost;
            serialized.FindProperty("_reach").floatValue = 3f;
            serialized.FindProperty("_targetingMin").intValue = 100;
            serialized.FindProperty("_targetingMax").intValue = 300;
            serialized.FindProperty("_maxSaturation").intValue = 5;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return customer;
        }

        private T NewAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _garbage.Add(asset);
            return asset;
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _garbage.Add(go);
            return go;
        }
    }
}
