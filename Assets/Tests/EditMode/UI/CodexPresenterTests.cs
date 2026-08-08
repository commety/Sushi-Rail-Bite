using System;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using SushiDefense.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 백과사전의 로직. <b>소유 여부로 가리지 않는다</b> — 사전은 무엇이 있는지 알려 주는
    /// 화면이고, 미획득 카드를 숨기면 사전이 아니다.
    ///
    /// <para>
    /// 애셋을 디스크에서 로드하지 않는다. 카탈로그는
    /// <c>ScriptableObject.CreateInstance</c> 로 세운다 (<c>.claude/rules/tests.md</c> §4) —
    /// 실제 <c>CardCatalog.asset</c> 의 항목이 늘 때마다 깨지면 안 된다.
    /// </para>
    /// </summary>
    public sealed class CodexPresenterTests
    {
        private readonly List<Object> _disposables = new();

        private FakeCodexView _view;
        private CardCatalog _catalog;
        private CodexPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeCodexView();
            _catalog = NewCatalog();
            _presenter = new CodexPresenter(_view, _catalog);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _disposables.Count; i++)
            {
                Object.DestroyImmediate(_disposables[i]);
            }

            _disposables.Clear();
        }

        [Test]
        public void Fresh_IsClosedAndEmpty()
        {
            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(0, _presenter.EntryCount);
            Assert.AreEqual(0, _view.ShowCount);
        }

        /// <summary>
        /// <b>전부</b>가 와야 한다. 초밥만 세면 손님이 빠진 구현이 통과하므로 둘 다 박는다.
        /// </summary>
        [Test]
        public void Open_ShowsEverySushiAndCustomer()
        {
            AddSushi(3);
            AddCustomers(2);

            _presenter.Open();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(1, _view.ShowCount);
            Assert.AreEqual(3, _view.LastSushi.Count);
            Assert.AreEqual(2, _view.LastCustomers.Count);
        }

        [Test]
        public void EntryCount_MatchesCatalog()
        {
            AddSushi(8);
            AddCustomers(3);

            _presenter.Open();

            Assert.AreEqual(11, _presenter.EntryCount);
        }

        /// <summary>
        /// 상수 <c>11</c> 을 돌려주는 구현을 배제한다. 다른 크기에서도 자기 카탈로그를
        /// 세는지 본다.
        /// </summary>
        [Test]
        public void EntryCount_SmallerCatalog_MatchesThatCatalog()
        {
            AddSushi(2);
            AddCustomers(1);

            _presenter.Open();

            Assert.AreEqual(3, _presenter.EntryCount);
        }

        /// <summary>
        /// 카탈로그가 비는 것은 오류가 아니다 (<c>CardCatalog</c> 의 클래스 주석). 조용히
        /// 안 열면 사전 버튼이 고장 난 것과 구분되지 않는다.
        /// </summary>
        [Test]
        public void Open_EmptyCatalog_StillOpens()
        {
            _presenter.Open();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(1, _view.ShowCount);
            Assert.AreEqual(0, _presenter.EntryCount);
        }

        /// <summary>사전은 <b>런 상태를 보지 않는다.</b> 순서도 카탈로그 그대로여야 한다.</summary>
        [Test]
        public void Open_PreservesCatalogOrder()
        {
            var first = NewSushi();
            var second = NewSushi();
            SerializedFieldSetter.AppendObject(_catalog, "_allSushi", first);
            SerializedFieldSetter.AppendObject(_catalog, "_allSushi", second);

            _presenter.Open();

            Assert.AreSame(first, _view.LastSushi[0]);
            Assert.AreSame(second, _view.LastSushi[1]);
        }

        [Test]
        public void Close_AfterOpen_HidesAndResetsCount()
        {
            AddSushi(2);
            _presenter.Open();

            _presenter.Close();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(0, _presenter.EntryCount);
            Assert.AreEqual(1, _view.HideCount);
        }

        [Test]
        public void Close_WhenNeverOpened_DoesNothing()
        {
            _presenter.Close();

            Assert.AreEqual(0, _view.HideCount);
        }

        [Test]
        public void Open_AfterClose_ShowsAgain()
        {
            AddSushi(1);
            _presenter.Open();
            _presenter.Close();

            _presenter.Open();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(2, _view.ShowCount);
            Assert.AreEqual(1, _presenter.EntryCount);
        }

        [Test]
        public void Constructor_NullView_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CodexPresenter(null, _catalog));
        }

        [Test]
        public void Constructor_NullCatalog_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CodexPresenter(_view, null));
        }

        // ── 조립 ───────────────────────────────────────────────────────────

        private CardCatalog NewCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<CardCatalog>();
            _disposables.Add(catalog);
            return catalog;
        }

        private SushiData NewSushi()
        {
            var sushi = ScriptableObject.CreateInstance<SushiData>();
            _disposables.Add(sushi);
            return sushi;
        }

        private void AddSushi(int count)
        {
            for (var i = 0; i < count; i++)
            {
                SerializedFieldSetter.AppendObject(_catalog, "_allSushi", NewSushi());
            }
        }

        private void AddCustomers(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var customer = ScriptableObject.CreateInstance<CustomerData>();
                _disposables.Add(customer);
                SerializedFieldSetter.AppendObject(_catalog, "_allCustomers", customer);
            }
        }

        private sealed class FakeCodexView : ICodexView
        {
            public int ShowCount { get; private set; }

            public int HideCount { get; private set; }

            /// <summary>마지막으로 올라간 초밥. 프레젠터가 카탈로그의 목록을 그대로 넘긴다.</summary>
            public IReadOnlyList<SushiData> LastSushi { get; private set; } =
                Array.Empty<SushiData>();

            public IReadOnlyList<CustomerData> LastCustomers { get; private set; } =
                Array.Empty<CustomerData>();

            public void ShowEntries(IReadOnlyList<SushiData> sushi,
                                    IReadOnlyList<CustomerData> customers)
            {
                ShowCount++;
                LastSushi = sushi;
                LastCustomers = customers;
            }

            public void Hide()
            {
                HideCount++;
            }
        }
    }
}
