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
    /// 덱 화면은 <b>계산하지 않는다.</b> 프레젠터가 정한 목록을 카드에 옮기기만 한다.
    ///
    /// <para>
    /// 카드는 미리 놓인다 (<c>CLAUDE.md</c> §3.4). 덱은 자라지만 카탈로그 크기를 넘지
    /// 못하므로 자리 수를 정할 수 있고, 그래도 넘치면 <b>있는 만큼만 그리고 예외를 내지
    /// 않는다</b> — 씬을 조금씩 조립하는 동안 흔한 상태다.
    /// </para>
    /// </summary>
    public sealed class DeckPanelViewTests
    {
        private const int CardSlots = 3;

        private readonly List<Object> _garbage = new();

        private DeckPanelView _view;
        private CardView[] _cards;
        private TMP_Text _notice;
        private GameObject _panelRoot;

        [SetUp]
        public void SetUp()
        {
            var root = NewObject("DeckPanel");

            _panelRoot = new GameObject("Panel", typeof(RectTransform));
            _panelRoot.transform.SetParent(root.transform, false);

            _cards = new CardView[CardSlots];
            for (var i = 0; i < CardSlots; i++)
            {
                _cards[i] = NewCard(_panelRoot, i);
            }

            var noticeGo = new GameObject("EmptyNotice", typeof(RectTransform));
            noticeGo.transform.SetParent(_panelRoot.transform, false);
            _notice = noticeGo.AddComponent<TextMeshProUGUI>();

            _view = root.AddComponent<DeckPanelView>();
            _view.Initialize(_panelRoot, _cards, _notice);
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
        public void ShowDeck_TwoCards_DrawsBoth()
        {
            _view.ShowDeck(new[] { NewSushi("장어"), NewSushi("연어") });

            Assert.IsTrue(_view.IsShowing);
            Assert.IsTrue(_cards[0].IsShowing);
            Assert.IsTrue(_cards[1].IsShowing);
            Assert.IsFalse(_cards[2].IsShowing, "남는 자리는 비어 있어야 한다");
        }

        /// <summary>
        /// 카드를 추가할 때 조용히 잘리는 것을 알아채야 한다. 예외는 내지 않는다 —
        /// 씬을 조금씩 조립하는 동안 흔한 상태이고, 여기서 터지면 나머지를 아무것도 못 본다.
        /// </summary>
        [Test]
        public void ShowDeck_MoreCardsThanSlots_DrawsWhatFits()
        {
            var deck = new[] { NewSushi("장어"), NewSushi("연어"), NewSushi("참치"), NewSushi("성게") };

            Assert.DoesNotThrow(() => _view.ShowDeck(deck));

            Assert.AreEqual(CardSlots, _view.ShownCardCount);
        }

        /// <summary>
        /// <b>줄어드는 방향</b>으로 본다. 늘어나는 방향은 덮어써지므로 남는 카드를 비우지
        /// 않는 구현도 통과한다.
        /// </summary>
        [Test]
        public void ShowDeck_ThenShorterDeck_DoesNotLeaveStaleCards()
        {
            _view.ShowDeck(new[] { NewSushi("장어"), NewSushi("연어"), NewSushi("참치") });

            _view.ShowDeck(new[] { NewSushi("계란") });

            Assert.AreEqual(1, _view.ShownCardCount);
            Assert.IsFalse(_cards[1].IsShowing);
            Assert.IsFalse(_cards[2].IsShowing);
        }

        /// <summary>
        /// 빈 덱은 실제로 나올 수 있는 상태다. 빈 패널을 그리면 덱 버튼이 고장 난 것과
        /// 구분되지 않는다.
        /// </summary>
        [Test]
        public void ShowDeck_Empty_ShowsNotice()
        {
            _view.ShowDeck(System.Array.Empty<SushiData>());

            Assert.IsTrue(_view.IsShowing);
            Assert.IsTrue(_notice.gameObject.activeSelf);
            Assert.IsNotEmpty(_notice.text);
        }

        [Test]
        public void ShowDeck_NonEmpty_HidesNotice()
        {
            _view.ShowDeck(new[] { NewSushi("장어") });

            Assert.IsFalse(_notice.gameObject.activeSelf);
        }

        [Test]
        public void Hide_AfterShow_ClearsEveryCardAndClosesPanel()
        {
            _view.ShowDeck(new[] { NewSushi("장어"), NewSushi("연어") });

            _view.Hide();

            Assert.IsFalse(_view.IsShowing);
            Assert.IsFalse(_panelRoot.activeSelf);
            Assert.IsFalse(_cards[0].IsShowing);
            Assert.IsFalse(_cards[1].IsShowing);
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

        private SushiData NewSushi(string displayName)
        {
            var sushi = ScriptableObject.CreateInstance<SushiData>();
            sushi.name = displayName;
            _garbage.Add(sushi);
            return sushi;
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _garbage.Add(go);
            return go;
        }
    }
}
