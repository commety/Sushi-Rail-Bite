using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Data
{
    public sealed class AudioCueTests
    {
        private const float Tolerance = 1e-6f;

        private AudioBankSO _bank;
        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            _bank = ScriptableObject.CreateInstance<AudioBankSO>();
            _clip = AudioClip.Create("cue-test", 1, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_bank);
            Object.DestroyImmediate(_clip);
        }

        [Test]
        public void Volume_FreshCue_IsFullyAudible()
        {
            // 기본값이 0 이면 클립을 물려도 소리가 나지 않는다. 증상은 "재생이 안 된다" 로
            // 보이지만 원인은 볼륨이라, 애셋을 열어 보기 전까지 찾지 못한다.
            Assert.AreEqual(1f, new AudioCue().Volume, Tolerance);
        }

        [Test]
        public void CooldownSeconds_FreshCue_IsZero()
        {
            // 겹침 제어는 기본적으로 꺼져 있다. 켜는 것은 밸런스 판단이며 애셋에서 한다.
            Assert.AreEqual(0f, new AudioCue().CooldownSeconds, Tolerance);
        }

        [Test]
        public void HasClip_NoClip_ReturnsFalse()
        {
            Assert.IsFalse(new AudioCue().HasClip);
        }

        [Test]
        public void HasClip_ClipAssigned_ReturnsTrue()
        {
            SerializedFieldSetter.SetObject(_bank, "_sushiEaten._clip", _clip);

            Assert.IsTrue(_bank.SushiEaten.HasClip);
        }

        [Test]
        public void Clamp_NegativeVolume_ClampsToZero()
        {
            SerializedFieldSetter.SetFloat(_bank, "_sushiEaten._volume", -1f);

            _bank.OnValidate();

            Assert.AreEqual(0f, _bank.SushiEaten.Volume, Tolerance);
        }

        [Test]
        public void Clamp_VolumeAboveOne_ClampsToOne()
        {
            SerializedFieldSetter.SetFloat(_bank, "_sushiEaten._volume", 4f);

            _bank.OnValidate();

            Assert.AreEqual(1f, _bank.SushiEaten.Volume, Tolerance);
        }

        [Test]
        public void Clamp_NegativeCooldown_ClampsToZero()
        {
            SerializedFieldSetter.SetFloat(_bank, "_sushiEaten._cooldownSeconds", -0.5f);

            _bank.OnValidate();

            Assert.AreEqual(0f, _bank.SushiEaten.CooldownSeconds, Tolerance);
        }

        [Test]
        public void Clamp_ValuesInRange_LeavesThemAlone()
        {
            // 상수를 돌려주는 클램프에서도 위 세 테스트는 통과한다. 정상값이 살아남는지를
            // 같은 파일에 박아 그 구현을 배제한다 (.claude/rules/tests.md §3).
            SerializedFieldSetter.SetFloat(_bank, "_sushiEaten._volume", 0.5f);
            SerializedFieldSetter.SetFloat(_bank, "_sushiEaten._cooldownSeconds", 0.06f);

            _bank.OnValidate();

            Assert.AreEqual(0.5f, _bank.SushiEaten.Volume, Tolerance);
            Assert.AreEqual(0.06f, _bank.SushiEaten.CooldownSeconds, Tolerance);
        }
    }
}
