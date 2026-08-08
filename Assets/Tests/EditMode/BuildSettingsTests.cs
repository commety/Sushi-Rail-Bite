using NUnit.Framework;
using SushiDefense.Navigation;
using UnityEditor;

namespace SushiDefense.Tests.EditMode
{
    /// <summary>
    /// 씬이 Build Settings 에 등록됐는지 본다.
    ///
    /// <para>
    /// <b>M5 가 여기서 값비싸게 배웠다.</b> 등록을 빠뜨리면 빌드는 성공하고 엔진도 예외 없이
    /// 뜨는데 화면에는 카메라 클리어 색만 나온다 — 로그도 조용하다
    /// (<c>.claude/domain/presentation-and-audio.md</c> §7).
    /// </para>
    /// <para>
    /// <b><see cref="SceneNames"/> 의 오타를 잡는 유일한 지점이기도 하다.</b> 상수를
    /// <c>"Stage1"</c> 로 잘못 써도 컴파일은 통과하고 프레젠터 테스트도 전부 통과한다 —
    /// 실제 등록 경로와 대조해야만 드러난다.
    /// </para>
    /// </summary>
    public sealed class BuildSettingsTests
    {
        private static EditorBuildSettingsScene[] Scenes => EditorBuildSettings.scenes;

        [Test]
        public void BuildSettings_ContainsMainScene()
        {
            Assert.IsTrue(ContainsEnabled("Assets/Level/Scenes/Main.unity"),
                          "Main 이 등록되지 않았습니다 — 빌드는 성공하고 화면은 빕니다");
        }

        [Test]
        public void BuildSettings_ContainsStageScene()
        {
            Assert.IsTrue(ContainsEnabled("Assets/Level/Scenes/Stage01.unity"));
        }

        /// <summary>빌드를 열면 메인 화면이 떠야 한다 — 첫 번째 씬이 시작 씬이다.</summary>
        [Test]
        public void BuildSettings_MainIsFirst()
        {
            Assert.IsNotEmpty(Scenes);
            Assert.AreEqual("Assets/Level/Scenes/Main.unity", Scenes[0].path);
            Assert.IsTrue(Scenes[0].enabled);
        }

        /// <summary>
        /// <b>상수와 실제 경로를 대조한다.</b> <c>SceneManager.LoadScene</c> 은 이름만으로도
        /// 찾지만, 그 이름이 등록된 어느 경로와도 맞지 않으면 로드가 조용히 실패한다.
        /// </summary>
        [Test]
        public void BuildSettings_PathsMatchSceneNames()
        {
            Assert.IsTrue(ContainsSceneNamed(SceneNames.Main),
                          $"등록된 씬 중 '{SceneNames.Main}' 이라는 이름이 없습니다");
            Assert.IsTrue(ContainsSceneNamed(SceneNames.Stage),
                          $"등록된 씬 중 '{SceneNames.Stage}' 이라는 이름이 없습니다");
        }

        /// <summary>
        /// 처분을 정한 씬이 되살아나지 않았는지 본다 (step-12 §4 — 삭제 승인됨).
        /// </summary>
        [Test]
        public void BuildSettings_DoesNotContainSampleScene()
        {
            foreach (var scene in Scenes)
            {
                StringAssert.DoesNotContain("SampleScene", scene.path);
            }
        }

        private static bool ContainsEnabled(string path)
        {
            foreach (var scene in Scenes)
            {
                if (scene.enabled && scene.path == path)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSceneNamed(string name)
        {
            foreach (var scene in Scenes)
            {
                if (scene.enabled
                    && System.IO.Path.GetFileNameWithoutExtension(scene.path) == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
