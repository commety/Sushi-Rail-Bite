using NUnit.Framework;
using SushiDefense.UI;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 화면이 뜬 직후 잠깐 입력을 받지 않는 게이트.
    ///
    /// <para>
    /// 웹에서 Unity 준비 화면이 떠 있는 동안 버튼 자리를 누르면, 게임이 뜨자마자 그 클릭이
    /// 그대로 배달되어 누른 적 없는 화면으로 넘어갔다.
    /// </para>
    /// </summary>
    public sealed class InputArmGateTests
    {
        [Test]
        public void Fresh_IsNotArmed()
        {
            var gate = new InputArmGate(0.25f, 2);

            Assert.IsFalse(gate.IsArmed);
        }

        /// <summary>
        /// <b>이것이 실제 증상을 잡는 테스트다.</b> 로딩이 끝난 첫 프레임의
        /// <c>deltaTime</c> 은 로딩 시간만큼 부풀어 있어, 시간만 세는 구현은 <b>정작
        /// 막아야 할 그 한 프레임</b>에 이미 열려 버린다.
        /// </summary>
        [Test]
        public void Tick_HugeFirstDelta_StillClosedBecauseOfFrameCount()
        {
            var gate = new InputArmGate(0.25f, 2);

            gate.Tick(3f);

            Assert.IsFalse(gate.IsArmed, "첫 프레임의 부푼 deltaTime 하나로 열리면 안 된다");
        }

        /// <summary>
        /// 반대 방향. 프레임만 세는 구현을 배제한다 — 프레임률이 높은 기기에서 너무 빨리
        /// 열린다.
        /// </summary>
        [Test]
        public void Tick_ManyTinyFrames_StillClosedBecauseOfTime()
        {
            var gate = new InputArmGate(0.25f, 2);

            for (var i = 0; i < 10; i++)
            {
                gate.Tick(0.001f);
            }

            Assert.IsFalse(gate.IsArmed);
        }

        [Test]
        public void Tick_BothConditionsMet_Arms()
        {
            var gate = new InputArmGate(0.25f, 2);

            gate.Tick(0.2f);
            gate.Tick(0.2f);

            Assert.IsTrue(gate.IsArmed);
        }

        /// <summary>
        /// 한 번 열리면 다시 닫히지 않는다. 닫힐 수 있으면 판이 도는 중에 입력이
        /// 사라지는 구간이 생긴다.
        /// </summary>
        [Test]
        public void Tick_AfterArmed_StaysArmed()
        {
            var gate = new InputArmGate(0.25f, 2);
            gate.Tick(0.2f);
            gate.Tick(0.2f);

            gate.Tick(0f);
            gate.Tick(0f);

            Assert.IsTrue(gate.IsArmed);
        }

        /// <summary>지연을 끄면 처음부터 열려 있다 — 게이트를 없애는 설정이 필요할 때의 형태다.</summary>
        [Test]
        public void Fresh_ZeroDelay_IsArmed()
        {
            var gate = new InputArmGate(0f, 0);

            Assert.IsTrue(gate.IsArmed);
        }
    }
}
