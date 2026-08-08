using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Navigation;
using SushiDefense.UI;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.PlayMode.UI
{
    /// <summary>
    /// 화면은 <b>판정하지 않는다.</b> 버튼이 프레젠터까지 실제로 배달되는지만 본다 —
    /// 어디로 갈지·무엇을 열지는 EditMode 의 <c>MainMenuPresenterTests</c> 가 검증한다.
    ///
    /// <para>
    /// <c>EventSystem</c> 없이 <c>onClick.Invoke()</c> 로 부른다. <b>그것은 우회로이므로</b>,
    /// 씬에 <c>EventSystem</c> 이 실재하는지는 씬 테스트가 따로 본다 (step-12).
    /// </para>
    /// </summary>
    public sealed class MainMenuViewTests
    {
        private readonly List<Object> _garbage = new();

        private MainMenuView _view;
        private GameObject _menuRoot;
        private TMP_Text _title;
        private Button _start;
        private Button _settings;
        private Button _codex;
        private FakeSceneRouter _router;

        private int _settingsRequests;
        private int _codexRequests;

        [SetUp]
        public void SetUp()
        {
            var root = NewObject("MainCanvas");
            root.AddComponent<RectTransform>();

            _menuRoot = NewChild(root, "Menu");

            var titleGo = NewChild(root, "TitleLabel");
            _title = titleGo.AddComponent<TextMeshProUGUI>();

            _start = NewButton(_menuRoot, "StartButton");
            _settings = NewButton(_menuRoot, "SettingsButton");
            _codex = NewButton(_menuRoot, "CodexButton");

            _view = root.AddComponent<MainMenuView>();
            _view.Initialize(_menuRoot, _title, _start, _settings, _codex);

            _router = new FakeSceneRouter();
            _settingsRequests = 0;
            _codexRequests = 0;

            var presenter = new MainMenuPresenter(_view, _router);
            presenter.SettingsRequested += () => _settingsRequests++;
            presenter.CodexRequested += () => _codexRequests++;

            _view.Bind(presenter);
            presenter.Open();
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

        /// <summary><b>이 단계의 핵심 테스트다.</b> 버튼이 프레젠터까지 배달되는가.</summary>
        [Test]
        public void ClickStart_LoadsStage()
        {
            _start.onClick.Invoke();

            Assert.AreEqual(1, _router.LoadStageCount);
        }

        /// <summary>버튼 셋을 같은 핸들러에 물린 배선을 배제한다.</summary>
        [Test]
        public void ClickStart_DoesNotOpenPanels()
        {
            _start.onClick.Invoke();

            Assert.AreEqual(0, _settingsRequests);
            Assert.AreEqual(0, _codexRequests);
        }

        [Test]
        public void ClickSettings_RequestsSettingsAndHidesMenu()
        {
            _settings.onClick.Invoke();

            Assert.AreEqual(1, _settingsRequests);
            Assert.AreEqual(0, _codexRequests);
            Assert.IsFalse(_menuRoot.activeSelf, "패널 뒤에 남은 버튼은 보이지 않으면서 눌린다");
        }

        [Test]
        public void ClickCodex_RequestsCodexWithoutChangingScene()
        {
            _codex.onClick.Invoke();

            Assert.AreEqual(1, _codexRequests);
            Assert.AreEqual(0, _settingsRequests);
            Assert.AreEqual(0, _router.LoadStageCount);
            Assert.AreEqual(0, _router.LoadMainCount);
        }

        [Test]
        public void ShowMenu_WritesTitleAndEnablesMenu()
        {
            _view.Hide();

            _view.ShowMenu();

            Assert.IsTrue(_view.IsShowing);
            Assert.IsTrue(_menuRoot.activeSelf);
            StringAssert.Contains("스시", _title.text);
        }

        [Test]
        public void Hide_AfterShow_DisablesMenu()
        {
            _view.Hide();

            Assert.IsFalse(_view.IsShowing);
            Assert.IsFalse(_menuRoot.activeSelf);
        }

        /// <summary>
        /// 파괴된 뒤에도 구독이 남아 있으면 다음 화면의 클릭이 죽은 프레젠터로 간다
        /// (<c>.claude/rules/scripts.md</c> §6).
        /// </summary>
        [Test]
        public void ClickStart_AfterDestroy_DoesNothing()
        {
            Object.DestroyImmediate(_view);

            _start.onClick.Invoke();

            Assert.AreEqual(0, _router.LoadStageCount);
        }

        /// <summary>
        /// <b>물리지 않은 채로 뜨는 경로.</b> 위 테스트들은 프레젠터를 직접 물려 주므로
        /// 이 폴백을 지나친다 — 이것이 죽으면 메인 씬을 열어도 아무것도 안 보인다
        /// (<c>.claude/rules/tests.md</c> §1 «직접 주입으로 우회되는 폴백»).
        /// </summary>
        [UnityTest]
        public IEnumerator Start_WithoutBind_OpensMenuByItself()
        {
            var root = NewObject("StandaloneMenu");
            root.AddComponent<RectTransform>();
            var menu = NewChild(root, "Menu");
            var router = root.AddComponent<SceneRouter>();

            var view = root.AddComponent<MainMenuView>();
            SetRouter(view, router);
            view.Initialize(menu, null, null, null, null);

            // 내려 둔 채로 프레임을 넘긴다 — 처음부터 켜져 있으면 열지 않는 구현도 통과한다.
            view.Hide();

            yield return null;

            Assert.IsNotNull(view.Presenter, "아무도 물려 주지 않으면 스스로 세운다");
            Assert.IsTrue(view.IsShowing, "메인 화면이 열리지 않으면 게임에 입구가 없다");
            Assert.IsTrue(menu.activeSelf);
        }

        // ── 조립 ───────────────────────────────────────────────────────────

        private static void SetRouter(MainMenuView view, SceneRouter router)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(view);
            serialized.FindProperty("_router").objectReferenceValue = router;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        private static GameObject NewChild(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static Button NewButton(GameObject parent, string name)
        {
            var go = NewChild(parent, name);
            go.AddComponent<Image>();
            return go.AddComponent<Button>();
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _garbage.Add(go);
            return go;
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
