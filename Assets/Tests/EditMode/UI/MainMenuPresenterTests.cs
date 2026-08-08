using System;
using NUnit.Framework;
using SushiDefense.Navigation;
using SushiDefense.UI;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 메인 화면의 로직. <b>Unity API 를 모른다</b> — 씬 전환이 인터페이스 뒤에 있어
    /// 씬을 띄우지 않고 «시작을 누르면 스테이지로 간다» 를 확인할 수 있다
    /// (<c>CLAUDE.md</c> §3.6).
    /// </summary>
    public sealed class MainMenuPresenterTests
    {
        private FakeMainMenuView _view;
        private FakeSceneRouter _router;
        private MainMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeMainMenuView();
            _router = new FakeSceneRouter();
            _presenter = new MainMenuPresenter(_view, _router);
        }

        [Test]
        public void Fresh_IsClosedAndHasNotChangedScene()
        {
            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(0, _router.LoadStageCount);
            Assert.AreEqual(0, _router.LoadMainCount);
        }

        [Test]
        public void Open_ShowsMenuOnce()
        {
            _presenter.Open();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(1, _view.ShowCount);
        }

        /// <summary>
        /// 이미 떠 있으면 다시 그리지 않는다 — 같은 상태를 두 번 그리면 구독자가 같은
        /// 전이를 여러 번 처리한다.
        /// </summary>
        [Test]
        public void Open_Twice_ShowsOnce()
        {
            _presenter.Open();

            _presenter.Open();

            Assert.AreEqual(1, _view.ShowCount);
        }

        [Test]
        public void StartGame_CallsLoadStageOnce()
        {
            _presenter.StartGame();

            Assert.AreEqual(1, _router.LoadStageCount);
        }

        /// <summary>
        /// 이것이 없으면 공허하다 — <c>LoadStage</c> 와 <c>LoadMain</c> 을 <b>둘 다</b>
        /// 부르는 구현도 <see cref="StartGame_CallsLoadStageOnce"/> 를 통과한다.
        /// </summary>
        [Test]
        public void StartGame_DoesNotLoadMain()
        {
            _presenter.StartGame();

            Assert.AreEqual(0, _router.LoadMainCount, "시작이 메인을 다시 여는 구현");
        }

        /// <summary>
        /// 씬이 통째로 갈리므로 <b>내릴 화면이 남지 않는다.</b> 여기서 내리면 로드가
        /// 거부됐을 때 아무것도 없는 화면만 남는다.
        /// </summary>
        [Test]
        public void StartGame_DoesNotHideMenu()
        {
            _presenter.Open();

            _presenter.StartGame();

            Assert.AreEqual(0, _view.HideCount);
            Assert.IsTrue(_presenter.IsOpen);
        }

        [Test]
        public void OpenSettings_DoesNotChangeScene()
        {
            _presenter.Open();

            _presenter.OpenSettings();

            Assert.AreEqual(0, _router.LoadStageCount);
            Assert.AreEqual(0, _router.LoadMainCount);
        }

        [Test]
        public void OpenCodex_DoesNotChangeScene()
        {
            _presenter.Open();

            _presenter.OpenCodex();

            Assert.AreEqual(0, _router.LoadStageCount);
            Assert.AreEqual(0, _router.LoadMainCount);
        }

        /// <summary>
        /// 패널 뒤에 남은 버튼은 <b>보이지 않으면서도 눌린다.</b> 설정을 만지다 스테이지가
        /// 시작되는 사고가 그렇게 난다.
        /// </summary>
        [Test]
        public void OpenSettings_HidesMenu()
        {
            _presenter.Open();

            _presenter.OpenSettings();

            Assert.AreEqual(1, _view.HideCount);
            Assert.IsFalse(_presenter.IsOpen);
        }

        [Test]
        public void OpenCodex_HidesMenu()
        {
            _presenter.Open();

            _presenter.OpenCodex();

            Assert.AreEqual(1, _view.HideCount);
            Assert.IsFalse(_presenter.IsOpen);
        }

        [Test]
        public void OpenSettings_RaisesSettingsRequestedOnce()
        {
            var settings = 0;
            _presenter.SettingsRequested += () => settings++;

            _presenter.OpenSettings();

            Assert.AreEqual(1, settings);
        }

        /// <summary>둘을 뒤바꾼 구현을 배제한다. 요청 횟수만 세면 구분되지 않는다.</summary>
        [Test]
        public void OpenSettings_DoesNotRaiseCodexRequested()
        {
            var codex = 0;
            _presenter.CodexRequested += () => codex++;

            _presenter.OpenSettings();

            Assert.AreEqual(0, codex);
        }

        [Test]
        public void OpenCodex_RaisesCodexRequestedOnce()
        {
            var codex = 0;
            _presenter.CodexRequested += () => codex++;

            _presenter.OpenCodex();

            Assert.AreEqual(1, codex);
        }

        [Test]
        public void OpenCodex_DoesNotRaiseSettingsRequested()
        {
            var settings = 0;
            _presenter.SettingsRequested += () => settings++;

            _presenter.OpenCodex();

            Assert.AreEqual(0, settings);
        }

        /// <summary>구독자가 없어도 메뉴는 내려간다 — 패널이 아직 없는 단계의 상태다.</summary>
        [Test]
        public void OpenSettings_NoSubscriber_StillHidesMenu()
        {
            _presenter.Open();

            Assert.DoesNotThrow(() => _presenter.OpenSettings());
            Assert.AreEqual(1, _view.HideCount);
        }

        /// <summary>
        /// 패널을 닫고 <b>돌아오는 길</b>. 이것이 없으면 설정을 한 번 연 뒤 메뉴가
        /// 영영 돌아오지 않는다.
        /// </summary>
        [Test]
        public void Open_AfterOpenSettings_ShowsMenuAgain()
        {
            _presenter.Open();
            _presenter.OpenSettings();

            _presenter.Open();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.AreEqual(2, _view.ShowCount);
        }

        [Test]
        public void Constructor_NullView_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MainMenuPresenter(null, _router));
        }

        [Test]
        public void Constructor_NullRouter_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MainMenuPresenter(_view, null));
        }

        private sealed class FakeMainMenuView : IMainMenuView
        {
            public int ShowCount { get; private set; }

            public int HideCount { get; private set; }

            public void ShowMenu()
            {
                ShowCount++;
            }

            public void Hide()
            {
                HideCount++;
            }
        }

        private sealed class FakeSceneRouter : ISceneRouter
        {
            public int LoadMainCount { get; private set; }

            public int LoadStageCount { get; private set; }

            public void LoadMain()
            {
                LoadMainCount++;
            }

            public void LoadStage()
            {
                LoadStageCount++;
            }
        }
    }
}
