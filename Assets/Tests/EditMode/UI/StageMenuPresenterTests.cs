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
        private StageWindowArbiter _windows;
        private StageMenuPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeStageMenuView();
            _pause = new PauseState();
            _restarter = new FakeStageRestarter();
            _router = new FakeSceneRouter();
            _windows = new StageWindowArbiter();
            _presenter = new StageMenuPresenter(_view, _pause, _restarter, _router, _windows);
            _restarter.Watching = _presenter;
        }

        [Test]
        public void Fresh_IsClosedAndRunning()
        {
            Assert.IsFalse(_presenter.IsOpen);
            Assert.IsFalse(_pause.IsPaused);
        }

        /// <summary>
        /// <b>여는 순간 멈춘다.</b> 메뉴를 보는 동안 벨트가 흐르면 메뉴가 곧 페널티가 된다.
        /// </summary>
        [Test]
        public void Open_WhenRunning_PausesAndShows()
        {
            _presenter.Open();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.IsTrue(_pause.IsPaused);
            Assert.AreEqual(1, _view.ShowCount);
            Assert.IsTrue(_view.LastCanResume);
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

        /// <summary>
        /// 메뉴 아이콘으로 <b>닫을 수도 있어야</b> 한다. 덱 버튼이 이미 토글이라, 같은
        /// 자리의 두 아이콘이 다르게 동작하면 어느 쪽이 규칙인지 알 수 없다.
        /// </summary>
        [Test]
        public void Toggle_WhenClosed_Opens()
        {
            _presenter.Toggle();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.IsTrue(_pause.IsPaused);
        }

        [Test]
        public void Toggle_WhenOpen_ClosesAndResumes()
        {
            _presenter.Open();

            _presenter.Toggle();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.IsFalse(_pause.IsPaused);
            Assert.AreEqual(1, _view.HideCount);
        }

        /// <summary>
        /// 두 번 토글하면 제자리다. 한쪽 방향만 보는 테스트는 «항상 연다» 는 구현도
        /// 통과시킨다.
        /// </summary>
        [Test]
        public void Toggle_Twice_ReturnsToClosed()
        {
            _presenter.Toggle();
            _presenter.Toggle();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(1, _view.ShowCount);
            Assert.AreEqual(1, _view.HideCount);
        }

        /// <summary>
        /// <b>재개는 메뉴를 닫는다.</b> 메뉴가 곧 멈춤이므로, 열어 둔 채 시간만 흘리는
        /// 네 번째 상태를 만들지 않는다.
        /// </summary>
        [Test]
        public void Resume_ClosesMenuAndResumes()
        {
            _presenter.Open();

            _presenter.Resume();

            Assert.IsFalse(_pause.IsPaused);
            Assert.IsFalse(_presenter.IsOpen, "재개했는데 메뉴가 그대로면 판이 가려진다");
            Assert.AreEqual(1, _view.HideCount);
        }

        /// <summary>실패해서 열린 메뉴에는 돌아갈 판이 없다 — 재개 버튼이 빠진다.</summary>
        [Test]
        public void OpenAfterFailure_HidesResume()
        {
            _presenter.OpenAfterFailure();

            Assert.IsTrue(_presenter.IsOpen);
            Assert.IsFalse(_presenter.CanResume);
            Assert.IsFalse(_view.LastCanResume);
        }

        /// <summary>
        /// 실패 창에서도 다시 시작과 나가기는 그대로 동작한다 — 그것이 이 창을 재사용하는
        /// 이유다.
        /// </summary>
        [Test]
        public void OpenAfterFailure_StillRestarts()
        {
            _presenter.OpenAfterFailure();

            _presenter.Restart();

            Assert.AreEqual(1, _restarter.RestartCount);
            Assert.IsFalse(_presenter.IsOpen);
        }

        /// <summary>
        /// 더 높은 창은 없지만, 조정자가 거절하면 열리지 않아야 한다. 조정자를 아예
        /// 무시하는 구현을 배제한다.
        /// </summary>
        [Test]
        public void Open_WhileArbiterHoldsMenu_DoesNotReopen()
        {
            _windows.TryOpen(StageWindow.Menu);

            _presenter.Open();

            Assert.AreEqual(1, _windows.OpenCount);
            Assert.AreEqual(1, _view.ShowCount);
        }

        /// <summary>메뉴가 닫히면 조정자도 그 자리를 비워야 다음 창이 열린다.</summary>
        [Test]
        public void Close_ReleasesArbiterSlot()
        {
            _presenter.Open();

            _presenter.Close();

            Assert.IsFalse(_windows.IsOpen(StageWindow.Menu));
            Assert.IsTrue(_windows.TryOpen(StageWindow.Deck));
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
                () => new StageMenuPresenter(null, _pause, _restarter, _router, _windows));
            Assert.Throws<ArgumentNullException>(
                () => new StageMenuPresenter(_view, null, _restarter, _router, _windows));
            Assert.Throws<ArgumentNullException>(
                () => new StageMenuPresenter(_view, _pause, null, _router, _windows));
            Assert.Throws<ArgumentNullException>(
                () => new StageMenuPresenter(_view, _pause, _restarter, null, _windows));
            Assert.Throws<ArgumentNullException>(
                () => new StageMenuPresenter(_view, _pause, _restarter, _router, null));
        }

        private sealed class FakeStageMenuView : IStageMenuView
        {
            public int ShowCount { get; private set; }

            public int HideCount { get; private set; }

            public bool LastCanResume { get; private set; }

            public void ShowMenu(bool canResume)
            {
                ShowCount++;
                LastCanResume = canResume;
            }

            public void Hide()
            {
                HideCount++;
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
