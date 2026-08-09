using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Editor
{
    /// <summary>
    /// M6 이 더한 UI 스프라이트의 <b>크기와 임포트 설정</b>을 고정한다.
    ///
    /// <para>
    /// <c>PixelArtImportSettingsTests</c> 는 «규칙이 이 경로에 적용되는가» 를 보고, 여기서는
    /// «디스크의 애셋이 실제로 그렇게 임포트됐는가» 를 본다. 규칙이 멀쩡해도 애셋이 다른
    /// 경로에서 들어오거나 나중에 손으로 덮이면 규칙만으로는 잡히지 않는다.
    /// </para>
    /// <para>
    /// <b>크기를 박는 이유.</b> SVG 래스터화 경로(<c>Sprite.ImportFromSvg</c>)는 출력을
    /// 정방형 2의 거듭제곱으로 강제하므로 48×64 를 만들 수 없다. 그래서 이 스프라이트들은
    /// 픽셀을 직접 찍어 만들었고(<c>scripts/make-ui-sprites.py</c>), 누군가 SVG 경로로
    /// 다시 만들면 카드 틀이 조용히 64×64 가 된다 — 그때 이 테스트가 잡는다.
    /// </para>
    /// </summary>
    public sealed class UiSpriteAssetTests
    {
        private const string Dir = "Assets/Art/Sprites/UI/";

        /// <summary>이름 → 기대 크기. <c>badge-digesting</c> 의 16×16 은 요구사항이다.</summary>
        private static readonly Dictionary<string, Vector2Int> Expected = new()
        {
            ["card-frame"] = new Vector2Int(48, 64),
            ["card-frame-disabled"] = new Vector2Int(48, 64),
            ["badge-digesting"] = new Vector2Int(16, 16),
            ["bar-cell"] = new Vector2Int(8, 8),
            ["button"] = new Vector2Int(32, 32),
            ["button-pressed"] = new Vector2Int(32, 32),
            ["panel"] = new Vector2Int(32, 32),
            ["icon-deck"] = new Vector2Int(16, 16),
            ["icon-menu"] = new Vector2Int(16, 16),
        };

        /// <summary>
        /// 9-slice 로 늘어나는 것들. 나머지는 테두리가 0 이어야 한다.
        ///
        /// <para>
        /// 카드 틀이 M6.5 에서 여기 들어왔다. 원본이 48×64 인데 카드가 128×192 가 되면서
        /// <b>그냥 늘리면 픽셀 아트의 각이 죽는다</b> — 테두리를 주면 모서리는 원본 픽셀
        /// 그대로 남고 가운데만 늘어난다.
        /// </para>
        /// </summary>
        private static readonly Dictionary<string, float> SliceBorders = new()
        {
            ["button"] = 6f,
            ["button-pressed"] = 6f,
            ["panel"] = 8f,
            ["card-frame"] = 8f,
            ["card-frame-disabled"] = 8f,
        };

        private static IEnumerable<string> Names => Expected.Keys;

        [Test]
        public void BadgeDigesting_IsExactlySixteenSquare()
        {
            // 크기가 요구사항이라 따로 박는다 — 배지가 커지면 손님 위에 겹쳐 얼굴을 가린다.
            var texture = Load("badge-digesting");

            Assert.AreEqual(16, texture.width);
            Assert.AreEqual(16, texture.height);
        }

        [Test]
        public void CardFrame_IsNotSnappedToPowerOfTwo()
        {
            // 48×64 는 정방형도 2의 거듭제곱도 아니다. 64×64 로 바뀌어 있으면 누군가
            // SVG 래스터화 경로로 다시 만든 것이다.
            var texture = Load("card-frame");

            Assert.AreEqual(48, texture.width);
            Assert.AreEqual(64, texture.height);
        }

        [Test]
        public void EverySprite_HasItsExpectedSize([ValueSource(nameof(Names))] string name)
        {
            var texture = Load(name);
            var expected = Expected[name];

            Assert.AreEqual(expected.x, texture.width, $"{name} 폭");
            Assert.AreEqual(expected.y, texture.height, $"{name} 높이");
        }

        [Test]
        public void EverySprite_UsesPixelArtImportSettings([ValueSource(nameof(Names))] string name)
        {
            var importer = Importer(name);

            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, name);
            Assert.AreEqual(32f, importer.spritePixelsPerUnit, 0.001f, $"{name} PPU");
            Assert.AreEqual(FilterMode.Point, importer.filterMode, $"{name} 필터 — 흐려진다");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression,
                            $"{name} 압축 — 픽셀이 뭉개진다");
            Assert.IsFalse(importer.mipmapEnabled, $"{name} 밉맵");
        }

        /// <summary>
        /// <b>9-slice 는 <c>FullRect</c> 여야 한다.</b> <c>Tight</c> 로 임포트되면 테두리가
        /// 지정돼 있어도 늘어나는 모양이 깨진다 — 버튼이 찌그러진 형태로 나온다.
        /// </summary>
        [Test]
        public void EverySprite_UsesFullRectMesh([ValueSource(nameof(Names))] string name)
        {
            // 메시 타입은 TextureImporter 의 프로퍼티가 아니라 설정 구조체 안에 있다.
            var settings = new TextureImporterSettings();
            Importer(name).ReadTextureSettings(settings);

            Assert.AreEqual(SpriteMeshType.FullRect, settings.spriteMeshType, $"{name} 메시 타입");
        }

        /// <summary>
        /// <b>임포터 필드가 아니라 로드된 스프라이트를 읽는다.</b> 임포터의
        /// <c>spriteBorder</c> 는 Single 모드용 입력이고, 이 스프라이트들은 Multiple 모드라
        /// 실제 값이 <b>스프라이트 시트 항목</b> 쪽에 산다 — 둘이 어긋나면 임포터만 보는
        /// 검사는 «테두리를 넣었는데 화면은 그대로» 를 놓친다. <c>Image</c> 가 쓰는 것은
        /// 여기 <see cref="Sprite.border"/> 다.
        /// </summary>
        [Test]
        public void StretchedSprites_HaveNineSliceBorders(
            [ValueSource(nameof(SliceBorderNames))] string name)
        {
            var border = LoadSprite(name).border;
            var expected = SliceBorders[name];

            Assert.AreEqual(expected, border.x, 0.001f, $"{name} 왼쪽");
            Assert.AreEqual(expected, border.y, 0.001f, $"{name} 아래");
            Assert.AreEqual(expected, border.z, 0.001f, $"{name} 오른쪽");
            Assert.AreEqual(expected, border.w, 0.001f, $"{name} 위");
        }

        /// <summary>
        /// 늘어나지 않는 스프라이트에 테두리가 붙으면 <b>가운데만 그려진다.</b> 아이콘이
        /// 잘려 보이는데 원인이 임포터에 있어 찾기 어렵다.
        /// </summary>
        [Test]
        public void FixedSprites_HaveNoBorder([ValueSource(nameof(FixedNames))] string name)
        {
            Assert.AreEqual(Vector4.zero, LoadSprite(name).border, name);
        }

        private static Sprite LoadSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}{name}.png");

            Assert.IsNotNull(sprite, $"{name} 의 스프라이트를 로드하지 못했다");
            return sprite;
        }

        private static IEnumerable<string> SliceBorderNames => SliceBorders.Keys;

        private static IEnumerable<string> FixedNames
        {
            get
            {
                foreach (var name in Expected.Keys)
                {
                    if (!SliceBorders.ContainsKey(name))
                    {
                        yield return name;
                    }
                }
            }
        }

        private static Texture2D Load(string name)
        {
            var path = Dir + name + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(texture, $"{path} 를 로드하지 못했습니다");
            return texture;
        }

        private static TextureImporter Importer(string name)
        {
            var path = Dir + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, $"{path} 의 TextureImporter 를 찾지 못했습니다");
            return importer;
        }
    }
}
