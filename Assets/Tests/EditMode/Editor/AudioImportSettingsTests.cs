using NUnit.Framework;
using SushiDefense.EditorTools.Import;
using UnityEditor;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Editor
{
    public sealed class AudioImportSettingsTests
    {
        private const string MusicPath = "Assets/Audio/Music/stage-bgm.wav";
        private const string SoundPath = "Assets/Audio/Sound/sfx-sushi-eaten.wav";

        [Test]
        public void AppliesTo_MusicPath_ReturnsTrue()
        {
            Assert.IsTrue(AudioImportSettings.AppliesTo(MusicPath));
        }

        [Test]
        public void AppliesTo_SoundPath_ReturnsTrue()
        {
            Assert.IsTrue(AudioImportSettings.AppliesTo(SoundPath));
        }

        [Test]
        public void AppliesTo_ArtPath_ReturnsFalse()
        {
            Assert.IsFalse(AudioImportSettings.AppliesTo("Assets/Art/Sprites/Sushi/sushi-tuna.png"));
        }

        [Test]
        public void AppliesTo_SimilarlyNamedFolder_ReturnsFalse()
        {
            Assert.IsFalse(AudioImportSettings.AppliesTo("Assets/Audio/MusicOld/x.wav"));
        }

        [Test]
        public void AppliesTo_RootAppearingLaterInPath_ReturnsFalse()
        {
            // Contains 로 짠 구현을 배제한다. step-03 에서 "비슷한 이름의 폴더" 반례만으로는
            // 이 구현을 못 잡는다는 것을 실측으로 배웠다 — 루트가 경로 중간에 통째로 박혀야 한다.
            Assert.IsFalse(AudioImportSettings.AppliesTo(
                "Packages/com.vendor.kit/Assets/Audio/Music/theme.wav"));
        }

        [Test]
        public void AppliesTo_EmptyPath_ReturnsFalse()
        {
            Assert.IsFalse(AudioImportSettings.AppliesTo(string.Empty));
        }

        [Test]
        public void PlanFor_Music_IsManaged()
        {
            Assert.IsTrue(AudioImportSettings.PlanFor(MusicPath).IsManaged);
        }

        [Test]
        public void PlanFor_NonAudio_IsNotManaged()
        {
            Assert.IsFalse(AudioImportSettings.PlanFor("Assets/Art/Sprites/UI/icon-coin.png").IsManaged);
        }

        [Test]
        public void PlanFor_Music_StaysCompressedInMemory()
        {
            // 배경음을 통째로 풀면 32초치가 메모리에 남는다. 배포 타깃(WebGL)에서 아깝다.
            Assert.AreEqual(AudioClipLoadType.CompressedInMemory,
                            AudioImportSettings.PlanFor(MusicPath).LoadType);
        }

        [Test]
        public void PlanFor_Sound_DecompressesOnLoad()
        {
            // 효과음은 짧고 자주 난다. 재생할 때마다 푸는 비용이 더 크다.
            Assert.AreEqual(AudioClipLoadType.DecompressOnLoad,
                            AudioImportSettings.PlanFor(SoundPath).LoadType);
        }

        [Test]
        public void PlanFor_MusicAndSound_UseDifferentLoadTypes()
        {
            // 둘을 같은 값으로 두는 구현을 배제한다 — 위 두 테스트가 나란히 있어도
            // 상수를 돌려주는 구현은 하나만 통과시키므로, 차이 자체를 못박는다.
            Assert.AreNotEqual(AudioImportSettings.PlanFor(MusicPath).LoadType,
                               AudioImportSettings.PlanFor(SoundPath).LoadType);
        }

        [Test]
        public void PlanFor_Audio_UsesVorbis()
        {
            Assert.AreEqual(AudioCompressionFormat.Vorbis,
                            AudioImportSettings.PlanFor(SoundPath).CompressionFormat);
        }

        [Test]
        public void PlanFor_Audio_PreloadsAudioData()
        {
            // 첫 재생에서 끊기지 않게 한다.
            Assert.IsTrue(AudioImportSettings.PlanFor(SoundPath).PreloadAudioData);
        }
    }
}
