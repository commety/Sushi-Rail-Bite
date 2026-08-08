using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
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
    /// 세 화면 사이를 <b>오가는 길</b>을 본다.
    ///
    /// <para>
    /// 프레젠터 단위 테스트는 각자 자기 화면만 안다 — 메인은 «열어 달라» 를 내보내고
    /// 패널은 «닫혔다» 를 내보낼 뿐, <b>둘을 이었을 때 메뉴가 돌아오는지</b>는 아무도 보지
    /// 않았다. 실제로 step-12 에서 씬을 조립하다 «패널을 닫으면 빈 화면에 갇힌다» 가 나왔고,
    /// 그때까지 모든 테스트가 초록이었다.
    /// </para>
    /// </summary>
    public sealed class MainBootstrapTests
    {
        private readonly List<Object> _garbage = new();

        private MainMenuView _menuView;
        private SettingsView _settingsView;
        private CodexView _codexView;
        private MainBootstrap _bootstrap;
        private GameObject _menuRoot;
        private Button _settingsButton;
        private Button _codexButton;
        private Button _settingsClose;
        private Button _codexClose;

        [SetUp]
        public void SetUp()
        {
            var root = NewObject("MainCanvas");
            root.AddComponent<RectTransform>();

            // ── 메인 메뉴 ──
            var menuGo = NewChild(root, "MainMenuView");
            _menuRoot = NewChild(menuGo, "Menu");
            NewChild(menuGo, "TitleLabel").AddComponent<TextMeshProUGUI>();
            var start = NewButton(_menuRoot, "StartButton");
            _settingsButton = NewButton(_menuRoot, "SettingsButton");
            _codexButton = NewButton(_menuRoot, "CodexButton");

            _menuView = menuGo.AddComponent<MainMenuView>();
            _menuView.Initialize(_menuRoot, null, start, _settingsButton, _codexButton);
            _menuView.Bind(new MainMenuPresenter(_menuView, new StubRouter()));

            // ── 설정 ──
            var settingsGo = NewChild(root, "SettingsView");
            var settingsPanel = NewChild(settingsGo, "Panel");
            var slider = NewChild(settingsPanel, "VolumeSlider").AddComponent<Slider>();
            var toggle = NewChild(settingsPanel, "FullscreenToggle").AddComponent<Toggle>();
            _settingsClose = NewButton(settingsPanel, "CloseButton");
            _settingsView = settingsGo.AddComponent<SettingsView>();
            _settingsView.Initialize(settingsPanel, null, slider, toggle, _settingsClose);

            // ── 백과사전 ──
            var codexGo = NewChild(root, "CodexView");
            var codexPanel = NewChild(codexGo, "Panel");
            _codexClose = NewButton(codexPanel, "CloseButton");
            _codexView = codexGo.AddComponent<CodexView>();
            _codexView.Initialize(codexPanel, System.Array.Empty<CardView>(), _codexClose);

            var catalog = ScriptableObject.CreateInstance<CardCatalog>();
            _garbage.Add(catalog);

            _bootstrap = root.AddComponent<MainBootstrap>();
            _bootstrap.Initialize(_menuView, _settingsView, _codexView, catalog);

            _menuView.Presenter.Open();
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
        public void ClickSettings_OpensSettingsAndHidesMenu()
        {
            _settingsButton.onClick.Invoke();

            Assert.IsTrue(_bootstrap.Settings.IsOpen);
            Assert.IsTrue(_settingsView.IsShowing);
            Assert.IsFalse(_menuRoot.activeSelf);
        }

        /// <summary>
        /// <b>이 테스트가 이 파일의 존재 이유다.</b> 돌아오는 길이 끊기면 플레이어가 빈
        /// 화면에 갇히는데, 프레젠터 단위 테스트는 그것을 볼 수 없다.
        /// </summary>
        [Test]
        public void CloseSettings_BringsTheMenuBack()
        {
            _settingsButton.onClick.Invoke();

            _settingsClose.onClick.Invoke();

            Assert.IsFalse(_bootstrap.Settings.IsOpen);
            Assert.IsFalse(_settingsView.IsShowing);
            Assert.IsTrue(_menuRoot.activeSelf, "설정을 닫았는데 메인 메뉴가 돌아오지 않았다");
            Assert.IsTrue(_menuView.Presenter.IsOpen);
        }

        [Test]
        public void ClickCodex_OpensCodexAndHidesMenu()
        {
            _codexButton.onClick.Invoke();

            Assert.IsTrue(_bootstrap.Codex.IsOpen);
            Assert.IsTrue(_codexView.IsShowing);
            Assert.IsFalse(_menuRoot.activeSelf);
        }

        [Test]
        public void CloseCodex_BringsTheMenuBack()
        {
            _codexButton.onClick.Invoke();

            _codexClose.onClick.Invoke();

            Assert.IsFalse(_bootstrap.Codex.IsOpen);
            Assert.IsTrue(_menuRoot.activeSelf, "사전을 닫았는데 메인 메뉴가 돌아오지 않았다");
        }

        /// <summary>설정 버튼이 사전을 열거나 그 반대가 되는 배선을 배제한다.</summary>
        [Test]
        public void ClickSettings_DoesNotOpenCodex()
        {
            _settingsButton.onClick.Invoke();

            Assert.IsFalse(_bootstrap.Codex.IsOpen);
        }

        [Test]
        public void ClickCodex_DoesNotOpenSettings()
        {
            _codexButton.onClick.Invoke();

            Assert.IsFalse(_bootstrap.Settings.IsOpen);
        }

        /// <summary>
        /// 열고 닫기를 반복해도 구독이 겹치지 않는지 본다. 겹치면 메뉴가 두 번 열리는데
        /// 재진입 가드에 막혀 <b>증상이 안 보인다</b> — 나중에 다른 구독자가 붙을 때 터진다.
        /// </summary>
        [Test]
        public void OpenAndCloseTwice_LeavesMenuOpenExactlyOnce()
        {
            _settingsButton.onClick.Invoke();
            _settingsClose.onClick.Invoke();
            _codexButton.onClick.Invoke();
            _codexClose.onClick.Invoke();

            Assert.IsTrue(_menuView.Presenter.IsOpen);
            Assert.IsTrue(_menuRoot.activeSelf);
            Assert.IsFalse(_bootstrap.Settings.IsOpen);
            Assert.IsFalse(_bootstrap.Codex.IsOpen);
        }

        /// <summary>
        /// <b>물려 주지 않는 경로.</b> 위 테스트들은 <c>Initialize</c> 로 참조를 넘기므로
        /// <c>Start</c> 의 자체 배선을 지나친다 (<c>tests.md</c> §1 «직접 주입으로 우회되는
        /// 폴백»).
        /// </summary>
        [UnityTest]
        public IEnumerator Start_WithInspectorReferences_WiresByItself()
        {
            var root = NewObject("StandaloneMain");
            root.AddComponent<RectTransform>();

            var menuGo = NewChild(root, "MainMenuView");
            var menuRoot = NewChild(menuGo, "Menu");
            var settingsButton = NewButton(menuRoot, "SettingsButton");
            var router = root.AddComponent<SceneRouter>();
            var menuView = menuGo.AddComponent<MainMenuView>();
            menuView.Initialize(menuRoot, null, null, settingsButton, null);
            SetRouter(menuView, router);

            var settingsGo = NewChild(root, "SettingsView");
            var settingsPanel = NewChild(settingsGo, "Panel");
            var settingsView = settingsGo.AddComponent<SettingsView>();
            settingsView.Initialize(settingsPanel, null, null, null,
                                    NewButton(settingsPanel, "CloseButton"));

            var bootstrap = root.AddComponent<MainBootstrap>();
            SetBootstrapFields(bootstrap, menuView, settingsView);

            yield return null;

            Assert.IsNotNull(bootstrap.Settings, "아무도 물려 주지 않으면 스스로 세운다");

            settingsButton.onClick.Invoke();

            Assert.IsTrue(bootstrap.Settings.IsOpen, "인스펙터 배선만으로 설정이 열려야 한다");
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

        private static void SetBootstrapFields(MainBootstrap bootstrap, MainMenuView menu,
                                               SettingsView settings)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(bootstrap);
            serialized.FindProperty("_menuView").objectReferenceValue = menu;
            serialized.FindProperty("_settingsView").objectReferenceValue = settings;
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

        /// <summary>씬을 실제로 로드하지 않는다 — 여기서 보는 것은 패널 사이의 길이다.</summary>
        private sealed class StubRouter : ISceneRouter
        {
            public void LoadMain()
            {
            }

            public void LoadStage()
            {
            }
        }
    }
}
