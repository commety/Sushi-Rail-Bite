using NUnit.Framework;
using SushiDefense.Navigation;

namespace SushiDefense.Tests.EditMode.Navigation
{
    /// <summary>
    /// 씬 이름은 오타가 컴파일에 잡히지 않는다. 상수로 묶어 <b>한 곳에서만</b> 틀릴 수 있게
    /// 했으므로, 여기서는 그 한 곳이 스스로 무너지지 않는지만 본다.
    ///
    /// <para>
    /// <b>실제 경로와의 대조는 여기서 못 한다.</b> 상수가 Build Settings 의 등록 경로와
    /// 맞는지는 씬 테스트의 몫이며(step-12), 그 대조가 없으면 <c>"Stage1"</c> 같은 오타가
    /// 컴파일·프레젠터 테스트를 모두 통과한다.
    /// </para>
    /// </summary>
    public sealed class SceneNamesTests
    {
        /// <summary>복붙으로 둘이 같아지면 「게임 시작」이 스테이지가 아니라 메인을 다시 연다.</summary>
        [Test]
        public void Names_AreDistinct()
        {
            Assert.AreNotEqual(SceneNames.Main, SceneNames.Stage);
        }

        [Test]
        public void Names_AreNotEmpty()
        {
            Assert.IsNotEmpty(SceneNames.Main);
            Assert.IsNotEmpty(SceneNames.Stage);
        }

        /// <summary>
        /// 경로가 아니라 <b>이름</b>이다. <c>SceneManager.LoadScene</c> 은 둘 다 받지만,
        /// 확장자나 폴더가 섞이면 Build Settings 대조 테스트가 헛돈다.
        /// </summary>
        [Test]
        public void Names_AreBareSceneNames()
        {
            StringAssert.DoesNotContain("/", SceneNames.Main);
            StringAssert.DoesNotContain(".unity", SceneNames.Main);
            StringAssert.DoesNotContain("/", SceneNames.Stage);
            StringAssert.DoesNotContain(".unity", SceneNames.Stage);
        }
    }
}
