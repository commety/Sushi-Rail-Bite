using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Tests.EditMode.Data;
using SushiDefense.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 보상 선택의 로직. <b>프레젠터는 Unity API 를 모른다</b> — 그래서 이 파일이 EditMode 에
    /// 있고, 뷰는 손으로 쓴 스텁으로 갈아 끼운다 (<c>CLAUDE.md</c> §3.6).
    /// </summary>
    public sealed class RewardSelectionPresenterTests
    {
        private const int Seed = 777;

        private readonly List<Object> _disposables = new();

        private RewardCatalog _catalog;
        private FakeRewardSelectionView _view;
        private RewardSelectionPresenter _presenter;
        private RunState _run;
        private List<RewardOffer> _chosen;
        private int _closedCount;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<RewardCatalog>();
            _view = new FakeRewardSelectionView();
            _run = new RunState(new SushiDeck(), new CustomerDeck(), Seed);
            _chosen = new List<RewardOffer>();
            _closedCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        [Test]
        public void Open_WithOffers_ShowsThemOnTheView()
        {
            AddSushiToPool();
            AddSushiToPool();
            SetOfferCount(2);
            Build();

            _presenter.Open(_run);

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(2, _presenter.OfferCount);
            Assert.AreEqual(2, _view.ShownOffers.Count);
            Assert.AreEqual(1, _view.ShowCount);
        }

        /// <summary>
        /// <b>후보가 0개여도 화면을 연다.</b> 조용히 건너뛰면 클리어했는데 아무 화면도 안 뜨는
        /// 상태가 되어, 보상 시스템이 고장 난 것과 구분되지 않는다. 착수 시점의 실제 상태이기도
        /// 하다 — 초밥 5종이 전부 stage01 시작 덱에 있어 미보유 카드가 0개다.
        /// </summary>
        [Test]
        public void Open_NoOffers_StillOpensWithEmptyList()
        {
            SetOfferCount(3);
            Build();

            _presenter.Open(_run);

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(0, _presenter.OfferCount);
            Assert.AreEqual(1, _view.ShowCount, "빈 목록이라도 화면은 열려야 한다");
            Assert.IsEmpty(_view.ShownOffers);
        }

        [Test]
        public void Open_Offers_CarryTheCardItself()
        {
            var sushi = AddSushiToPool();
            SerializedFieldSetter.SetString(sushi, "_displayName", "장어");
            SetOfferCount(1);
            Build();

            _presenter.Open(_run);

            Assert.AreEqual(1, _view.ShownOffers.Count);
            Assert.AreEqual(RewardKind.SushiCard, _view.ShownOffers[0].Kind);
            Assert.AreEqual("장어", _view.ShownOffers[0].Sushi.DisplayName);
        }

        [Test]
        public void Choose_ValidIndex_AppliesToRunAndCloses()
        {
            AddSushiToPool();
            SetOfferCount(1);
            Build();
            _presenter.Open(_run);

            Assert.IsTrue(_presenter.Choose(0));

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(1, _view.HideCount);
        }

        /// <summary>
        /// 완료 판정 <i>"고른 보상이 덱/손님 목록에 반영된다"</i> 를 코드로 고정한다.
        /// 이벤트가 났는지만 보면 알리기만 하고 반영은 안 하는 구현이 통과한다.
        /// </summary>
        [Test]
        public void Choose_SushiOffer_CardAppearsInDeck()
        {
            var card = AddSushiToPool();
            SetOfferCount(1);
            Build();
            _presenter.Open(_run);

            _presenter.Choose(0);

            Assert.IsTrue(_run.Sushi.Contains(card), "고른 카드가 덱에 들어가지 않았다");
        }

        [Test]
        public void Choose_CustomerOffer_MemberAppearsInRoster()
        {
            var member = AddCustomerToPool();
            SetOfferCount(1);
            Build();
            _presenter.Open(_run);

            _presenter.Choose(0);

            Assert.IsTrue(_run.Customers.Contains(member));
        }

        [Test]
        public void Choose_ValidIndex_RaisesRewardChosenOnce()
        {
            AddSushiToPool();
            SetOfferCount(1);
            Build();
            _presenter.Open(_run);

            _presenter.Choose(0);

            Assert.AreEqual(1, _chosen.Count);
            Assert.AreEqual(RewardKind.SushiCard, _chosen[0].Kind);
        }

        [Test]
        public void Choose_OutOfRange_ReturnsFalseAndStaysOpen()
        {
            AddSushiToPool();
            SetOfferCount(1);
            Build();
            _presenter.Open(_run);

            Assert.IsFalse(_presenter.Choose(5));
            Assert.IsFalse(_presenter.Choose(-1));

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(0, _chosen.Count);
            Assert.AreEqual(0, _view.HideCount);
        }

        [Test]
        public void Choose_AfterClosed_ReturnsFalse()
        {
            AddSushiToPool();
            SetOfferCount(1);
            Build();
            _presenter.Open(_run);
            _presenter.Choose(0);

            Assert.IsFalse(_presenter.Choose(0));

            Assert.AreEqual(1, _chosen.Count, "닫힌 뒤 다시 고를 수 없다");
        }

        /// <summary>
        /// 덱과 명부를 <b>둘 다</b> 확인한다. 하나만 보면 반대쪽에 몰래 추가하는 구현이 통과한다.
        /// </summary>
        [Test]
        public void Skip_Open_ClosesWithoutChangingRun()
        {
            AddSushiToPool();
            AddCustomerToPool();
            SetOfferCount(2);
            Build();
            _presenter.Open(_run);

            _presenter.Skip();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(0, _run.Sushi.Count);
            Assert.AreEqual(0, _run.Customers.Count);
        }

        [Test]
        public void Skip_RaisesClosedButNotRewardChosen()
        {
            AddSushiToPool();
            SetOfferCount(1);
            Build();
            _presenter.Open(_run);

            _presenter.Skip();

            Assert.AreEqual(0, _chosen.Count);
            Assert.AreEqual(1, _closedCount);
        }

        [Test]
        public void Choose_RaisesClosedToo()
        {
            AddSushiToPool();
            SetOfferCount(1);
            Build();
            _presenter.Open(_run);

            _presenter.Choose(0);

            Assert.AreEqual(1, _closedCount, "고르든 건너뛰든 닫힘은 한 번 발생한다");
        }

        [Test]
        public void Skip_NotOpen_DoesNothing()
        {
            Build();

            _presenter.Skip();

            Assert.AreEqual(0, _closedCount);
            Assert.AreEqual(0, _view.HideCount);
        }

        [Test]
        public void Open_Twice_ReplacesOffersInsteadOfStacking()
        {
            AddSushiToPool();
            AddSushiToPool();
            SetOfferCount(2);
            Build();

            _presenter.Open(_run);
            _presenter.Open(_run);

            Assert.AreEqual(2, _presenter.OfferCount);
            Assert.AreEqual(2, _view.ShownOffers.Count);
        }

        // ── 헬퍼 ────────────────────────────────────────────────

        private void Build()
        {
            _presenter = new RewardSelectionPresenter(_view, new RewardGenerator(_catalog));
            _presenter.RewardChosen += offer => _chosen.Add(offer);
            _presenter.Closed += () => _closedCount++;
        }

        private void SetOfferCount(int value)
        {
            SerializedFieldSetter.SetInt(_catalog, "_offerCount", value);
        }

        private SushiData AddSushiToPool()
        {
            var sushi = StageConfigBuilder.CreateSushi(_disposables);
            SerializedFieldSetter.AppendObject(_catalog, "_sushiPool", sushi);
            return sushi;
        }

        private CustomerData AddCustomerToPool()
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(customer);
            SerializedFieldSetter.AppendObject(_catalog, "_customerPool", customer);
            return customer;
        }
    }
}
