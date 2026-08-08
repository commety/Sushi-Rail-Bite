using System;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 덱 보기의 로직. <b>Unity API 를 모른다</b> — 뷰는 인터페이스로만 본다
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// <b>일시정지를 건드리지 않는다</b> (README D8). 덱 확인은 정보 조회이고 멈추는 것은
    /// 메뉴의 일이다 — 한 화면이 둘을 하면 나중에 한쪽만 고쳐진다.
    /// </para>
    /// </summary>
    public sealed class DeckPanelPresenterTests
    {
        private readonly List<Object> _garbage = new();

        private FakeDeckPanelView _view;
        private RunState _run;
        private DeckPanelPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeDeckPanelView();
            _run = new RunState(new SushiDeck(new[] { NewSushi("장어"), NewSushi("연어") }),
                                new CustomerDeck(), seed: 1);
            _presenter = new DeckPanelPresenter(_view, _run);
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
        public void Fresh_IsClosed()
        {
            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(0, _view.ShowCount);
        }

        [Test]
        public void Open_WithDeck_ShowsEveryCard()
        {
            _presenter.Open();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(1, _view.ShowCount);
            Assert.AreEqual(2, _view.LastShown.Count);
            Assert.AreEqual(2, _presenter.CardCount);
        }

        /// <summary>
        /// 빈 덱이어도 연다. 조용히 안 열면 "덱 버튼이 고장 났다" 와 구분되지 않는다 —
        /// <c>RewardSelectionPresenter</c> 가 후보 0개에 내린 판단과 같다.
        /// </summary>
        [Test]
        public void Open_EmptyDeck_StillOpens()
        {
            var empty = new DeckPanelPresenter(_view, new RunState(new SushiDeck(),
                                                                   new CustomerDeck(), seed: 1));

            empty.Open();

            Assert.IsTrue(empty.IsOpen);
            Assert.AreEqual(1, _view.ShowCount);
            Assert.IsEmpty(_view.LastShown);
        }

        [Test]
        public void Close_AfterOpen_HidesAndForgetsCount()
        {
            _presenter.Open();

            _presenter.Close();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(1, _view.HideCount);
            Assert.AreEqual(0, _presenter.CardCount);
        }

        [Test]
        public void Close_WhenAlreadyClosed_DoesNotHideAgain()
        {
            _presenter.Close();

            Assert.AreEqual(0, _view.HideCount);
        }

        [Test]
        public void Toggle_WhenClosed_Opens()
        {
            _presenter.Toggle();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(1, _view.ShowCount);
        }

        [Test]
        public void Toggle_WhenOpen_Closes()
        {
            _presenter.Open();

            _presenter.Toggle();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(1, _view.HideCount);
        }

        /// <summary>
        /// <b>이 단계의 실제 이유다.</b> 보상으로 얻은 초밥이 화면 어디에도 없어,
        /// "카드를 받았다" 가 로그라이트의 축적감으로 이어지지 않았다.
        /// </summary>
        [Test]
        public void Open_AfterRewardAdded_IncludesNewCard()
        {
            var reward = NewSushi("성게");
            _run.Sushi.TryAdd(reward);

            _presenter.Open();

            Assert.AreEqual(3, _presenter.CardCount);
            CollectionAssert.Contains(_view.LastShown, reward);
        }

        /// <summary>
        /// 덱의 진실은 런에 있다. 스테이지 설정의 스폰 구성을 읽으면 보상으로 얻은 카드가
        /// 영영 안 보인다 — <c>SushiDeck</c> 의 클래스 주석이 이미 세운 규칙이다.
        /// </summary>
        [Test]
        public void Open_ShowsDeckOrder()
        {
            _presenter.Open();

            Assert.AreSame(_run.Sushi.Cards[0], _view.LastShown[0]);
            Assert.AreSame(_run.Sushi.Cards[1], _view.LastShown[1]);
        }

        [Test]
        public void Constructor_NullView_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DeckPanelPresenter(null, _run));
        }

        [Test]
        public void Constructor_NullRun_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DeckPanelPresenter(_view, null));
        }

        private SushiData NewSushi(string displayName)
        {
            var sushi = ScriptableObject.CreateInstance<SushiData>();
            sushi.name = displayName;
            _garbage.Add(sushi);
            return sushi;
        }
    }
}
