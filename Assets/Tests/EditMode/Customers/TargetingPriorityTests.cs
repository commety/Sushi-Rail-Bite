using NUnit.Framework;
using SushiDefense.Customers;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// <see cref="TargetingPriority"/> 는 배정에서만 쓰인다 — <b>자격 판정에는 등장하지 않는다</b>
    /// (<c>CLAUDE.md</c> §1.1-3a).
    ///
    /// <para>
    /// 계산 자체는 세 줄이라 검증할 것이 적다. 그럼에도 별도 타입인 이유는 가격을 읽는 지점을
    /// 셀 수 있게 만들기 위해서다 — 비교자 안에 산술을 인라인하면, 다음 사람이 같은 식을
    /// 자격 판정 쪽에 복사해 넣어도 grep 이 잡지 못한다.
    /// </para>
    /// </summary>
    public sealed class TargetingPriorityTests
    {
        [Test]
        public void Distance_ExactMatch_IsZero()
        {
            Assert.AreEqual(0, TargetingPriority.Distance(200, 200));
        }

        [Test]
        public void Distance_BelowTargeting_IsSymmetricToAbove()
        {
            // 거리는 대칭이다 — 타겟팅 200 인 손님에게 190 과 210 은 똑같이 가깝다.
            // 점수만 보면 손해지만, 모든 손님이 무조건 비싼 걸 먹으면 타겟팅도 배치도
            // 의미가 없어진다. 손님이 탐욕적이지 않은 선호를 갖는 것이 전략을 만든다.
            //
            // 누군가 "비싼 쪽을 선호하게" 비대칭 가중치를 넣으면 이 테스트가 먼저 깨진다.
            var above = TargetingPriority.Distance(210, 200);
            var below = TargetingPriority.Distance(190, 200);

            // 구체값을 함께 박는다. 같은지만 보면 둘 다 0 인 구현에서도 통과한다.
            Assert.AreEqual(10, above);
            Assert.AreEqual(above, below);
        }

        [Test]
        public void Distance_FarPrice_IsLargerThanNear()
        {
            var near = TargetingPriority.Distance(220, 200);
            var far = TargetingPriority.Distance(500, 200);

            Assert.That(far, Is.GreaterThan(near));
        }

        [Test]
        public void Distance_AboveTargeting_IsPositive()
        {
            // 부호가 아니라 크기다. 오름차순 정렬로 다루기 때문에 음수가 나오면
            // 비교자에서 "먼 쪽이 먼저" 로 뒤집힌다.
            Assert.AreEqual(50, TargetingPriority.Distance(250, 200));
        }

        [Test]
        public void Distance_BelowTargeting_IsPositive()
        {
            Assert.AreEqual(50, TargetingPriority.Distance(150, 200));
        }

        [Test]
        public void Distance_ZeroTargeting_UsesPriceAsDistance()
        {
            Assert.AreEqual(300, TargetingPriority.Distance(300, 0));
        }
    }
}
