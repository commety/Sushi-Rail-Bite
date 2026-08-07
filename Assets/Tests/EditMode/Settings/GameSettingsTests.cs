using NUnit.Framework;
using SushiDefense.Settings;

namespace SushiDefense.Tests.EditMode.Settings
{
    /// <summary>
    /// <see cref="GameSettings"/> 는 <b>무엇을 골랐나</b>만 답한다. 저장도 적용도 하지 않는다.
    ///
    /// <para>
    /// 셋을 합치면 "소리가 안 줄었다" 의 원인이 값인지 저장인지 적용인지 테스트에서
    /// 구분되지 않는다 — <c>SoundBudget</c> 과 <c>AudioUnlockGate</c> 를 합치지 않은 것과
    /// 같은 판단이다.
    /// </para>
    /// </summary>
    public sealed class GameSettingsTests
    {
        private const float Tolerance = 1e-6f;

        private GameSettings _settings;
        private int _changedCount;

        [SetUp]
        public void SetUp()
        {
            _settings = new GameSettings();
            _changedCount = 0;
            _settings.Changed += OnChanged;
        }

        [TearDown]
        public void TearDown()
        {
            _settings.Changed -= OnChanged;
        }

        /// <summary>
        /// 설정을 만진 적 없는 상태는 <b>다 들리는</b> 상태다. 기본값이 0 이면 소리가 안 나는
        /// 것을 버그로 신고하게 된다.
        /// </summary>
        [Test]
        public void Fresh_MasterVolumeIsFull()
        {
            Assert.AreEqual(1f, _settings.MasterVolume, Tolerance);
        }

        [Test]
        public void Fresh_IsNotFullscreen()
        {
            Assert.IsFalse(_settings.Fullscreen);
        }

        [Test]
        public void SetMasterVolume_AboveOne_ClampsToOne()
        {
            _settings.SetMasterVolume(1.5f);

            Assert.AreEqual(1f, _settings.MasterVolume, Tolerance);
        }

        [Test]
        public void SetMasterVolume_Negative_ClampsToZero()
        {
            _settings.SetMasterVolume(-0.5f);

            Assert.AreEqual(0f, _settings.MasterVolume, Tolerance);
        }

        [Test]
        public void SetMasterVolume_Changed_RaisesOnce()
        {
            _settings.SetMasterVolume(0.3f);

            Assert.AreEqual(0.3f, _settings.MasterVolume, Tolerance);
            Assert.AreEqual(1, _changedCount);
        }

        [Test]
        public void SetMasterVolume_SameValue_DoesNotRaise()
        {
            _settings.SetMasterVolume(0.3f);
            _changedCount = 0;

            _settings.SetMasterVolume(0.3f);

            Assert.AreEqual(0, _changedCount);
        }

        /// <summary>
        /// 클램프 <b>뒤</b>의 값으로 비교해야 한다. 원본으로 비교하면 슬라이더를 상한 밖에서
        /// 흔드는 동안 값이 그대로인데도 알림이 계속 나간다.
        /// </summary>
        [Test]
        public void SetMasterVolume_DifferentInputsClampingToSame_RaisesOnce()
        {
            _settings.SetMasterVolume(1.5f);
            _changedCount = 0;

            _settings.SetMasterVolume(2f);

            Assert.AreEqual(1f, _settings.MasterVolume, Tolerance);
            Assert.AreEqual(0, _changedCount);
        }

        /// <summary>
        /// 숫자가 아닌 값에는 자르기가 통하지 않는다 — 비교가 전부 <c>false</c> 라 그대로
        /// 들어가고 볼륨이 영영 복구되지 않는다. 저장소가 손상된 값을 돌려주면 실제로
        /// 도달하는 경로다.
        /// </summary>
        [Test]
        public void SetMasterVolume_NotANumber_IsIgnored()
        {
            _settings.SetMasterVolume(0.6f);
            _changedCount = 0;

            _settings.SetMasterVolume(float.NaN);

            Assert.AreEqual(0.6f, _settings.MasterVolume, Tolerance);
            Assert.AreEqual(0, _changedCount);
        }

        [Test]
        public void SetFullscreen_Toggled_RaisesOnce()
        {
            _settings.SetFullscreen(true);

            Assert.IsTrue(_settings.Fullscreen);
            Assert.AreEqual(1, _changedCount);
        }

        [Test]
        public void SetFullscreen_SameValue_DoesNotRaise()
        {
            _settings.SetFullscreen(true);
            _changedCount = 0;

            _settings.SetFullscreen(true);

            Assert.AreEqual(0, _changedCount);
        }

        /// <summary>
        /// 두 값은 서로를 건드리지 않는다. 하나의 알림을 쓰되 상태는 각자다.
        /// </summary>
        [Test]
        public void SetFullscreen_DoesNotTouchVolume()
        {
            _settings.SetMasterVolume(0.4f);

            _settings.SetFullscreen(true);

            Assert.AreEqual(0.4f, _settings.MasterVolume, Tolerance);
        }

        private void OnChanged()
        {
            _changedCount++;
        }
    }
}
