using NUnit.Framework;
using SushiDefense.Audio;

namespace SushiDefense.Tests.EditMode.Audio
{
    public sealed class AudioUnlockGateTests
    {
        private AudioUnlockGate _gate;

        [SetUp]
        public void SetUp()
        {
            // 잠금은 페이지 단위(=`static`)라 테스트끼리 샌다. 각 테스트가 «방금 페이지를
            // 연» 상태에서 시작하도록 되돌린다.
            AudioUnlockGate.ResetOnLoad();
            _gate = new AudioUnlockGate();
        }

        [TearDown]
        public void TearDown()
        {
            AudioUnlockGate.ResetOnLoad();
        }

        [Test]
        public void IsUnlocked_FreshGate_IsFalse()
        {
            // 배포 타깃(WebGL)은 첫 사용자 입력 전까지 오디오가 잠겨 있다.
            Assert.IsFalse(_gate.IsUnlocked);
        }

        [Test]
        public void IsUnlocked_AfterUnlock_IsTrue()
        {
            _gate.Unlock();

            Assert.IsTrue(_gate.IsUnlocked);
        }

        [Test]
        public void TryConsumeUnlockMoment_BeforeUnlock_ReturnsFalse()
        {
            // 여기서 true 를 돌려주면 배경음이 잠긴 채로 시작해 영영 들리지 않는다.
            Assert.IsFalse(_gate.TryConsumeUnlockMoment());
        }

        [Test]
        public void TryConsumeUnlockMoment_AfterUnlock_ReturnsTrueThenFalse()
        {
            _gate.Unlock();

            Assert.IsTrue(_gate.TryConsumeUnlockMoment(), "열린 직후 한 번은 잡혀야 한다");
            Assert.IsFalse(_gate.TryConsumeUnlockMoment(), "그 다음부터는 잡히지 않아야 한다");
        }

        [Test]
        public void Unlock_Twice_DoesNotRearmMoment()
        {
            // 입력은 계속 들어온다. 두 번째 입력에서 순간이 되살아나면 배경음이 처음부터
            // 다시 재생된다.
            _gate.Unlock();
            _gate.TryConsumeUnlockMoment();

            _gate.Unlock();

            Assert.IsFalse(_gate.TryConsumeUnlockMoment());
        }

        /// <summary>
        /// 브라우저는 한 번 연 것을 다시 잠그지 않는다. 게이트마다 따로 들고 있었을 때는
        /// 씬이 바뀔 때마다 새 진행자가 <b>잠긴 채로 태어나</b>, 화면을 옮길 때마다 다시
        /// 클릭하기 전까지 음악이 없었다.
        /// </summary>
        [Test]
        public void IsUnlocked_NewGateAfterAnotherUnlocked_IsTrue()
        {
            _gate.Unlock();

            Assert.IsTrue(new AudioUnlockGate().IsUnlocked);
        }

        /// <summary>
        /// 새 게이트는 <b>자기 몫의 시작 기회</b>를 갖는다. 잠금만 이어받고 순간을 못
        /// 받으면, 다음 화면의 배경음이 영영 시작되지 않는다.
        /// </summary>
        [Test]
        public void TryConsumeUnlockMoment_NewGateOnAnUnlockedPage_ReturnsTrueOnce()
        {
            _gate.Unlock();
            _gate.TryConsumeUnlockMoment();

            var next = new AudioUnlockGate();

            Assert.IsTrue(next.TryConsumeUnlockMoment(), "새 화면이 자기 배경음을 못 켠다");
            Assert.IsFalse(next.TryConsumeUnlockMoment());
        }

        [Test]
        public void ResetOnLoad_AfterUnlock_LocksAgain()
        {
            // 재생을 다시 시작하면 페이지도 새로 열린 것이다 (RULE-01 — 도메인 리로드가
            // 꺼져 있어도 `static` 이 살아남는다).
            _gate.Unlock();

            AudioUnlockGate.ResetOnLoad();

            Assert.IsFalse(new AudioUnlockGate().IsUnlocked);
        }

        [Test]
        public void Unlock_TwiceBeforeConsuming_StillOnlyOneMoment()
        {
            _gate.Unlock();
            _gate.Unlock();

            Assert.IsTrue(_gate.TryConsumeUnlockMoment());
            Assert.IsFalse(_gate.TryConsumeUnlockMoment());
        }
    }
}
