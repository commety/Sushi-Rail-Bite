using System.Collections;
using NUnit.Framework;
using SushiDefense.Audio;
using SushiDefense.Navigation;
using SushiDefense.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SushiDefense.Tests.PlayMode
{
    /// <summary>
    /// 실제 <c>Main.unity</c> 를 재생해 배선을 확인한다.
    ///
    /// <para>
    /// <b>코드로 세운 하네스는 씬 사고를 못 잡는다</b> (<c>.claude/rules/tests.md</c> §1).
    /// 프레젠터·뷰 테스트는 구성을 코드로 만들고 <c>Initialize</c> 로 참조를 물려 주므로,
    /// 씬의 참조가 통째로 빠져도 전부 초록이다. 씬 애셋 자체를 보는 것은 이 파일뿐이다.
    /// </para>
    /// </summary>
    public sealed class MainSceneTests
    {
        private const string ScenePath = "Assets/Level/Scenes/Main.unity";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            var parameters = new UnityEngine.SceneManagement.LoadSceneParameters(
                UnityEngine.SceneManagement.LoadSceneMode.Single);
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ScenePath, parameters);

            // 두 프레임을 넘긴다 — Start 에서 프레젠터가 세워지므로 한 프레임으로는 이르다.
            yield return null;
            yield return null;
#else
            yield break;
#endif
        }

        /// <summary>없으면 버튼도 드래그도 배달되지 않는다.</summary>
        [Test]
        public void MainScene_HasEventSystem()
        {
            Assert.IsNotNull(Object.FindAnyObjectByType<EventSystem>(),
                             "EventSystem 이 없으면 클릭이 아예 배달되지 않는다");
        }

        /// <summary>
        /// <b>메인 화면이 이 문제의 현장이다.</b> 준비 화면(로딩 바)이 떠 있는 동안 버튼이
        /// 놓일 자리를 누르면, 게임이 뜨자마자 그 클릭이 그대로 배달되어 누른 적 없는 화면으로
        /// 넘어갔다 — 화면은 아직 안 그려졌는데 입력이 먼저 도착한다.
        /// </summary>
        [Test]
        public void MainScene_EventSystemIsArmedLate()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            Assert.IsNotNull(eventSystem, "전제: 씬에 EventSystem 이 있다");

            Assert.IsNotNull(eventSystem.GetComponent<UiInputArmer>(),
                             "로딩 중 클릭이 그대로 배달된다 — 준비 화면에서 누른 자리로 넘어간다");
        }

        /// <summary>
        /// 이 프로젝트는 <c>ENABLE_LEGACY_INPUT_MANAGER</c> 가 정의되어 있지 않아
        /// <c>StandaloneInputModule</c> 이 런타임에 죽는다. <b>에디터에서는 경고만 뜨고
        /// 넘어갈 수 있어</b> 빌드에서 클릭이 통째로 안 먹는 형태로 드러난다.
        /// </summary>
        [Test]
        public void MainScene_UsesInputSystemUIModule()
        {
            // 타입 이름으로 본다. Tests.PlayMode 가 Unity.InputSystem 을 참조하지 않고,
            // 테스트 하나 때문에 asmdef 를 바꾸는 것은 §7 승인 사항이다.
            var module = Object.FindAnyObjectByType<BaseInputModule>();

            Assert.IsNotNull(module, "입력 모듈이 없다");
            Assert.AreEqual("InputSystemUIInputModule", module.GetType().Name,
                            "레거시 입력 모듈이면 빌드에서 클릭이 통째로 죽는다");
        }

        [Test]
        public void MainScene_HasMainMenuViewWithPresenter()
        {
            var view = Object.FindAnyObjectByType<MainMenuView>();

            Assert.IsNotNull(view, "MainMenuView 가 없다");
            Assert.IsNotNull(view.Presenter, "프레젠터가 세워지지 않았다 — 라우터 참조를 확인하라");
        }

        [Test]
        public void MainScene_HasSceneRouter()
        {
            Assert.IsNotNull(Object.FindAnyObjectByType<SceneRouter>(),
                             "SceneRouter 가 없으면 게임 시작이 아무 일도 하지 않는다");
        }

        [Test]
        public void MainScene_MenuStartsOpen()
        {
            var view = Object.FindAnyObjectByType<MainMenuView>();

            Assert.IsTrue(view.IsShowing, "메인 화면이 열리지 않으면 게임에 입구가 없다");
        }

        /// <summary>두 패널이 처음부터 떠 있으면 메뉴를 가린다.</summary>
        [Test]
        public void MainScene_SettingsAndCodexStartHidden()
        {
            Assert.IsFalse(Object.FindAnyObjectByType<SettingsView>().IsShowing);
            Assert.IsFalse(Object.FindAnyObjectByType<CodexView>().IsShowing);
        }

        [Test]
        public void MainScene_BootstrapWiresBothPanels()
        {
            var bootstrap = Object.FindAnyObjectByType<MainBootstrap>();

            Assert.IsNotNull(bootstrap, "MainBootstrap 이 없으면 설정·사전이 열리지 않는다");
            Assert.IsNotNull(bootstrap.Settings);
            Assert.IsNotNull(bootstrap.Codex);
        }

        /// <summary>
        /// 사전 자리가 카탈로그(초밥 8 · 손님 3)를 덮어야 한다. 모자라면 뒤쪽 카드가 조용히
        /// 안 그려지고, <c>CodexView</c> 는 예외를 내지 않으므로 눈으로만 드러난다.
        /// </summary>
        [Test]
        public void MainScene_CodexHasEnoughCardSlots()
        {
            var bootstrap = Object.FindAnyObjectByType<MainBootstrap>();
            var codexView = Object.FindAnyObjectByType<CodexView>();

            bootstrap.Codex.Open();

            Assert.AreEqual(bootstrap.Codex.EntryCount, codexView.ShownCardCount,
                            "카드 자리가 카탈로그보다 적다");
        }

        /// <summary>
        /// <b>씬 조립에서 반복해 걸린 것.</b> 폰트가 비어 있으면 TMP 가 기본 폰트로 폴백하고,
        /// 에디터에서는 시스템 폰트가 메워 줘서 <b>빌드해야만 두부가 드러난다</b>.
        ///
        /// <para>
        /// <b>«비었나» 로는 모자란다.</b> M6 에서 <c>Card.prefab</c> 이 기본 <c>SDF</c> 폰트를
        /// 물고 있었는데, 비어 있지는 않아 이 검사를 그대로 통과했다 — 사전 카드 11장이
        /// 빌드에서 전부 두부가 되는 상태였다. 그래서 한글 커버리지까지 본다.
        /// </para>
        /// </summary>
        [Test]
        public void MainScene_EveryLabelHasAFont()
        {
            var labels = Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include,
                                                                 FindObjectsSortMode.None);

            Assert.IsNotEmpty(labels, "라벨을 하나도 찾지 못했다 — 씬이 비었는지 확인하라");
            foreach (var label in labels)
            {
                Assert.IsNotNull(label.font, $"{label.name} 의 폰트가 비었다 — 빌드에서 두부가 된다");
                Assert.IsTrue(label.font.HasCharacters("매출 클리어"),
                              $"{label.name} 의 폰트에 한글이 없다: {label.font.name}");
            }
        }

        /// <summary>
        /// 클릭음이 <b>버튼에만</b> 붙었는지 씬에서 확인한다. 사전 카드 11장이 함께 잡히면
        /// 카드를 누를 때도 메뉴 소리가 난다.
        /// </summary>
        [Test]
        public void MainScene_ClickSoundHooksButtonsOnly()
        {
            var clicks = Object.FindAnyObjectByType<UiClickSound>();
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                                                          FindObjectsSortMode.None);
            var cards = Object.FindObjectsByType<CardView>(FindObjectsInactive.Include,
                                                          FindObjectsSortMode.None);

            Assert.IsNotNull(clicks, "UiClickSound 가 없으면 버튼이 소리를 내지 않는다");
            Assert.AreEqual(buttons.Length - cards.Length, clicks.HookedCount,
                            "카드가 클릭음에 딸려 들어갔다");
        }

        [Test]
        public void MainScene_AudioDirectorHasBankAndSource()
        {
            var director = Object.FindAnyObjectByType<AudioDirector>();

            Assert.IsNotNull(director, "AudioDirector 가 없으면 클릭음이 나지 않는다");

            // 참조가 비어 있으면 재생 요청이 조용히 버려진다 — 눌러도 아무 일이 없다.
            director.PlayUiClick();
            Assert.AreEqual(1, director.PlayedCount,
                            "클릭음이 눌렸다 — _bank 또는 _sfxSource 가 비었는지 확인하라");
        }
    }
}
