using NUnit.Framework;
using SushiDefense.Data;
using UnityEditor;

namespace SushiDefense.Tests.EditMode.Data
{
    /// <summary>
    /// 디스크의 <c>AudioBank.asset</c> 이 <b>제대로 물려 있는지</b>만 본다.
    ///
    /// <para>
    /// 밸런스 애셋을 로드하는 테스트는 원칙적으로 피하지만(<c>.claude/rules/tests.md</c> §4),
    /// 그 금지는 <b>수치</b>에 대한 것이다. 여기서 보는 것은 유효성 — 클립이 비어 있으면
    /// 소리가 안 나고, 그 증상은 볼륨·믹서·재생 코드를 한참 뒤진 뒤에야 애셋으로 돌아온다.
    /// §4 가 "별도의 데이터 검증 테스트로 분리한다" 고 적은 경우가 이것이다.
    /// </para>
    /// <para>
    /// <b>수치는 단언하지 않는다.</b> 간격·볼륨은 M9 에서 다시 만질 값이고, 여기서 박으면
    /// 밸런싱할 때마다 테스트가 깨진다.
    /// </para>
    /// </summary>
    public sealed class AudioBankAssetTests
    {
        private const string AssetPath = "Assets/Level/Balance/AudioBank.asset";

        private AudioBankSO _bank;

        [SetUp]
        public void SetUp()
        {
            _bank = AssetDatabase.LoadAssetAtPath<AudioBankSO>(AssetPath);
        }

        [Test]
        public void Asset_Exists()
        {
            // 스크립트 참조가 깨져 있으면 로드가 null 로 떨어진다 — 손으로 쓴 YAML 이
            // 실제로 역직렬화됐는지를 이 한 줄이 지킨다.
            Assert.IsNotNull(_bank, $"{AssetPath} 를 로드하지 못했다");
        }

        /// <summary>
        /// <b><c>UiClick</c> 은 아직 이 목록에 없다.</b> 클립을 M6 step-11 에서 만들기 때문이며,
        /// 그 단계가 클립을 물리면서 이 테스트와 <see cref="EveryCue_UsesItsOwnClip"/> 의
        /// 개수를 함께 늘린다. 그때까지 이 파일은 <b>큐 전부가 아니라 일곱</b>을 덮는다.
        /// </summary>
        [Test]
        public void EveryCue_HasClip()
        {
            Assert.IsTrue(_bank.SushiEaten.HasClip, nameof(_bank.SushiEaten));
            Assert.IsTrue(_bank.CustomerPlaced.HasClip, nameof(_bank.CustomerPlaced));
            Assert.IsTrue(_bank.RewardPicked.HasClip, nameof(_bank.RewardPicked));
            Assert.IsTrue(_bank.StageAdvanced.HasClip, nameof(_bank.StageAdvanced));
            Assert.IsTrue(_bank.StageCleared.HasClip, nameof(_bank.StageCleared));
            Assert.IsTrue(_bank.StageFailed.HasClip, nameof(_bank.StageFailed));
            Assert.IsTrue(_bank.Bgm.HasClip, nameof(_bank.Bgm));
        }

        [Test]
        public void EveryCue_UsesItsOwnClip()
        {
            // 같은 클립을 두 큐에 물리는 실수는 소리가 나기 때문에 눈치채기 어렵다.
            var clips = new System.Collections.Generic.HashSet<UnityEngine.AudioClip>
            {
                _bank.SushiEaten.Clip, _bank.CustomerPlaced.Clip, _bank.RewardPicked.Clip,
                _bank.StageAdvanced.Clip, _bank.StageCleared.Clip, _bank.StageFailed.Clip,
                _bank.Bgm.Clip
            };

            Assert.AreEqual(7, clips.Count, "큐 일곱이 서로 다른 클립을 써야 한다");
        }

        [Test]
        public void SushiEaten_IsTheQuietestCue()
        {
            // 가장 자주 나는 소리다. 여기가 다른 큐보다 크면 귀가 먼저 지친다.
            // 구체값이 아니라 관계를 박아 M9 의 밸런싱을 막지 않는다.
            Assert.Less(_bank.SushiEaten.Volume, _bank.StageCleared.Volume);
            Assert.Less(_bank.SushiEaten.Volume, _bank.CustomerPlaced.Volume);
        }

        [Test]
        public void SushiEaten_IsTheOnlyCueWithCooldown()
        {
            // 겹침 제어가 필요한 것은 연달아 터지는 큐뿐이다. 전부에 간격을 걸면
            // 한 번뿐인 결과음까지 버려질 수 있다.
            Assert.Greater(_bank.SushiEaten.CooldownSeconds, 0f);
            Assert.AreEqual(0f, _bank.StageCleared.CooldownSeconds, 1e-6f);
            Assert.AreEqual(0f, _bank.Bgm.CooldownSeconds, 1e-6f);
        }

        [Test]
        public void MaxConcurrentSfx_LeavesRoomForEveryTable()
        {
            // 자리가 넷이므로 손님 넷이 동시에 먹는 순간이 실제로 온다.
            Assert.GreaterOrEqual(_bank.MaxConcurrentSfx, 4);
        }
    }
}
