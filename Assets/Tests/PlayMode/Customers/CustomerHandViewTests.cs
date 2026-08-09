using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense;
using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Scoring;
using SushiDefense.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
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

        /// <summary>접혔을 때 컨테이너가 내려가는 자리. 손잡이만 남기는 값이다.</summary>
        private const float CollapsedOffsetY = -144f;

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

        // ── 한도는 인구수를 센다 ─────────────────────────────────────────
        //
        // 손패 코드는 이 규칙을 위해 한 줄도 바뀌지 않았다 — CanPlaceAnywhere 가
        // 서비스에 되묻기 때문이다. 고친 데가 없다는 것은 회귀를 잡을 그물도 없다는
        // 뜻이라, 여기 남긴다.

        /// <summary>
        /// <b>잔액과 자리를 일부러 풀어 둔다.</b> 둘 중 하나라도 막고 있으면 «한도가
        /// 막았다» 가 확정되지 않는다 (<c>.claude/rules/tests.md</c> §3).
        /// </summary>
        [Test]
        public void Refresh_PopulationTwoWithOneHeadroom_DimsTheCard()
        {
            var heavy = NewCustomer("먹보", recruitCost: 10, population: 2);
            _hand.Bind(new[] { _cheap, heavy });
            _placement.Place(_cheap, 0, 2f);

            _hand.Refresh();

            Assert.AreEqual(1, _placement.PlacedPopulation, "한도 2 중 1 을 썼다");
            Assert.IsNull(_placement.OccupantOf(1), "자리는 남아 있다");
            Assert.IsTrue(_wallet.CanAfford(10), "잔액도 남아 있다");
            Assert.IsFalse(_hand.IsAvailableAt(1), "인구수 2 는 남은 1 에 못 들어간다");
        }

        /// <summary>
        /// 반례. 위 테스트만 있으면 «자리가 하나 남으면 무조건 흐리게» 하는 구현도 통과한다.
        /// </summary>
        [Test]
        public void Refresh_PopulationOneWithOneHeadroom_KeepsCardBright()
        {
            _hand.Bind(new[] { _cheap, _pricey });
            _placement.Place(_cheap, 0, 2f);

            _hand.Refresh();

            Assert.IsTrue(_hand.IsAvailableAt(0), "인구수 1 은 남은 1 에 들어간다");
        }

        // ── 잔액이 늘면 스스로 다시 평가한다 ──────────────────────────────

        /// <summary>
        /// <b>실플레이에서 «영입 비용이 충분해도 특수 손님을 더 못 놓는다» 던 증상이다.</b>
        ///
        /// <para>
        /// 카드가 다시 평가되는 시점이 «명부를 물릴 때» 와 «배치에 성공했을 때» 둘뿐이라,
        /// 영입 재화가 쌓여도 비싼 카드는 흐린 채로 남았다 — 다른 손님을 먼저 앉히면
        /// 풀리는 것이 그 증거였다.
        /// </para>
        /// <para>
        /// <b><c>Refresh</c> 를 부르지 않는다.</b> 부르면 이 테스트가 검증하려는 «스스로
        /// 다시 평가하는가» 를 테스트가 대신해 주게 되어, 고쳐지지 않은 코드도 통과한다.
        /// </para>
        /// </summary>
        [Test]
        public void Watch_BalanceGrows_ExpensiveCardBecomesAvailableWithoutRefresh()
        {
            _wallet.TrySpend(_wallet.Balance - 30);
            _hand.Watch(_wallet);
            _hand.Bind(new[] { _cheap, _pricey });
            Assert.IsFalse(_hand.IsAvailableAt(1), "잔액 30 이면 60 짜리는 못 놓는다");

            // 재화는 소비된 초밥 가격에서 적립된다 — 400 이 곧 40 이다.
            _wallet.AccrueFrom(400);

            Assert.IsTrue(_hand.IsAvailableAt(1), "잔액이 70 이 됐는데도 카드가 흐린 채로 남는다");
        }

        /// <summary>
        /// 반대 방향도 본다. 한쪽만 보면 «잔액이 바뀌면 무조건 켠다» 는 구현이 통과한다.
        /// </summary>
        [Test]
        public void Watch_BalanceSpent_ExpensiveCardGoesUnavailable()
        {
            _wallet.AccrueFrom(500);
            _hand.Watch(_wallet);
            _hand.Bind(new[] { _cheap, _pricey });
            Assert.IsTrue(_hand.IsAvailableAt(1));

            _wallet.TrySpend(_wallet.Balance - 10);

            Assert.IsFalse(_hand.IsAvailableAt(1));
        }

        /// <summary>
        /// 판이 바뀌면 지갑도 새로 열린다. 끊지 않으면 <b>죽은 지갑</b>이 손패를 계속
        /// 흔들어, 새 판의 잔액과 무관한 표시가 나온다.
        /// </summary>
        [Test]
        public void Unwatch_ThenOldWalletChanges_DoesNotTouchTheHand()
        {
            _wallet.TrySpend(_wallet.Balance - 30);
            _hand.Watch(_wallet);
            _hand.Bind(new[] { _cheap, _pricey });

            _hand.Unwatch();
            _wallet.AccrueFrom(1000);

            Assert.IsFalse(_hand.IsAvailableAt(1), "끊었는데도 옛 지갑이 카드를 켰다");
        }

        // ── 접히는 손패 (M6.5) ───────────────────────────────────────────
        //
        // 카드가 128×192 가 되면서 손패가 화면을 먹는다. 평소에는 내려 두고 손이 오면 올린다.
        // 움직이는 것은 **컨테이너**이고 카드는 제자리에 있는다 — 카드를 직접 옮기면
        // 드래그가 돌아갈 자리(CustomerCardDrag._home)가 접힌 위치를 가리킨다.

        [Test]
        public void Fresh_IsCollapsed()
        {
            Assert.IsFalse(_hand.IsExpanded);
        }

        /// <summary>
        /// <c>SetExpanded</c> 는 <b>상태만 바꾸고</b> 옮기는 것은 <c>Update</c> 다 — 옮기는
        /// 경로를 하나로 두려는 것이라, 테스트도 프레임을 한 번 넘겨 그 경로를 지난다.
        /// </summary>
        [UnityTest]
        public IEnumerator SetExpanded_True_RaisesTheTray()
        {
            var tray = TrayOf(_hand);
            yield return null;
            var collapsed = tray.anchoredPosition.y;

            _hand.SetExpanded(true);
            yield return null;

            Assert.IsTrue(_hand.IsExpanded);
            Assert.Greater(tray.anchoredPosition.y, collapsed);
        }

        /// <summary>
        /// 반례. «올라간다» 만 보면 <b>항상 올려 두는</b> 구현도 통과한다. 접힌 높이가
        /// 구체값으로 정해져 있어야 화면에서 손잡이만 남는다.
        /// </summary>
        [UnityTest]
        public IEnumerator SetExpanded_False_LowersToPeekHeight()
        {
            var tray = TrayOf(_hand);
            _hand.SetExpanded(true);
            yield return null;

            _hand.SetExpanded(false);
            yield return null;

            Assert.IsFalse(_hand.IsExpanded);
            Assert.AreEqual(CollapsedOffsetY, tray.anchoredPosition.y, 0.001f);
        }

        /// <summary>
        /// <b>이 단계에서 가장 중요한 테스트다.</b> 카드를 직접 움직이는 구현으로 되돌아가면
        /// 여기서만 죽는다 — 화면으로는 «접힌다» 가 똑같이 보이고, 증상은 <b>펼친 채 끌어
        /// 놓았을 때 카드가 화면 밖으로 돌아가는 것</b>으로만 드러난다.
        /// </summary>
        [UnityTest]
        public IEnumerator SetExpanded_DoesNotMoveTheCards()
        {
            var card = (RectTransform)_hand.transform.GetChild(0).GetChild(0);
            yield return null;
            var before = card.anchoredPosition;

            _hand.SetExpanded(true);
            yield return null;

            Assert.AreEqual(before, card.anchoredPosition, "카드를 직접 움직였다");
        }

        /// <summary>
        /// 펼친 채 카드를 끌어 놓아도 <b>제자리로 돌아온다.</b> 위 테스트와 짝이며, 이쪽은
        /// 드래그 경로를 실제로 지난다.
        /// </summary>
        [Test]
        public void DropAt_WhileExpanded_ReturnsCardToItsHome()
        {
            var drag = _hand.transform.GetChild(0).GetChild(0).GetComponent<CustomerCardDrag>();
            var home = ((RectTransform)drag.transform).anchoredPosition;
            _hand.Bind(new[] { _cheap });
            _hand.SetExpanded(true);

            var pointer = new PointerEventData(EventSystem.current) { position = Vector2.zero };
            drag.OnBeginDrag(pointer);
            drag.OnEndDrag(pointer);

            Assert.AreEqual(home, ((RectTransform)drag.transform).anchoredPosition,
                            "펼친 상태에서 끌면 카드가 엉뚱한 자리로 돌아간다");
        }

        /// <summary>
        /// <b>컨테이너를 안 물려도 동작해야 한다.</b> 씬의 손패가 정확히 그 모양이다 —
        /// <c>Hand</c> 는 그래픽이 없고 자식이 카드뿐이라, 그 자신이 컨테이너 역할을 한다.
        ///
        /// <para>
        /// 다른 테스트는 전부 <c>_tray</c> 를 주입하므로 <b>이 폴백을 한 번도 밟지 않는다</b> —
        /// 우회로가 곧 사각지대라는 그 형태다 (<c>.claude/rules/tests.md</c> §1). 폴백이 죽으면
        /// 씬에서만 손패가 안 접힌다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator SetExpanded_WithoutTray_MovesItself()
        {
            var hand = NewHand(cardCount: 1);
            ClearTray(hand);
            var rect = (RectTransform)hand.transform;
            yield return null;
            var collapsed = rect.anchoredPosition.y;

            hand.SetExpanded(true);
            yield return null;

            Assert.Greater(rect.anchoredPosition.y, collapsed, "컨테이너가 없으면 아무것도 안 움직인다");
        }

        // ── 드래그 좌표 ──────────────────────────────────────────────────

        /// <summary>
        /// <b>카드가 커서에 붙어 있어야 한다.</b> 화면 델타를 그대로 더하면 캔버스 배율만큼
        /// 어긋남이 <b>누적되어</b>, 끌수록 그림이 손에서 떨어져 나간다 — 기능은 커서 좌표로
        /// 판정하므로 멀쩡하고 <b>그림만 틀리는</b> 형태라 테스트가 없으면 브라우저에서만
        /// 드러난다.
        ///
        /// <para>
        /// 캔버스를 <b>배율 2</b>로 세운다. 배율이 1이면 화면 픽셀과 캔버스 단위가 같아져
        /// 잘못된 구현도 통과한다 (<c>.claude/rules/tests.md</c> §3).
        /// </para>
        /// </summary>
        [Test]
        public void OnDrag_ScaledCanvas_KeepsTheCardUnderTheCursor()
        {
            var (drag, canvas) = NewScaledCard(scale: 2f);
            var rect = (RectTransform)drag.transform;
            var start = ScreenPointOf(canvas, rect.anchoredPosition);

            drag.OnBeginDrag(Pointer(start));
            drag.OnDrag(Pointer(start + new Vector2(200f, 0f)));

            // 화면에서 200px 옮겼고 배율이 2이므로 캔버스로는 100 이어야 한다.
            Assert.AreEqual(100f, rect.anchoredPosition.x, 0.01f,
                            "카드가 커서에서 벗어났다 — 화면 델타를 그대로 더하고 있다");
        }

        /// <summary>
        /// 집은 지점을 유지한다. 카드 원점으로 순간이동하면 «집은 곳» 이 무시된다.
        /// </summary>
        [Test]
        public void OnDrag_GrabbedOffCenter_KeepsTheGrabOffset()
        {
            var (drag, canvas) = NewScaledCard(scale: 1f);
            var rect = (RectTransform)drag.transform;
            var grab = ScreenPointOf(canvas, rect.anchoredPosition + new Vector2(40f, 0f));

            drag.OnBeginDrag(Pointer(grab));
            drag.OnDrag(Pointer(grab));

            Assert.AreEqual(0f, rect.anchoredPosition.x, 0.01f, "카드가 커서로 순간이동했다");
        }

        private static PointerEventData Pointer(Vector2 screenPoint)
        {
            return new PointerEventData(EventSystem.current) { position = screenPoint };
        }

        private static Vector2 ScreenPointOf(Canvas canvas, Vector2 canvasPoint)
        {
            return canvasPoint * canvas.scaleFactor
                   + new Vector2(Screen.width, Screen.height) * 0.5f;
        }

        /// <summary>배율이 1이 아닌 캔버스 위의 카드 하나. 배율 1이면 이 버그가 안 드러난다.</summary>
        private (CustomerCardDrag drag, Canvas canvas) NewScaledCard(float scale)
        {
            var canvasGo = NewObject("ScaledCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = scale;

            var card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(canvasGo.transform, false);
            new GameObject("Icon", typeof(RectTransform)).transform.SetParent(card.transform, false);
            card.AddComponent<Image>();
            card.AddComponent<Button>();
            card.AddComponent<CardView>();

            var drag = card.AddComponent<CustomerCardDrag>();
            drag.Bind(_hand, _cheap, NewCamera());
            return (drag, canvas);
        }

        [Test]
        public void PointerEnter_Collapsed_Expands()
        {
            _hand.OnPointerEnter(null);

            Assert.IsTrue(_hand.IsExpanded);
        }

        [Test]
        public void PointerExit_Expanded_Collapses()
        {
            _hand.SetExpanded(true);

            _hand.OnPointerExit(null);

            Assert.IsFalse(_hand.IsExpanded);
        }

        /// <summary>
        /// <b>호버는 터치에 없다.</b> 배포 타깃이 웹이라 모바일 브라우저가 사정권이고,
        /// 탭으로도 여는 길이 없으면 그쪽에서는 손패가 영영 접힌 채로 남는다.
        /// </summary>
        [Test]
        public void PointerClick_Toggles()
        {
            _hand.OnPointerClick(null);
            Assert.IsTrue(_hand.IsExpanded);

            _hand.OnPointerClick(null);
            Assert.IsFalse(_hand.IsExpanded);
        }

        /// <summary>
        /// 카드를 끌고 손패 밖으로 나가는 것은 <b>정상 조작</b>이다. 그때 접히면 카드가
        /// 손가락 아래에서 함께 내려간다.
        /// </summary>
        [Test]
        public void PointerExit_WhileDragging_StaysExpanded()
        {
            var drag = _hand.transform.GetChild(0).GetChild(0).GetComponent<CustomerCardDrag>();
            _hand.Bind(new[] { _cheap });
            _hand.SetExpanded(true);
            drag.OnBeginDrag(new PointerEventData(EventSystem.current));

            _hand.OnPointerExit(null);

            Assert.IsTrue(_hand.IsExpanded, "끌고 나가는데 손패가 접혔다");
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

        /// <summary>
        /// 손패 하나. <b>카드는 컨테이너 아래</b>에 붙는다 — 접힘이 움직이는 것이 그 컨테이너라,
        /// 카드를 루트에 직접 붙이면 «카드가 안 움직인다» 를 확인할 수가 없다.
        /// </summary>
        private CustomerHandView NewHand(int cardCount)
        {
            var root = NewObject("CustomerHand");
            root.AddComponent<RectTransform>();

            var tray = new GameObject("Tray", typeof(RectTransform));
            tray.transform.SetParent(root.transform, false);

            var drags = new CustomerCardDrag[cardCount];
            for (var i = 0; i < cardCount; i++)
            {
                var card = new GameObject($"Card{i}", typeof(RectTransform));
                card.transform.SetParent(tray.transform, false);

                new GameObject("Icon", typeof(RectTransform)).transform
                    .SetParent(card.transform, false);

                card.AddComponent<Image>();
                card.AddComponent<Button>();
                card.AddComponent<CardView>();
                drags[i] = card.AddComponent<CustomerCardDrag>();
            }

            var hand = root.AddComponent<CustomerHandView>();
            hand.InitializeCards(drags);

            SetHandFields(hand, (RectTransform)tray.transform);
            return hand;
        }

        /// <summary>
        /// 접힘 수치를 인스펙터 대신 직렬화로 밀어 넣는다. <b>전개 시간을 0 으로 둔다</b> —
        /// 보간이 억제 장치라, 시간이 걸리는 값이면 «안 움직인다» 와 «아직 안 움직였다» 가
        /// 구분되지 않는다 (<c>.claude/rules/tests.md</c> §3).
        /// </summary>
        private static void SetHandFields(CustomerHandView hand, RectTransform tray)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(hand);
            serialized.FindProperty("_tray").objectReferenceValue = tray;
            serialized.FindProperty("_collapsedOffsetY").floatValue = CollapsedOffsetY;
            serialized.FindProperty("_slideSeconds").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        /// <summary>컨테이너를 안 물린 상태로 되돌린다 — 씬의 손패와 같은 조건.</summary>
        private static void ClearTray(CustomerHandView hand)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(hand);
            serialized.FindProperty("_tray").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        private static RectTransform TrayOf(CustomerHandView hand)
        {
            return (RectTransform)hand.transform.Find("Tray");
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

        private CustomerData NewCustomer(string displayName, int recruitCost, int population = 1)
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
            serialized.FindProperty("_population").intValue = population;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return customer;
        }

        /// <summary>
        /// <b><c>Awake</c> 가 아직 돌지 않은 카드에 물려도</b> 그려지는지 본다.
        ///
        /// <para>
        /// 씬 진입점은 <c>Awake</c> 안에서 판을 세우고 마지막 줄에서 손패를 물리므로, 카드의
        /// <c>Awake</c> 가 아직인 상태로 <see cref="CustomerCardDrag.Bind"/> 가 불릴 수 있다.
        /// 그러면 <c>Card</c> 가 <c>null</c> 이라 그 자리에서 터지고, 앞의 것은 전부 끝난 뒤라
        /// <b>HUD 도 벨트도 멀쩡한 채 손패만 빈 카드</b>가 된다.
        /// </para>
        /// <para>
        /// <b>이 순서를 손으로 만들어야 한다.</b> 이 파일의 다른 하네스는 켜져 있는
        /// 오브젝트에 <c>AddComponent</c> 하므로 <c>Awake</c> 가 즉시 돌아 이 자리를 지나가고,
        /// 씬 테스트도 에디터에서는 순서가 반대라 잡지 못한다 — <b>실제로 난 곳은 WebGL 플레이어</b>
        /// 뿐이었다. 꺼진 오브젝트에 붙이면 <c>Awake</c> 는 켜질 때까지 오지 않는다.
        /// </para>
        /// </summary>
        [Test]
        public void Bind_CardAwakeHasNotRun_StillDrawsInsteadOfThrowing()
        {
            var card = new GameObject("Card", typeof(RectTransform));
            _garbage.Add(card);
            card.SetActive(false);

            new GameObject("Icon", typeof(RectTransform)).transform.SetParent(card.transform, false);
            card.AddComponent<Image>();
            card.AddComponent<Button>();
            card.AddComponent<CardView>();
            var drag = card.AddComponent<CustomerCardDrag>();

            drag.Bind(null, _cheap, null);

            Assert.IsNotNull(drag.Card, "카드 표현을 물지 못했다 — Awake 순서에 기대고 있다");
            Assert.IsTrue(drag.Card.IsShowing, "카드가 내용을 그리지 않았다");
            Assert.AreEqual("기본", drag.Card.NameText);
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
