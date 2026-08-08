using System;
using NUnit.Framework;
using SushiDefense.Navigation;
using SushiDefense.Stages;
using SushiDefense.UI;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 인스테이지 메뉴의 로직. <b>Unity API 를 모른다</b> — 씬 전환도 재시작도 인터페이스
    /// 뒤에 있다 (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// 스테이지에 출구가 없었다. 시작하면 실패하거나 클리어할 때까지 나갈 수 없고 멈출 수도
    /// 없어, 웹 데모에서는 새로고침 말고 방법이 없었다.
    /// </para>
    /// </summary>
    public sealed class StageMenuPresenterTests
    {
        private FakeStageMenuView _view;
        private PauseState _pause;
        private FakeStageRestarter _restarter;
        private FakeSceneRouter _router;
        private StageMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeStageMenuView();
            _pause = new PauseState();
            _restarter = new FakeStageRestarter();
            _router = new FakeSceneRouter();
            _presenter = new StageMenuPresenter(_view, _pause, _restarter, _router);
            _restarter.Watching = _presenter;
        }

        [Test]
        public void Fresh_IsClosedAndRunning()
        {
            Assert.IsFalse(_presenter.IsOpen);
            Assert.IsFalse(_pause.IsPaused);
        }

        /// <summary>
        /// <b>여는 순간 멈춘다.</b> 메뉴를 보는 동안 벨트가 흐르면 메뉴가 곧 페널티가 된다 —
        /// <c>Pause</c>/<c>Play</c> 버튼은 메뉴를 연 채로 다시 흘려 보고 싶을 때의 것이다.
        /// </summary>
        [Test]
        public void Open_WhenRunning_PausesAndShows()
        {
            _presenter.Open();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.IsTrue(_pause.IsPaused);
            Assert.AreEqual(1, _view.ShowCount);
            Assert.IsTrue(_view.LastShownPaused);
        }

        [Test]
        public void Close_WhenPaused_Resumes()
        {
            _presenter.Open();

            _presenter.Close();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.IsFalse(_pause.IsPaused);
            Assert.AreEqual(1, _view.HideCount);
        }

        [Test]
        public void Pause_Twice_StaysPausedAndShowsOnce()
        {
            _presenter.Open();
            _view.Reset();

            _presenter.Pause();

            Assert.IsTrue(_pause.IsPaused);
            Assert.AreEqual(0, _view.ShowCount, "이미 멈춰 있으면 화면을 다시 그리지 않는다");
        }

        /// <summary>
        /// 메뉴를 연 채로 다시 흘려 볼 수 있어야 한다 — 그것이 <c>Play</c> 버튼의 쓰임새다.
        /// </summary>
        [Test]
        public void Resume_WhileMenuOpen_KeepsMenuOpen()
        {
            _presenter.Open();

            _presenter.Resume();

            Assert.IsFalse(_pause.IsPaused);
            Assert.IsTrue(_presenter.IsOpen, "재개가 메뉴를 닫아 버리면 다시 멈출 수가 없다");
        }

        [Test]
        public void Restart_CallsRestarterOnce()
        {
            _presenter.Open();

            _presenter.Restart();

            Assert.AreEqual(1, _restarter.RestartCount);
        }

        /// <summary>
        /// <b>메뉴를 먼저 내린다.</b> 재시작은 판을 다시 세우는데 그때 메뉴가 떠 있으면
        /// 새 판 위에 겹쳐 남는다 — <c>StageTransitionPresenter.Proceed</c> 가 같은 사고를
        /// 이미 막아 둔 자리다.
        /// </summary>
        [Test]
        public void Restart_ClosesMenuBeforeRestarting()
        {
            _presenter.Open();

            _presenter.Restart();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.IsTrue(_restarter.MenuWasClosedWhenCalled,
                          "판을 다시 세울 때 메뉴가 아직 떠 있었다");
        }

        /// <summary>재시작한 판이 멈춘 채로 열리면 고장으로 보인다.</summary>
        [Test]
        public void Restart_ResumesTime()
        {
            _presenter.Open();

            _presenter.Restart();

            Assert.IsFalse(_pause.IsPaused);
        }

        [Test]
        public void QuitToMain_CallsRouterOnce()
        {
            _presenter.Open();

            _presenter.QuitToMain();

            Assert.AreEqual(1, _router.LoadMainCount);
        }

        /// <summary>
        /// 나가기와 재시작을 뒤바꾼 구현을 배제한다. 한쪽만 세면 둘 다 부르는 구현도 통과한다.
        /// </summary>
        [Test]
        public void QuitToMain_DoesNotRestart()
        {
            _presenter.Open();

            _presenter.QuitToMain();

            Assert.AreEqual(0, _restarter.RestartCount);
            Assert.AreEqual(0, _router.LoadStageCount);
        }

        [Test]
        public void Restart_DoesNotChangeScene()
        {
            _presenter.Open();

            _presenter.Restart();

            Assert.AreEqual(0, _router.LoadMainCount);
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StageMenuPresenter(null, _pause, _restarter, _router));
            Assert.Throws<ArgumentNullException>(
                () => new StageMenuPresenter(_view, null, _restarter, _router));
            Assert.Throws<ArgumentNullException>(
                () => new StageMenuPresenter(_view, _pause, null, _router));
            Assert.Throws<ArgumentNullException>(
                () => new StageMenuPresenter(_view, _pause, _restarter, null));
        }

        private sealed class FakeStageMenuView : IStageMenuView
        {
            public int ShowCount { get; private set; }

            public int HideCount { get; private set; }

            public bool LastShownPaused { get; private set; }

            public void ShowMenu(bool paused)
            {
                ShowCount++;
                LastShownPaused = paused;
            }

            public void Hide()
            {
                HideCount++;
            }

            public void Reset()
            {
                ShowCount = 0;
                HideCount = 0;
            }
        }

        /// <summary>
        /// 재시작 시점에 <b>메뉴가 이미 닫혀 있었는지</b>를 함께 기록한다. 호출 횟수만 세면
        /// 순서를 뒤집은 구현도 통과한다.
        /// </summary>
        private sealed class FakeStageRestarter : IStageRestarter
        {
            public StageMenuPresenter Watching { get; set; }

            public int RestartCount { get; private set; }

            public bool MenuWasClosedWhenCalled { get; private set; }

            public void Restart()
            {
                RestartCount++;
                MenuWasClosedWhenCalled = Watching == null || !Watching.IsOpen;
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
