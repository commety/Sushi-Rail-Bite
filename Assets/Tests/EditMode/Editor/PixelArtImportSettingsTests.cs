using NUnit.Framework;
using SushiDefense.EditorTools.Import;
using UnityEditor;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Editor
{
    public sealed class PixelArtImportSettingsTests
    {
        private const string SpritePath = "Assets/Art/Sprites/Sushi/sushi-tuna.png";

        [Test]
        public void AppliesTo_ArtSpritesPath_ReturnsTrue()
        {
            Assert.IsTrue(PixelArtImportSettings.AppliesTo(SpritePath));
        }

        [Test]
        public void AppliesTo_ArtSpritesSubfolder_ReturnsTrue()
        {
            // UI 아이콘도 픽셀 아트다. 하위 폴더가 빠지면 거기만 조용히 흐려진다.
            Assert.IsTrue(PixelArtImportSettings.AppliesTo("Assets/Art/Sprites/UI/icon-coin.png"));
        }

        [Test]
        public void AppliesTo_PlaceholderPath_ReturnsTrue()
        {
            Assert.IsTrue(PixelArtImportSettings.AppliesTo("Assets/Level/Placeholder/SushiBlock.png"));
        }

        [Test]
        public void AppliesTo_SimilarlyNamedFolder_ReturnsFalse()
        {
            Assert.IsFalse(PixelArtImportSettings.AppliesTo("Assets/Level/Fan Art/Sprites/x.png"));
        }

        [Test]
        public void AppliesTo_RootAppearingLaterInPath_ReturnsFalse()
        {
            // Contains 로 짠 구현을 배제한다. 패키지 안의 애셋은 Packages/... 로 시작하므로
            // 프로젝트 경로가 통째로 뒤에 박혀 있어도 우리 규칙의 대상이 아니다.
            //
            // 위의 비슷한 이름 테스트만으로는 이 구현을 못 잡는다 — 루트에 Assets/ 접두가
            // 있어서 애초에 매칭되지 않기 때문이다. 실제로 주입해 보고 알았다.
            Assert.IsFalse(PixelArtImportSettings.AppliesTo(
                "Packages/com.vendor.kit/Assets/Art/Sprites/icon.png"));
        }

        [Test]
        public void AppliesTo_SiblingFolderWithSamePrefix_ReturnsFalse()
        {
            // 끝 슬래시가 빠진 StartsWith 구현을 배제한다.
            Assert.IsFalse(PixelArtImportSettings.AppliesTo("Assets/Art/SpritesOld/x.png"));
        }

        [Test]
        public void AppliesTo_FontsPath_ReturnsFalse()
        {
            // 폰트 아틀라스가 이 규칙에 걸리면 TMP 폰트가 깨진다 (step-09).
            Assert.IsFalse(PixelArtImportSettings.AppliesTo("Assets/Art/Fonts/SushiRailBite-KR.asset"));
        }

        [Test]
        public void AppliesTo_AudioPath_ReturnsFalse()
        {
            Assert.IsFalse(PixelArtImportSettings.AppliesTo("Assets/Audio/Sound/sfx-sushi-eaten.wav"));
        }

        [Test]
        public void AppliesTo_EmptyPath_ReturnsFalse()
        {
            Assert.IsFalse(PixelArtImportSettings.AppliesTo(string.Empty));
        }

        [Test]
        public void PlanFor_PixelArt_IsPixelArt()
        {
            Assert.IsTrue(PixelArtImportSettings.PlanFor(SpritePath).IsPixelArt);
        }

        [Test]
        public void PlanFor_NonPixelArt_IsNotPixelArt()
        {
            Assert.IsFalse(PixelArtImportSettings.PlanFor("Assets/Audio/Sound/x.wav").IsPixelArt);
        }

        [Test]
        public void PlanFor_PixelArt_UsesPointFilter()
        {
            // 이 한 항목이 빠지면 픽셀 아트가 흐릿하게 뭉개진다. 증상이 "약간 이상함" 이라
            // 발견도 늦다.
            Assert.AreEqual(FilterMode.Point, PixelArtImportSettings.PlanFor(SpritePath).FilterMode);
        }

        [Test]
        public void PlanFor_PixelArt_DisablesMipmaps()
        {
            Assert.IsFalse(PixelArtImportSettings.PlanFor(SpritePath).MipmapEnabled);
        }

        [Test]
        public void PlanFor_PixelArt_UsesProjectPixelsPerUnit()
        {
            // 구체값을 박는다. 값이 갈리면 같은 초밥이 자리마다 다른 크기로 보인다.
            Assert.AreEqual(32, PixelArtImportSettings.PlanFor(SpritePath).PixelsPerUnit);
        }

        [Test]
        public void PlanFor_PixelArt_KeepsColorsUncompressed()
        {
            // 블록 압축은 픽셀 경계에서 색을 뭉갠다. 용량은 step-11 의 WebGL 실측 뒤에 다시 본다.
            Assert.AreEqual(TextureImporterCompression.Uncompressed,
                            PixelArtImportSettings.PlanFor(SpritePath).Compression);
        }

        [Test]
        public void PlanFor_PixelArt_KeepsAlphaTransparency()
        {
            Assert.IsTrue(PixelArtImportSettings.PlanFor(SpritePath).AlphaIsTransparency);
        }

        [Test]
        public void PlanFor_PixelArt_UsesFullRectMesh()
        {
            Assert.AreEqual(SpriteMeshType.FullRect,
                            PixelArtImportSettings.PlanFor(SpritePath).MeshType);
        }

        [Test]
        public void PlanFor_PixelArt_ImportsAsSprite()
        {
            Assert.AreEqual(TextureImporterType.Sprite,
                            PixelArtImportSettings.PlanFor(SpritePath).TextureType);
        }

        [Test]
        public void AppliesTo_AnimationSheetPath_ReturnsTrue()
        {
            // 애니메이션 시트도 픽셀 아트다. 이 줄이 빠져 있던 동안 시트만 기본 설정으로
            // 들어와 흐릿했고, 화면에 안 나오는 애셋이라 아무도 눈치채지 못했다.
            Assert.IsTrue(PixelArtImportSettings.AppliesTo(
                "Assets/Art/Animations/customer-standard-idle.png"));
        }

        [Test]
        public void IsSheet_AnimationFolder_ReturnsTrue()
        {
            Assert.IsTrue(PixelArtImportSettings.IsSheet(
                "Assets/Art/Animations/customer-standard-idle.png"));
        }

        [Test]
        public void IsSheet_TilesetByName_ReturnsTrue()
        {
            // 타일 세트는 낱장 타일과 같은 폴더에 산다 — 경로만으로는 갈리지 않는다.
            // 원본 두 장이 구분자가 다르므로 <b>둘 다</b> 본다 — 하나만 보면 구분자를 박은
            // 구현이 통과하고, 나머지 한 장이 조용히 안 잘린다.
            Assert.IsTrue(PixelArtImportSettings.IsSheet(
                "Assets/Art/Sprites/Tilemap/belt_tileset.png"));
            Assert.IsTrue(PixelArtImportSettings.IsSheet(
                "Assets/Art/Sprites/Tilemap/sushi_outgoing-tileset.png"));
        }

        [Test]
        public void IsSheet_SingleTileInTheSameFolder_ReturnsFalse()
        {
            Assert.IsFalse(PixelArtImportSettings.IsSheet(
                "Assets/Art/Sprites/Tilemap/map-tile-1.png"));
        }

        [Test]
        public void IsSheet_PlainSprite_ReturnsFalse()
        {
            Assert.IsFalse(PixelArtImportSettings.IsSheet(SpritePath));
        }

        [Test]
        public void PlanFor_AnimationSheet_ImportsAsMultiple()
        {
            // Single 로 들어오면 128×32 스프라이트 한 장이 되어 클립을 만들 프레임이 없다.
            Assert.AreEqual(SpriteImportMode.Multiple,
                            PixelArtImportSettings.PlanFor(
                                "Assets/Art/Animations/customer-standard-idle.png").SpriteMode);
        }

        [Test]
        public void PlanFor_PlainSprite_ImportsAsSingle()
        {
            Assert.AreEqual(SpriteImportMode.Single,
                            PixelArtImportSettings.PlanFor(SpritePath).SpriteMode);
        }

        [Test]
        public void IsSheet_UiSprite_ReturnsFalse()
        {
            // 사람이 스프라이트 에디터에서 나눠 둔 UI 스프라이트를 임포터가 도로 합치면
            // 하위 스프라이트가 사라지고, 그것을 물고 있던 프리팹이 그림 없이 뜬다.
            Assert.IsFalse(PixelArtImportSettings.IsSheet("Assets/Art/Sprites/UI/bar-cell.png"));
            Assert.IsFalse(PixelArtImportSettings.IsSheet("Assets/Art/Sprites/UI/card-frame.png"));
        }

        [Test]
        public void PlanFor_AnimationSheet_KeepsEveryOtherPixelArtRule()
        {
            // 시트라고 필터·PPU 가 달라지면 낱장과 크기·선명도가 어긋난다.
            var plan = PixelArtImportSettings.PlanFor(
                "Assets/Art/Animations/customer-standard-idle.png");

            Assert.AreEqual(FilterMode.Point, plan.FilterMode);
            Assert.AreEqual(32, plan.PixelsPerUnit);
        }
    }
}
