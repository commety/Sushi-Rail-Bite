using NUnit.Framework;
using SushiDefense.Customers;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// 손님 상태 → 몸통 동작. <b>«집기» 가 «먹기» 를 얼마나 가리는가</b>가 이 클래스의 전부다.
    ///
    /// <para>
    /// 대기 클립은 먹기·소화 <b>둘 다</b>의 재료이고 갈리는 것은 배속뿐이므로, 배속 쪽은
    /// <c>CustomerMotionSpeedTests</c> 가 따로 본다 — 한 테스트가 «무엇을» 과 «얼마나 빠르게»
    /// 를 같이 보면 어느 쪽이 깨졌는지 알 수 없다.
    /// </para>
    /// </summary>
    public sealed class CustomerMotionSelectorTests
    {
        private const float PickingSeconds = 0.5f;

        private CustomerMotionSelector _selector;

        [SetUp]
        public void SetUp()
        {
            _selector = new CustomerMotionSelector();
        }

        [Test]
        public void Advance_IdleCustomer_ShowsNoMotion()
        {
            var motion = _selector.Advance(CustomerState.Idle, 0.1f);

            Assert.AreEqual(CustomerMotion.None, motion);
        }

        [Test]
        public void Advance_EatingCustomer_ShowsEating()
        {
            var motion = _selector.Advance(CustomerState.Eating, 0.1f);

            Assert.AreEqual(CustomerMotion.Eating, motion);
        }

        [Test]
        public void Advance_DigestingCustomer_ShowsDigesting()
        {
            var motion = _selector.Advance(CustomerState.Digesting, 0.1f);

            Assert.AreEqual(CustomerMotion.Digesting, motion);
        }

        [Test]
        public void Advance_JustPicked_ShowsPickingBeforeEating()
        {
            _selector.NotifyPicked(PickingSeconds);

            var motion = _selector.Advance(CustomerState.Eating, 0.1f);

            Assert.AreEqual(CustomerMotion.Picking, motion,
                            "배정이 확정된 직후에는 집는 동작이 먹기를 가려야 한다");
        }

        /// <summary>
        /// 집기는 <b>먹기의 첫머리</b>지 먹기 전체가 아니다.
        ///
        /// <para>
        /// 두 시점을 한 테스트에 함께 박는다. 앞만 보면 «항상 집기» 구현이, 뒤만 보면
        /// «항상 먹기» 구현이 통과한다 (<c>.claude/rules/tests.md</c> §3).
        /// </para>
        /// </summary>
        [Test]
        public void Advance_PickingElapsed_FallsBackToEating()
        {
            _selector.NotifyPicked(PickingSeconds);

            var during = _selector.Advance(CustomerState.Eating, PickingSeconds * 0.5f);
            var after = _selector.Advance(CustomerState.Eating, PickingSeconds);

            Assert.AreEqual(CustomerMotion.Picking, during);
            Assert.AreEqual(CustomerMotion.Eating, after);
        }

        /// <summary>
        /// 먹다 만 초밥이 끝점에서 반납되면 손님은 <c>Idle</c> 로 돌아간다
        /// (<c>ClaimCoordinator.OnSushiRemoved</c>). 그때 집기가 남아 있으면
        /// <b>아무것도 안 든 손님이 계속 집는 시늉을 한다.</b>
        /// </summary>
        [Test]
        public void Advance_PickedThenStoppedEating_DropsPicking()
        {
            _selector.NotifyPicked(PickingSeconds);

            var motion = _selector.Advance(CustomerState.Idle, 0.01f);

            Assert.AreEqual(CustomerMotion.None, motion);
        }

        /// <summary>
        /// 집기가 끊긴 뒤 다시 먹기 시작해도 <b>되살아나지 않는다.</b> 남은 시간을 그대로
        /// 들고 있으면 다음 초밥에서 집기가 두 번 나온다.
        /// </summary>
        [Test]
        public void Advance_EatingAgainAfterPickingDropped_DoesNotResumePicking()
        {
            _selector.NotifyPicked(PickingSeconds);
            _selector.Advance(CustomerState.Idle, 0.01f);

            var motion = _selector.Advance(CustomerState.Eating, 0.01f);

            Assert.AreEqual(CustomerMotion.Eating, motion);
        }

        /// <summary>
        /// 물릴 집기 클립이 없으면 길이가 <c>0</c> 으로 넘어온다. 그때는 집기를 건너뛰고
        /// 곧장 먹는 모습이어야 한다 — <c>0</c> 을 «무한» 으로 읽으면 손님이 영영 집는다.
        /// </summary>
        [Test]
        public void Advance_PickedWithNoClip_ShowsEatingImmediately()
        {
            _selector.NotifyPicked(0f);

            var motion = _selector.Advance(CustomerState.Eating, 0.01f);

            Assert.AreEqual(CustomerMotion.Eating, motion);
        }

        /// <summary>
        /// 자리를 갈아탈 때 앞 손님의 집기가 이어지면, 방금 앉은 손님이 아무것도 없는데
        /// 집는 시늉을 한다.
        /// </summary>
        [Test]
        public void Reset_AfterPicked_ClearsPicking()
        {
            _selector.NotifyPicked(PickingSeconds);

            _selector.Reset();

            Assert.AreEqual(CustomerMotion.None, _selector.Current,
                            "전제: Reset 직후에는 아무 동작도 아니다");
            Assert.AreEqual(CustomerMotion.Eating,
                            _selector.Advance(CustomerState.Eating, 0.01f));
        }

        [Test]
        public void Current_TracksTheLastAdvance()
        {
            _selector.Advance(CustomerState.Digesting, 0.1f);

            Assert.AreEqual(CustomerMotion.Digesting, _selector.Current);
        }
    }
}
