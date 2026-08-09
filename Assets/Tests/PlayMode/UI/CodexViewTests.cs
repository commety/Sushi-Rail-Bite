using System;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.UI
{
    /// <summary>
    /// 화면은 <b>판정하지 않는다.</b> 프레젠터가 넘긴 목록을 카드로 옮기는 것까지만 본다 —
    /// 무엇을 보여줄지는 EditMode 의 <c>CodexPresenterTests</c> 가 검증한다.
    ///
    /// <para>
    /// PlayMode 는 느리므로 <b>씬·컴포넌트가 실제로 필요한 것만</b> 여기 둔다
    /// (<c>.claude/rules/tests.md</c> §1). 자리 수와 목록 길이가 어긋나는 경우가 그렇다 —
    /// <see cref="CardView"/> 가 실제로 켜지고 꺼지는지는 오브젝트가 있어야 보인다.
    /// </para>
    /// </summary>
    public sealed class CodexViewTests
    {
        private const int CardSlots = 4;

        private readonly List<Object> _garbage = new();

        private CodexView _view;
        private CardView[] _cards;
        private GameObject _panelRoot;
        private Button _close;

        [SetUp]
        public void SetUp()
        {
            var root = NewObject("CodexView");
            root.AddComponent<RectTransform>();

            _panelRoot = NewChild(root, "Panel");

            _cards = new CardView[CardSlots];
            for (var i = 0; i < CardSlots; i++)
            {
                _cards[i] = NewCard(_panelRoot, i);
            }

            var closeGo = NewChild(_panelRoot, "CloseButton");
            closeGo.AddComponent<Image>();
            _close = closeGo.AddComponent<Button>();

            _view = root.AddComponent<CodexView>();
            _view.Initialize(_panelRoot, _cards, _close);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _garbage)
            {
                Object.DestroyImmediate(item);
            }

            _garbage.Clear();
        }

        /// <summary>초밥이 먼저, 손님이 뒤. 카탈로그의 순서를 그대로 잇는다.</summary>
        [Test]
        public void ShowEntries_SushiThenCustomers_DrawsInThatOrder()
        {
            _view.ShowEntries(Sushi("장어", 300), Customers("먹보"));

            Assert.IsTrue(_view.IsShowing);
            Assert.AreEqual(2, _view.ShownCardCount);
            StringAssert.Contains("장어", _cards[0].NameText);
            StringAssert.Contains("먹보", _cards[1].NameText);
        }

        /// <summary>
        /// <b>자리가 모자라도 예외를 내지 않는다.</b> 씬을 조금씩 조립하는 동안 나머지를
        /// 아무것도 확인할 수 없게 되기 때문이다.
        /// </summary>
        [Test]
        public void ShowEntries_MoreThanSlots_DrawsWhatFits()
        {
            var many = new SushiData[CardSlots + 3];
            for (var i = 0; i < many.Length; i++)
            {
                many[i] = NewSushi($"초밥{i}", 100 + i);
            }

            Assert.DoesNotThrow(() => _view.ShowEntries(many, Array.Empty<CustomerData>()));

            Assert.AreEqual(CardSlots, _view.ShownCardCount);
            Assert.IsTrue(_cards[CardSlots - 1].IsShowing);
        }

        /// <summary>
        /// <b>줄어드는 방향</b>으로 본다. 늘어나는 방향만 보면 지우지 않는 구현이 통과한다.
        /// </summary>
        [Test]
        public void ShowEntries_Twice_DoesNotLeaveStaleCards()
        {
            _view.ShowEntries(Sushi("장어", 300), Customers("먹보", "소식가"));

            _view.ShowEntries(Sushi("계란", 100), Array.Empty<CustomerData>());

            Assert.AreEqual(1, _view.ShownCardCount);
            StringAssert.Contains("계란", _cards[0].NameText);
            Assert.IsFalse(_cards[1].IsShowing, "직전 손님 카드가 남았다");
            Assert.IsFalse(_cards[2].IsShowing);
        }

        [Test]
        public void Hide_AfterShow_DisablesEveryCard()
        {
            _view.ShowEntries(Sushi("장어", 300), Customers("먹보"));

            _view.Hide();

            Assert.IsFalse(_view.IsShowing);
            Assert.AreEqual(0, _view.ShownCardCount);
            Assert.IsFalse(_cards[0].IsShowing);
            Assert.IsFalse(_cards[1].IsShowing);
            Assert.IsFalse(_panelRoot.activeSelf);
        }

        /// <summary>
        /// 목록이 비는 것은 오류가 아니다 — 사전은 열리고 카드만 비어 있다.
        /// </summary>
        [Test]
        public void ShowEntries_Empty_StillOpensPanel()
        {
            _view.ShowEntries(Array.Empty<SushiData>(), Array.Empty<CustomerData>());

            Assert.IsTrue(_view.IsShowing);
            Assert.AreEqual(0, _view.ShownCardCount);
            Assert.IsTrue(_panelRoot.activeSelf);
        }

        /// <summary>
        /// 손님 카드에는 <b>영입 비용</b>이 보여야 한다 — 사전이 답할 값이다.
        ///
        /// <para>
        /// M6.5 에서 그 값이 수치 줄에서 <b>우상단 동전으로</b> 옮겨 갔다. 검증도 따라 옮긴다 —
        /// 빼 버리면 사전 화면에서 비용이 보이는지 아무도 안 본다.
        /// </para>
        /// </summary>
        [Test]
        public void ShowEntries_CustomerCard_ShowsRecruitCost()
        {
            _view.ShowEntries(Array.Empty<SushiData>(), Customers("먹보"));

            Assert.AreEqual("45", _cards[0].CostText);
            Assert.IsTrue(_cards[0].IsCoinShown, "동전이 꺼져 값이 안 보인다");
        }

        /// <summary>
        /// 닫기 버튼이 프레젠터까지 배달되는가. <c>EventSystem</c> 없이
        /// <c>onClick.Invoke()</c> 로 부르는 것은 우회로이며, 씬에 <c>EventSystem</c> 이
        /// 실재하는지는 씬 테스트가 따로 본다 (step-12).
        /// </summary>
        [Test]
        public void ClickClose_ClosesThroughPresenter()
        {
            var catalog = ScriptableObject.CreateInstance<CardCatalog>();
            _garbage.Add(catalog);

            var presenter = new CodexPresenter(_view, catalog);
            _view.Bind(presenter);
            presenter.Open();

            _close.onClick.Invoke();

            Assert.IsFalse(presenter.IsOpen);
            Assert.IsFalse(_view.IsShowing);
        }

        // ── 조립 ───────────────────────────────────────────────────────────

        private SushiData[] Sushi(string displayName, int price)
        {
            return new[] { NewSushi(displayName, price) };
        }

        private SushiData NewSushi(string displayName, int price)
        {
            var sushi = ScriptableObject.CreateInstance<SushiData>();
            sushi.name = displayName;
            _garbage.Add(sushi);

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(sushi);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_price").intValue = price;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return sushi;
        }

        private CustomerData[] Customers(params string[] displayNames)
        {
            var customers = new CustomerData[displayNames.Length];
            for (var i = 0; i < displayNames.Length; i++)
            {
                customers[i] = NewCustomer(displayNames[i]);
            }

            return customers;
        }

        private CustomerData NewCustomer(string displayName)
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            customer.name = displayName;
            _garbage.Add(customer);

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(customer);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_recruitCost").intValue = 45;
            serialized.FindProperty("_targetingMin").intValue = 100;
            serialized.FindProperty("_targetingMax").intValue = 300;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return customer;
        }

        private CardView NewCard(GameObject parent, int index)
        {
            var go = NewChild(parent, $"Card{index}");

            var icon = NewChild(go, "Icon");
            icon.AddComponent<Image>();

            NewChild(go, "NameLabel").AddComponent<TextMeshProUGUI>();
            NewChild(go, "DetailLabel").AddComponent<TextMeshProUGUI>();

            // M6.5 에서 영입 비용이 우상단 동전으로 옮겨 갔다. 하네스에 이 둘이 없으면
            // 비용이 갈 곳이 없어, 카드가 값을 그리는지 확인할 방법도 사라진다.
            NewChild(go, "CostLabel").AddComponent<TextMeshProUGUI>();
            NewChild(go, "Coin");

            go.AddComponent<Image>();
            go.AddComponent<Button>();
            return go.AddComponent<CardView>();
        }

        private static GameObject NewChild(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _garbage.Add(go);
            return go;
        }
    }
}
