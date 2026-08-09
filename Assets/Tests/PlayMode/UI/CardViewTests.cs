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
    /// 카드 한 장의 화면 표현. <b>판정하지 않는다</b> — 무엇을 보여줄지는 부르는 쪽이 정한다.
    ///
    /// <para>
    /// 자식을 인스펙터로 물리지 않고 <b>이름으로 찾게</b> 세운다. 참조를 직접 주입하면
    /// "비었을 때 찾는" 분기가 죽어 그 폴백이 사각지대가 된다
    /// (<c>.claude/rules/tests.md</c> §1).
    /// </para>
    /// </summary>
    public sealed class CardViewTests
    {
        private readonly List<Object> _garbage = new();

        private CardView _card;
        private Button _button;
        private Image _icon;
        private TMP_Text _nameLabel;
        private TMP_Text _detailLabel;
        private TMP_Text _costLabel;
        private GameObject _coin;
        private Sprite _fallbackSprite;

        [SetUp]
        public void SetUp()
        {
            var root = NewObject("Card");
            root.AddComponent<RectTransform>();

            _icon = NewChild(root, "Icon").AddComponent<Image>();
            _fallbackSprite = NewSprite();
            _icon.sprite = _fallbackSprite;

            _nameLabel = NewChild(root, "NameLabel").AddComponent<TextMeshProUGUI>();
            _detailLabel = NewChild(root, "DetailLabel").AddComponent<TextMeshProUGUI>();
            _costLabel = NewChild(root, "CostLabel").AddComponent<TextMeshProUGUI>();
            _coin = NewChild(root, "Coin");
            _button = root.AddComponent<Button>();

            // 자식이 전부 선 뒤에 붙인다 — Awake 가 이 시점에 돌면서 이름으로 찾는다.
            _card = root.AddComponent<CardView>();
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

        [Test]
        public void Show_Sushi_WritesNameDetailAndIcon()
        {
            var icon = NewSprite();
            var sushi = NewSushi("참치", 250, 2, icon);

            _card.Show(sushi);

            Assert.IsTrue(_card.IsShowing);
            Assert.AreEqual("참치", _nameLabel.text);
            StringAssert.Contains("250", _detailLabel.text);
            Assert.AreSame(icon, _card.ShownSprite);
        }

        /// <summary>
        /// M6.5 에서 영입 비용이 수치 줄에서 <b>우상단 동전으로</b> 옮겨 갔다. 대역은 줄에
        /// 남고 비용은 동전이 진다 — 둘 다 적으면 같은 값이 카드에 두 번 나온다.
        /// </summary>
        [Test]
        public void Show_Customer_WritesBandInTheRowsAndCostInTheCoin()
        {
            var customer = NewCustomer("기본", 100, 300, 20, NewSprite());

            _card.Show(customer);

            Assert.AreEqual("기본", _nameLabel.text);
            StringAssert.Contains("100", _detailLabel.text);
            StringAssert.Contains("300", _detailLabel.text);
            Assert.AreEqual("20", _costLabel.text);
        }

        /// <summary>
        /// 카드는 재사용된다. 직전 내용이 한 줄이라도 남으면 다른 카드가 섞여 보인다 —
        /// M5 에서 풀 반납 시 그림을 되돌리지 않아 재사용 첫 프레임에 직전 초밥이 번쩍였다.
        /// </summary>
        [Test]
        public void Show_CustomerThenSushi_ReplacesEveryField()
        {
            _card.Show(NewCustomer("기본", 100, 300, 20, NewSprite()));

            var sushiIcon = NewSprite();
            _card.Show(NewSushi("참치", 250, 2, sushiIcon));

            Assert.AreEqual("참치", _nameLabel.text);
            StringAssert.DoesNotContain("영입", _detailLabel.text);
            Assert.AreSame(sushiIcon, _card.ShownSprite);
        }

        /// <summary>
        /// 아이콘이 비면 프리팹의 그림을 그대로 둔다. 빈 화면을 만들지 않는다 —
        /// 아이콘을 아직 안 채운 애셋에서 카드가 사라지면 목록이 고장 난 것처럼 보인다.
        /// </summary>
        [Test]
        public void Show_SushiWithoutIcon_KeepsPrefabSprite()
        {
            _card.Show(NewSushi("계란", 130, 1, null));

            Assert.AreSame(_fallbackSprite, _card.ShownSprite);
        }

        [Test]
        public void Clear_AfterShow_EmptiesLabelsAndRestoresSprite()
        {
            _card.Show(NewSushi("참치", 250, 2, NewSprite()));

            _card.Clear();

            Assert.IsFalse(_card.IsShowing);
            Assert.IsEmpty(_nameLabel.text);
            Assert.IsEmpty(_detailLabel.text);
            Assert.AreSame(_fallbackSprite, _card.ShownSprite);
        }

        [Test]
        public void Clear_AfterShow_DeactivatesObject()
        {
            _card.Show(NewSushi("참치", 250, 2, NewSprite()));

            _card.Clear();

            Assert.IsFalse(_card.gameObject.activeSelf);
        }

        [Test]
        public void Show_AfterClear_ReactivatesObject()
        {
            _card.Clear();

            _card.Show(NewSushi("참치", 250, 2, NewSprite()));

            Assert.IsTrue(_card.gameObject.activeSelf);
        }

        /// <summary>
        /// <b>버튼을 통해</b> 누른다. 뷰에 테스트용 진입점을 열면 <c>onClick</c> 배선이
        /// 사각지대가 된다 — 실제 클릭이 <c>EventSystem</c> 을 거쳐 배달되는지는 씬 테스트
        /// (step-12)의 몫이고, 여기서는 그 앞단까지 본다.
        /// </summary>
        [Test]
        public void Click_WhenShowing_RaisesClickedWithSelf()
        {
            _card.Show(NewSushi("참치", 250, 2, NewSprite()));

            CardView clicked = null;
            var count = 0;
            _card.Clicked += card =>
            {
                clicked = card;
                count++;
            };

            _button.onClick.Invoke();

            Assert.AreEqual(1, count);
            Assert.AreSame(_card, clicked);
        }

        /// <summary>
        /// 빈 카드는 눌려도 아무 일이 없다. 목록이 자리보다 짧을 때 남는 카드가 눌리면
        /// 부르는 쪽이 없는 인덱스를 고르게 된다.
        /// </summary>
        [Test]
        public void Click_AfterClear_DoesNotRaise()
        {
            _card.Show(NewSushi("참치", 250, 2, NewSprite()));
            _card.Clear();

            var count = 0;
            _card.Clicked += _ => count++;

            _button.onClick.Invoke();

            Assert.AreEqual(0, count);
        }

        // ── 우상단 동전 (M6.5) ─────────────────────────────────────────────

        [Test]
        public void Show_Customer_WritesTheRecruitCost()
        {
            _card.Show(NewCustomer("먹보", 100, 150, 60, NewSprite()));

            Assert.AreEqual("60", _costLabel.text);
            Assert.AreEqual("60", _card.CostText);
            Assert.IsTrue(_coin.activeSelf, "동전이 꺼져 있다");
        }

        /// <summary>
        /// 반례. <b>초밥에는 영입 비용이 없다</b> — 동전을 켜 둔 채로 두면 값 없는 동전이
        /// 남는다.
        /// </summary>
        [Test]
        public void Show_Sushi_HidesTheCoin()
        {
            _card.Show(NewSushi("참치", 250, 2, NewSprite()));

            Assert.IsEmpty(_costLabel.text);
            Assert.IsFalse(_coin.activeSelf, "초밥 카드에 값 없는 동전이 남았다");
        }

        /// <summary>
        /// 손님을 그린 카드를 초밥으로 재사용할 때 <b>직전 비용이 번쩍이지 않아야</b> 한다 —
        /// 풀에서 그림을 되돌리는 것과 같은 이유다.
        /// </summary>
        [Test]
        public void Show_SushiAfterCustomer_ClearsTheCost()
        {
            _card.Show(NewCustomer("먹보", 100, 150, 60, NewSprite()));

            _card.Show(NewSushi("참치", 250, 2, NewSprite()));

            Assert.IsEmpty(_costLabel.text);
            Assert.IsFalse(_coin.activeSelf);
        }

        [Test]
        public void Clear_ResetsTheCost()
        {
            _card.Show(NewCustomer("먹보", 100, 150, 60, NewSprite()));

            _card.Clear();

            Assert.IsEmpty(_costLabel.text);
            Assert.IsFalse(_coin.activeSelf);
        }

        private SushiData NewSushi(string displayName, int price, int saturation, Sprite icon)
        {
            var sushi = ScriptableObject.CreateInstance<SushiData>();
            sushi.name = "Sushi.Test";
            _garbage.Add(sushi);

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(sushi);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_price").intValue = price;
            serialized.FindProperty("_saturationAmount").intValue = saturation;
            serialized.FindProperty("_icon").objectReferenceValue = icon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return sushi;
        }

        private CustomerData NewCustomer(string displayName, int min, int max, int cost, Sprite icon)
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            customer.name = "Customer.Test";
            _garbage.Add(customer);

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(customer);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_targetingMin").intValue = min;
            serialized.FindProperty("_targetingMax").intValue = max;
            serialized.FindProperty("_recruitCost").intValue = cost;
            serialized.FindProperty("_icon").objectReferenceValue = icon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return customer;
        }

        private Sprite NewSprite()
        {
            var texture = new Texture2D(4, 4);
            _garbage.Add(texture);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f);
            _garbage.Add(sprite);
            return sprite;
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _garbage.Add(go);
            return go;
        }

        private GameObject NewChild(GameObject parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child;
        }
    }
}
