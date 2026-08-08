using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Navigation;
using SushiDefense.Stages;
using SushiDefense.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.UI
{
    /// <summary>
    /// 메뉴 <b>화면</b>이 프레젠터의 답을 그대로 반영하는지 본다. 판정은
    /// <c>StageMenuPresenterTests</c> 가 EditMode 로 이미 검증하므로, 여기서 볼 것은
    /// «버튼이 실제로 켜지고 꺼지는가» 뿐이다.
    ///
    /// <para>
    /// <b>버튼을 눌러 확인한다.</b> 프레젠터를 직접 부르면 <c>onClick</c> 배선을 지나쳐,
    /// 버튼이 아무 데도 안 물려 있어도 초록이다 — M5·M6 에서 반복된 사각지대다
    /// (<c>.claude/rules/tests.md</c> §1 «직접 주입으로 우회되는 폴백»).
    /// </para>
    /// </summary>
    public sealed class StageMenuViewTests
    {
        private readonly List<GameObject> _objects = new();

        private StageMenuView _view;
        private StageMenuPresenter _presenter;
        private PauseState _pause;
        private FakeRestarter _restarter;
        private FakeRouter _router;
        private Button _open;
        private Button _resume;
        private Button _restart;
        private Button _quit;

        [SetUp]
        public void SetUp()
        {
            var host = NewObject("StageMenu");
            var panel = NewObject("Panel");
            panel.transform.SetParent(host.transform, false);

            var label = NewObject("StateLabel");
            label.transform.SetParent(panel.transform, false);
            var text = label.AddComponent<TextMeshProUGUI>();

            _open = NewButton("OpenButton", host.transform);
            _resume = NewButton("ResumeButton", panel.transform);
            _restart = NewButton("RestartButton", panel.transform);
            _quit = NewButton("QuitButton", panel.transform);

            _view = host.AddComponent<StageMenuView>();
            _view.Initialize(panel, text);
            _view.InitializeButtons(_open, null, _resume, _restart, _quit);

            _pause = new PauseState();
            _restarter = new FakeRestarter();
            _router = new FakeRouter();
            _presenter = new StageMenuPresenter(_view, _pause, _restarter, _router,
                                                new StageWindowArbiter());
            _view.Bind(_presenter);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                Object.DestroyImmediate(go);
            }

            _objects.Clear();
        }

        /// <summary>
        /// 메뉴 아이콘이 <b>토글</b>인지 버튼을 눌러 본다. 열기만 하면 아이콘으로 닫을 수
        /// 없는데, 그것이 리포트에 적힌 증상이다.
        /// </summary>
        [Test]
        public void OpenButton_Clicked_TogglesTheMenu()
        {
            _open.onClick.Invoke();
            Assert.IsTrue(_view.IsShowing, "아이콘이 메뉴에 물려 있지 않다");

            _open.onClick.Invoke();

            Assert.IsFalse(_view.IsShowing, "아이콘으로 메뉴를 닫을 수 없다");
        }

        /// <summary>재개는 <b>창을 닫는다.</b> 열어 둔 채 시간만 흐르는 상태를 만들지 않는다.</summary>
        [Test]
        public void ResumeButton_Clicked_ClosesTheMenu()
        {
            _open.onClick.Invoke();

            _resume.onClick.Invoke();

            Assert.IsFalse(_view.IsShowing);
            Assert.IsFalse(_pause.IsPaused);
        }

        [Test]
        public void Open_ShowsResumeAndPausedTitle()
        {
            _presenter.Open();

            Assert.IsTrue(_view.IsResumeShown);
            Assert.IsTrue(_resume.gameObject.activeSelf);
            Assert.AreEqual("일시정지", _view.StateText);
        }

        /// <summary>
        /// 실패로 열린 메뉴에는 <b>돌아갈 판이 없다</b> — 재개가 꺼져야 한다. 눌러도 아무
        /// 일이 없는 버튼을 남기면 «고장» 으로 읽힌다.
        /// </summary>
        [Test]
        public void OpenAfterFailure_HidesResumeAndSaysFailed()
        {
            _presenter.OpenAfterFailure();

            Assert.IsFalse(_view.IsResumeShown);
            Assert.IsFalse(_resume.gameObject.activeSelf);
            Assert.AreEqual("실패", _view.StateText);
        }

        /// <summary>
        /// 실패 창에서도 다시 시작·나가기는 눌린다 — 그 둘이 <b>유일한 출구</b>다.
        /// 재개만 확인하면 창 전체가 꺼진 구현도 통과한다.
        /// </summary>
        [Test]
        public void OpenAfterFailure_RestartAndQuitStillWork()
        {
            _presenter.OpenAfterFailure();

            Assert.IsTrue(_restart.gameObject.activeSelf);
            Assert.IsTrue(_quit.gameObject.activeSelf);

            _restart.onClick.Invoke();
            Assert.AreEqual(1, _restarter.RestartCount);

            _presenter.OpenAfterFailure();
            _quit.onClick.Invoke();
            Assert.AreEqual(1, _router.LoadMainCount);
        }

        /// <summary>
        /// <b>실패 창 위에서 메뉴 아이콘은 죽어 있다.</b> 눌러서 닫히면 실패한 판만 남고
        /// 다시 시작·나가기가 함께 사라진다 — 브라우저 실플레이에서 나온 증상이다.
        ///
        /// <para>
        /// 프레젠터가 아니라 <b>버튼을 눌러</b> 확인한다. 아이콘의 <c>onClick</c> 이 어디에
        /// 물려 있는지가 이 테스트의 대상이다.
        /// </para>
        /// </summary>
        [Test]
        public void OpenButton_ClickedAfterFailure_LeavesTheFailureWindowUp()
        {
            _presenter.OpenAfterFailure();

            _open.onClick.Invoke();

            Assert.IsTrue(_view.IsShowing, "실패 창이 닫혀 출구가 사라졌다");
            Assert.AreEqual("실패", _view.StateText, "«일시정지» 로 다시 열렸다");
            Assert.IsFalse(_view.IsResumeShown, "실패한 판에 재개가 돌아왔다");
        }

        /// <summary>
        /// 실패로 감춘 재개가 <b>다음에 멈췄을 때 돌아오는지</b> 본다. 한 번 끄고 다시
        /// 켜지 않으면, 재시작한 판에서는 영영 재개할 수 없다.
        /// </summary>
        [Test]
        public void Open_AfterFailure_BringsResumeBack()
        {
            _presenter.OpenAfterFailure();
            _presenter.Close();

            _presenter.Open();

            Assert.IsTrue(_view.IsResumeShown);
            Assert.IsTrue(_resume.gameObject.activeSelf);
        }

        private Button NewButton(string name, Transform parent)
        {
            var go = NewObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();
            return go.AddComponent<Button>();
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            _objects.Add(go);
            return go;
        }

        private sealed class FakeRestarter : IStageRestarter
        {
            public int RestartCount { get; private set; }

            public void Restart()
            {
                RestartCount++;
            }
        }

        private sealed class FakeRouter : ISceneRouter
        {
            public int LoadMainCount { get; private set; }

            public void LoadMain()
            {
                LoadMainCount++;
            }

            public void LoadStage()
            {
            }
        }
    }
}
