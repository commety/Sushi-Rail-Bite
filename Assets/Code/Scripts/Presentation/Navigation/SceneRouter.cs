using UnityEngine;
using UnityEngine.SceneManagement;

namespace SushiDefense.Navigation
{
    /// <summary>
    /// <see cref="ISceneRouter"/> 의 유일한 구현. <b>한 줄짜리 래퍼라 테스트하지 않는다</b> —
    /// 테스트할 것이 없다.
    ///
    /// <para>
    /// 씬 이름을 여기서 문자열로 쓰지 않는다. <see cref="SceneNames"/> 하나만 본다.
    /// </para>
    /// </summary>
    public sealed class SceneRouter : MonoBehaviour, ISceneRouter
    {
        /// <inheritdoc />
        public void LoadMain()
        {
            SceneManager.LoadScene(SceneNames.Main);
        }

        /// <inheritdoc />
        public void LoadStage()
        {
            SceneManager.LoadScene(SceneNames.Stage);
        }
    }
}
