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
            _gate = new AudioUnlockGate();
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
