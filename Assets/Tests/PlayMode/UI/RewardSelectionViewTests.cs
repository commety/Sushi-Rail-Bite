using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.UI
{
    /// <summary>
    /// 화면은 <b>계산하지 않는다.</b> 프레젠터가 정한 후보를 카드로 옮기는 것까지만 본다 —
    /// 무엇을 제시할지·고른 것이 유효한지는 EditMode 의
    /// <c>RewardSelectionPresenterTests</c> 가 검증한다.
    ///
    /// <para>
    /// PlayMode 는 느리므로 <b>씬·컴포넌트가 실제로 필요한 것만</b> 여기 둔다
    /// (<c>.claude/rules/tests.md</c> §1).
    /// </para>
    /// </summary>
    public sealed class RewardSelectionViewTests
    {
        private const int CardSlots = 3;

        private readonly List<Object> _garbage = new();

        private RewardSelectionView _view;
        private CardView[] _cards;
        private TMP_Text _title;
        private Button _skip;

        /// <summary>
        /// 화면이 프레젠터에게 넘긴 선택. <see cref="RewardSelectionPresenter"/> 는 구체
        /// 타입이라 가짜를 물릴 수 없으므로 <b>진짜를 세우고 그 이벤트로 관측한다</b> —
        /// 실제 경로를 그대로 지나는 편이 배선을 더 잘 지킨다.
        /// </summary>
        private RewardOffer? _chosen;

        private bool _closed;

        [SetUp]
        public void SetUp()
        {
            var root = NewObject("RewardView");
            root.AddComponent<RectTransform>();

            _cards = new CardView[CardSlots];
            for (var i = 0; i < CardSlots; i++)
            {
                _cards[i] = NewCard(root, i);
            }

            var titleGo = new GameObject("RewardOffersLabel", typeof(RectTransform));
            titleGo.transform.SetParent(root.transform, false);
            _title = titleGo.AddComponent<TextMeshProUGUI>();

            var skipGo = new GameObject("SkipButton", typeof(RectTransform));
            skipGo.transform.SetParent(root.transform, false);
            skipGo.AddComponent<Image>();
            _skip = skipGo.AddComponent<Button>();

            _view = root.AddComponent<RewardSelectionView>();
            _view.Initialize(_cards, _title, _skip);

            _chosen = null;
            _closed = false;
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
        public void ShowOffers_Three_DrawsThreeCards()
        {
            _view.ShowOffers(new[] { Sushi("장어"), Sushi("연어"), Customer("먹보") });

            Assert.IsTrue(_view.IsShowing);
            Assert.AreEqual(3, _view.ShownCardCount);
            Assert.IsTrue(_cards[0].IsShowing);
            Assert.IsTrue(_cards[2].IsShowing);
        }

        /// <summary>
        /// 손님 보상에는 <b>영입 비용이 함께</b> 보여야 한다. 유형마다 비용이 달라서, 값을
        /// 모르면 "얻고 나서 예산이 모자라 못 앉히는" 상황을 고르는 시점에 예측할 수 없다.
        /// </summary>
        [Test]
        public void ShowOffers_CustomerCard_ShowsRecruitCost()
        {
            _view.ShowOffers(new[] { Customer("먹보", recruitCost: 60) });

            StringAssert.Contains("60", _cards[0].DetailText);
        }

        [Test]
        public void ShowOffers_SushiCard_ShowsPrice()
        {
            _view.ShowOffers(new[] { Sushi("장어", price: 300) });

            StringAssert.Contains("300", _cards[0].DetailText);
        }

        /// <summary>
        /// 후보가 0개인 것은 정상이다 (카탈로그의 카드를 전부 가진 뒤). 빈 화면을 그리면
        /// 보상 시스템이 고장 난 것과 구분되지 않는다.
        /// </summary>
        [Test]
        public void ShowOffers_Empty_ShowsNoticeAndKeepsSkip()
        {
            _view.ShowOffers(System.Array.Empty<RewardOffer>());

            Assert.IsTrue(_view.IsShowing);
            StringAssert.Contains("없음", _view.OffersText);
            Assert.IsTrue(_skip.gameObject.activeSelf,
                          "건너뛰기가 사라지면 이 화면에서 나갈 수 없다");
        }

        /// <summary>
        /// <b>이 단계의 핵심 테스트다.</b> "누르면 골라진다" 만 보면 어느 카드를 눌러도
        /// 0번을 고르는 구현이 통과한다.
        /// </summary>
        [Test]
        public void ClickCard_Second_ChoosesTheSecondOffer()
        {
            OpenWithPresenter(offerCount: 3);

            // 고르면 화면이 닫히면서 카드가 비므로 **누르기 전에** 잡아 둔다.
            var expected = _cards[1].NameText;
            ClickCard(1);

            Assert.IsTrue(_chosen.HasValue, "카드를 눌렀는데 아무것도 고르지 않았다");
            Assert.AreEqual(expected, NameOf(_chosen.Value));
        }

        [Test]
        public void ClickCard_First_ChoosesTheFirstOffer()
        {
            OpenWithPresenter(offerCount: 3);

            var expected = _cards[0].NameText;
            ClickCard(0);

            Assert.IsTrue(_chosen.HasValue);
            Assert.AreEqual(expected, NameOf(_chosen.Value));
        }

        [Test]
        public void ClickSkip_ClosesWithoutChoosing()
        {
            OpenWithPresenter(offerCount: 3);

            _skip.onClick.Invoke();

            Assert.IsTrue(_closed);
            Assert.IsFalse(_chosen.HasValue);
            Assert.IsFalse(_view.IsShowing);
        }

        [Test]
        public void ShowOffers_ThenFewer_DoesNotLeaveStaleCards()
        {
            _view.ShowOffers(new[] { Sushi("장어"), Sushi("연어"), Sushi("참치") });

            _view.ShowOffers(new[] { Sushi("계란") });

            Assert.AreEqual(1, _view.ShownCardCount);
            Assert.IsFalse(_cards[1].IsShowing);
            Assert.IsFalse(_cards[2].IsShowing);
        }

        [Test]
        public void Hide_AfterShow_ClearsEveryCard()
        {
            _view.ShowOffers(new[] { Sushi("장어"), Sushi("연어") });

            _view.Hide();

            Assert.IsFalse(_view.IsShowing);
            Assert.IsFalse(_cards[0].IsShowing);
            Assert.IsEmpty(_view.OffersText);
        }

        /// <summary>빈 카드는 눌려도 아무 일이 없다 — <c>CardView</c> 가 먼저 막는다.</summary>
        [Test]
        public void ClickCard_AfterHide_ChoosesNothing()
        {
            OpenWithPresenter(offerCount: 3);
            _view.Hide();

            ClickCard(0);

            Assert.IsFalse(_chosen.HasValue);
        }

        // ── 조립 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 카드의 <c>Button</c> 을 통해 누른다. 뷰에 테스트용 진입점을 열면 배선이
        /// 사각지대가 된다.
        /// </summary>
        private void ClickCard(int index)
        {
            _cards[index].GetComponent<Button>().onClick.Invoke();
        }

        /// <summary>
        /// 진짜 프레젠터를 세워 화면을 연다. 어느 후보가 몇 번째로 뽑혔는지는 추첨이
        /// 정하므로, <b>화면에 그려진 이름과 대조</b>해 «올바른 번호를 넘겼는가» 를 본다 —
        /// 뽑기 순서를 테스트가 알 필요가 없다.
        /// </summary>
        private void OpenWithPresenter(int offerCount)
        {
            var catalog = ScriptableObject.CreateInstance<RewardCatalog>();
            _garbage.Add(catalog);

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(catalog);
            serialized.FindProperty("_offerCount").intValue = offerCount;

            var pool = serialized.FindProperty("_sushiPool");
            for (var i = 0; i < offerCount; i++)
            {
                var sushi = ScriptableObject.CreateInstance<SushiData>();
                sushi.name = $"Sushi{i}";
                _garbage.Add(sushi);

                var inner = new UnityEditor.SerializedObject(sushi);
                inner.FindProperty("_displayName").stringValue = $"초밥{i}";
                inner.FindProperty("_price").intValue = 100 + (i * 50);
                inner.ApplyModifiedPropertiesWithoutUndo();

                pool.InsertArrayElementAtIndex(i);
                pool.GetArrayElementAtIndex(i).objectReferenceValue = sushi;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            var run = new RunState(new SushiDeck(), new CustomerDeck(), seed: 7);
            var presenter = new RewardSelectionPresenter(_view, new RewardGenerator(catalog));
            presenter.RewardChosen += offer => _chosen = offer;
            presenter.Closed += () => _closed = true;

            _view.Bind(presenter);
            presenter.Open(run);
        }

        private static string NameOf(RewardOffer offer)
        {
            return offer.Kind == RewardKind.SushiCard
                ? CardCaption.NameOf(offer.Sushi)
                : CardCaption.NameOf(offer.Customer);
        }

        private CardView NewCard(GameObject parent, int index)
        {
            var go = new GameObject($"Card{index}", typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);

            var icon = new GameObject("Icon", typeof(RectTransform));
            icon.transform.SetParent(go.transform, false);
            icon.AddComponent<Image>();

            var nameLabel = new GameObject("NameLabel", typeof(RectTransform));
            nameLabel.transform.SetParent(go.transform, false);
            nameLabel.AddComponent<TextMeshProUGUI>();

            var detailLabel = new GameObject("DetailLabel", typeof(RectTransform));
            detailLabel.transform.SetParent(go.transform, false);
            detailLabel.AddComponent<TextMeshProUGUI>();

            go.AddComponent<Image>();
            go.AddComponent<Button>();
            return go.AddComponent<CardView>();
        }

        private RewardOffer Sushi(string displayName, int price = 150)
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

            return RewardOffer.OfSushi(sushi);
        }

        private RewardOffer Customer(string displayName, int recruitCost = 20)
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            customer.name = displayName;
            _garbage.Add(customer);

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(customer);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_recruitCost").intValue = recruitCost;
            serialized.FindProperty("_targetingMin").intValue = 100;
            serialized.FindProperty("_targetingMax").intValue = 300;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return RewardOffer.OfCustomer(customer);
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _garbage.Add(go);
            return go;
        }
    }
}
