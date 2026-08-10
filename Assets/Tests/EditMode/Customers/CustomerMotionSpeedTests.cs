using NUnit.Framework;
using SushiDefense.Customers;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// 대기 클립 한 장이 배속만으로 «먹기» 와 «소화» 로 갈리는지 본다.
    ///
    /// <para>
    /// 밸런스 애셋을 로드하지 않는다 — 수치는 여기 상수로 박아 두고, 실제 애셋의 값이
    /// 바뀌어도 이 테스트는 깨지지 않아야 한다 (<c>.claude/rules/tests.md</c> §4).
    /// </para>
    /// </summary>
    public sealed class CustomerMotionSpeedTests
    {
        /// <summary>네 칸짜리 시트를 초당 8프레임으로 돌린 길이. 파이프라인이 정한 값이다.</summary>
        private const float ClipSeconds = 0.5f;

        private const int ChewCycles = 3;
        private const int RestCycles = 1;

        /// <summary>
        /// 먹는 시간이 짧을수록 빠르게 씹는다.
        ///
        /// <para>
        /// <b>세 유형을 한 테스트에 함께 박는다.</b> 기본 손님(1.5초)만 보면 기대값이 정확히
        /// <see cref="CustomerMotionSpeed.Natural"/> 이라 <b>상수 1 을 돌려주는 구현도
        /// 통과한다</b> — 그 함정을 피하려면 1 이 아닌 값이 같은 테스트 안에 있어야 한다
        /// (<c>.claude/rules/tests.md</c> §3).
        /// </para>
        /// </summary>
        [Test]
        public void For_Eating_ShorterMealChewsFaster()
        {
            var standard = Eating(eatSeconds: 1.5f);
            var bigEater = Eating(eatSeconds: 1f);
            var smallEater = Eating(eatSeconds: 2f);

            Assert.AreEqual(1f, standard, 0.0001f);
            Assert.AreEqual(1.5f, bigEater, 0.0001f);
            Assert.AreEqual(0.75f, smallEater, 0.0001f);
            Assert.Greater(bigEater, smallEater, "먹보가 소식가보다 느리게 씹는다");
        }

        /// <summary>
        /// 클립이 먹는 시간 안에 <see cref="ChewCycles"/> 번 돈다는 것이 배속의 정의다.
        /// 되짚어 계산해 그 관계 자체를 고정한다.
        /// </summary>
        [Test]
        public void For_Eating_LoopsTheClipOncePerChewCycle()
        {
            const float eatSeconds = 1.2f;

            var loops = eatSeconds * Eating(eatSeconds) / ClipSeconds;

            Assert.AreEqual(ChewCycles, loops, 0.0001f);
        }

        /// <summary>
        /// 같은 클립을 쓰므로 <b>둘을 가르는 것은 배속뿐</b>이다. 소화가 먹기보다 느리지
        /// 않으면 화면에서 두 상태가 구분되지 않는다.
        /// </summary>
        [Test]
        public void For_Digesting_IsSlowerThanEating()
        {
            var eating = Eating(eatSeconds: 1.5f);
            var digesting = Digesting(digestSeconds: 3f);

            Assert.AreEqual(ClipSeconds / 3f, digesting, 0.0001f);
            Assert.Less(digesting, eating, "쉬는 모습이 먹는 모습보다 빠르다");
        }

        /// <summary>집는 동작은 손님이 얼마나 오래 먹는지와 무관하게 같은 속도로 일어난다.</summary>
        [Test]
        public void For_Picking_IsNaturalRegardlessOfBalance()
        {
            var fast = CustomerMotionSpeed.For(CustomerMotion.Picking, ClipSeconds,
                                               0.5f, 1f, ChewCycles, RestCycles);
            var slow = CustomerMotionSpeed.For(CustomerMotion.Picking, ClipSeconds,
                                               9f, 20f, ChewCycles, RestCycles);

            Assert.AreEqual(CustomerMotionSpeed.Natural, fast, 0.0001f);
            Assert.AreEqual(CustomerMotionSpeed.Natural, slow, 0.0001f);
        }

        /// <summary>
        /// 먹는 시간이 <c>0</c> 인 손님이 실제로 있다 (<c>Customer.Placeholder</c>).
        /// 그대로 나누면 배속이 무한대가 되어 <c>Animator</c> 가 조용히 멈춘다 — 화면에는
        /// 첫 프레임에 굳은 손님만 남고 예외는 나지 않는다.
        /// </summary>
        [Test]
        public void For_ZeroEatSeconds_FallsBackToNatural()
        {
            var speed = Eating(eatSeconds: 0f);

            Assert.AreEqual(CustomerMotionSpeed.Natural, speed, 0.0001f);
            Assert.IsFalse(float.IsInfinity(speed), "0 을 나눠 무한대가 나왔다");
        }

        [Test]
        public void For_ZeroDigestSeconds_FallsBackToNatural()
        {
            Assert.AreEqual(CustomerMotionSpeed.Natural, Digesting(digestSeconds: 0f), 0.0001f);
        }

        /// <summary>물릴 클립이 없으면 길이가 <c>0</c> 으로 넘어온다.</summary>
        [Test]
        public void For_NoClip_FallsBackToNatural()
        {
            var speed = CustomerMotionSpeed.For(CustomerMotion.Eating, clipSeconds: 0f,
                                                eatSeconds: 1.5f, digestSeconds: 3f,
                                                ChewCycles, RestCycles);

            Assert.AreEqual(CustomerMotionSpeed.Natural, speed, 0.0001f);
        }

        [Test]
        public void For_NoMotion_IsNatural()
        {
            var speed = CustomerMotionSpeed.For(CustomerMotion.None, ClipSeconds,
                                                1.5f, 3f, ChewCycles, RestCycles);

            Assert.AreEqual(CustomerMotionSpeed.Natural, speed, 0.0001f);
        }

        private static float Eating(float eatSeconds) =>
            CustomerMotionSpeed.For(CustomerMotion.Eating, ClipSeconds,
                                    eatSeconds, digestSeconds: 3f, ChewCycles, RestCycles);

        private static float Digesting(float digestSeconds) =>
            CustomerMotionSpeed.For(CustomerMotion.Digesting, ClipSeconds,
                                    eatSeconds: 1.5f, digestSeconds, ChewCycles, RestCycles);
    }
}
