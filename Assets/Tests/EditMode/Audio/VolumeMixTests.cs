using NUnit.Framework;
using SushiDefense.Audio;

namespace SushiDefense.Tests.EditMode.Audio
{
    public sealed class VolumeMixTests
    {
        [SetUp]
        public void SetUp()
        {
            // 전역이라 테스트끼리 샌다 (RULE-01 의 초기화가 여기서도 그물이다).
            VolumeMix.ResetOnLoad();
        }

        [TearDown]
        public void TearDown()
        {
            VolumeMix.ResetOnLoad();
        }

        [Test]
        public void Default_Untouched_IsFullVolume()
        {
            // 0 으로 시작하면 «소리가 안 난다» 를 버그로 신고하게 된다.
            Assert.AreEqual(1f, VolumeMix.Bgm);
            Assert.AreEqual(1f, VolumeMix.Sfx);
        }

        [Test]
        public void Bgm_InRange_IsKept()
        {
            VolumeMix.Bgm = 0.3f;

            Assert.AreEqual(0.3f, VolumeMix.Bgm, 1e-6f);
        }

        [Test]
        public void Bgm_AboveOne_IsClamped()
        {
            VolumeMix.Bgm = 4f;

            Assert.AreEqual(1f, VolumeMix.Bgm);
        }

        [Test]
        public void Bgm_Negative_IsClamped()
        {
            VolumeMix.Bgm = -2f;

            Assert.AreEqual(0f, VolumeMix.Bgm);
        }

        [Test]
        public void Bgm_NaN_KeepsTheOldValue()
        {
            // NaN 은 비교가 전부 false 라 자르기를 통과한다. 그대로 들어가면 볼륨이
            // 영영 복구되지 않는다 — 저장소가 손상된 값을 돌려주는 경로가 실제로 있다.
            VolumeMix.Bgm = 0.5f;

            VolumeMix.Bgm = float.NaN;

            Assert.AreEqual(0.5f, VolumeMix.Bgm, 1e-6f);
        }

        [Test]
        public void Sfx_Changed_DoesNotTouchBgm()
        {
            // 갈래를 나눈 이유 자체다. 하나로 이어져 있으면 배경음만 줄일 수 없다.
            VolumeMix.Bgm = 0.2f;

            VolumeMix.Sfx = 0.9f;

            Assert.AreEqual(0.2f, VolumeMix.Bgm, 1e-6f);
            Assert.AreEqual(0.9f, VolumeMix.Sfx, 1e-6f);
        }

        [Test]
        public void ResetOnLoad_AfterChanges_RestoresFullVolume()
        {
            VolumeMix.Bgm = 0f;
            VolumeMix.Sfx = 0f;

            VolumeMix.ResetOnLoad();

            Assert.AreEqual(1f, VolumeMix.Bgm);
            Assert.AreEqual(1f, VolumeMix.Sfx);
        }
    }
}
