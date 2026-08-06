using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Data
{
    public sealed class AudioBankTests
    {
        private const float Tolerance = 1e-6f;

        /// <summary>
        /// 큐 필드의 직렬화 경로. 한 큐만 조이는 클램프를 배제하려면 전부를 같은 테스트에서
        /// 흔들어야 한다.
        /// </summary>
        private static readonly string[] CueFieldPaths =
        {
            "_sushiEaten", "_customerPlaced", "_rewardPicked",
            "_stageAdvanced", "_stageCleared", "_stageFailed", "_bgm"
        };

        private AudioBankSO _bank;

        [SetUp]
        public void SetUp()
        {
            _bank = ScriptableObject.CreateInstance<AudioBankSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_bank);
        }

        [Test]
        public void MaxConcurrentSfx_FreshBank_IsOne()
        {
            // 구조적 하한에서 시작한다. 실제 상한은 밸런스라 애셋에서 정해진다 —
            // 0 이면 소리가 하나도 나지 않으므로 그 상태로 태어나지 않게만 막는다.
            Assert.AreEqual(1, _bank.MaxConcurrentSfx);
        }

        [Test]
        public void OnValidate_ZeroMaxConcurrent_ClampsToOne()
        {
            SerializedFieldSetter.SetInt(_bank, "_maxConcurrentSfx", 0);

            _bank.OnValidate();

            Assert.AreEqual(1, _bank.MaxConcurrentSfx);
        }

        [Test]
        public void OnValidate_MaxConcurrentAboveOne_Kept()
        {
            SerializedFieldSetter.SetInt(_bank, "_maxConcurrentSfx", 6);

            _bank.OnValidate();

            Assert.AreEqual(6, _bank.MaxConcurrentSfx);
        }

        [Test]
        public void Cues_FreshBank_AreNotNull()
        {
            // CreateInstance 는 역직렬화를 거치지 않아 이니셜라이저가 없으면 실제로 null 이다.
            // 여기서 막지 않으면 재생 쪽이 NullReferenceException 으로 죽는다.
            Assert.IsNotNull(_bank.SushiEaten);
            Assert.IsNotNull(_bank.CustomerPlaced);
            Assert.IsNotNull(_bank.RewardPicked);
            Assert.IsNotNull(_bank.StageAdvanced);
            Assert.IsNotNull(_bank.StageCleared);
            Assert.IsNotNull(_bank.StageFailed);
            Assert.IsNotNull(_bank.Bgm);
        }

        [Test]
        public void Cues_FreshBank_HaveNoClip()
        {
            // 뱅크를 막 만들었는데 소리가 나면 안 된다.
            Assert.IsFalse(_bank.SushiEaten.HasClip);
            Assert.IsFalse(_bank.Bgm.HasClip);
        }

        [Test]
        public void OnValidate_NegativeVolumeInEveryCue_ClampsAll()
        {
            // 큐 하나만 조이는 구현을 배제한다. 한 큐만 흔들면 그 구현도 통과한다.
            foreach (var path in CueFieldPaths)
            {
                SerializedFieldSetter.SetFloat(_bank, $"{path}._volume", -1f);
            }

            _bank.OnValidate();

            Assert.AreEqual(0f, _bank.SushiEaten.Volume, Tolerance, nameof(_bank.SushiEaten));
            Assert.AreEqual(0f, _bank.CustomerPlaced.Volume, Tolerance, nameof(_bank.CustomerPlaced));
            Assert.AreEqual(0f, _bank.RewardPicked.Volume, Tolerance, nameof(_bank.RewardPicked));
            Assert.AreEqual(0f, _bank.StageAdvanced.Volume, Tolerance, nameof(_bank.StageAdvanced));
            Assert.AreEqual(0f, _bank.StageCleared.Volume, Tolerance, nameof(_bank.StageCleared));
            Assert.AreEqual(0f, _bank.StageFailed.Volume, Tolerance, nameof(_bank.StageFailed));
            Assert.AreEqual(0f, _bank.Bgm.Volume, Tolerance, nameof(_bank.Bgm));
        }
    }
}
