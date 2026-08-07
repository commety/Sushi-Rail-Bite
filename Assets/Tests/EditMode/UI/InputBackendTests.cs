using NUnit.Framework;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 이 프로젝트가 <b>어느 입력 백엔드 위에 서 있는지</b> 고정한다.
    ///
    /// <para>
    /// 설정이 Input System 전용이면 <c>UnityEngine.Input</c> 은 런타임에 예외를 던진다.
    /// 그런데 그 코드는 화면이 열려 있을 때만 실행되는 자리에 있어서 <b>PlayMode 테스트가
    /// 아예 밟지 않았고</b>, M5 에서 빌드 직전까지 아무도 못 잡았다 — Enter 로 스테이지를
    /// 넘길 수 없는 상태였다.
    /// </para>
    /// <para>
    /// 설정을 되돌리면 반대 방향으로 깨진다. 어느 쪽이든 <b>소리 없이</b> 깨지지 않도록
    /// 컴파일 시점에 못박는다.
    /// </para>
    /// </summary>
    public sealed class InputBackendTests
    {
        [Test]
        public void Project_UsesInputSystemPackage()
        {
#if !ENABLE_INPUT_SYSTEM
            Assert.Fail("Input System 이 꺼져 있다 — Presentation 의 Keyboard/Mouse 코드가 컴파일되지 않는다");
#endif
            Assert.Pass();
        }

        [Test]
        public void Project_DoesNotRelyOnLegacyInputManager()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Assert.Inconclusive("레거시 입력이 켜졌다 — UnityEngine.Input 을 다시 써도 되지만, "
                                + "지금 코드는 Input System 만 쓴다");
#endif
            Assert.Pass();
        }
    }
}
