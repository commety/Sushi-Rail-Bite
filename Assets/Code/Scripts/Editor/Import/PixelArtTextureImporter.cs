using System.IO;
using UnityEditor;

namespace SushiDefense.EditorTools.Import
{
    /// <summary>
    /// <see cref="PixelArtImportSettings"/> 가 정한 계획을 임포터에 옮긴다.
    /// <b>판단하지 않는다</b> — 무엇을 적용할지는 전부 그쪽에서 정해져 온다.
    ///
    /// <para>
    /// Unity 가 임포트 도중에 부르는 콜백이라 테스트에서 직접 호출할 수 없다. 그래서 이
    /// 클래스는 얇게 두고, 검증 가능한 부분을 전부 밖으로 뺐다.
    /// </para>
    /// </summary>
    internal sealed class PixelArtTextureImporter : AssetPostprocessor
    {
        /// <summary>
        /// 규칙을 고쳤을 때 올린다. <b>이 값이 그대로면 이미 들어온 텍스처는 다시 임포트되지
        /// 않는다</b> — 규칙만 바뀌고 애셋은 옛 설정 그대로라, 코드는 맞는데 화면만 틀린
        /// 상태가 된다.
        ///
        /// <para>
        /// 2: 시트를 여러 칸으로 자르는 규칙이 생겼다.
        /// 3: 시트를 가리는 이름 규칙이 <c>belt_tileset</c> 을 놓쳤다.
        /// </para>
        /// </summary>
        public override uint GetVersion()
        {
            return 3;
        }

        private void OnPreprocessTexture()
        {
            var plan = PixelArtImportSettings.PlanFor(assetPath);
            if (!plan.IsPixelArt)
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = plan.TextureType;
            importer.filterMode = plan.FilterMode;
            importer.mipmapEnabled = plan.MipmapEnabled;
            importer.spritePixelsPerUnit = plan.PixelsPerUnit;
            importer.textureCompression = plan.Compression;
            importer.alphaIsTransparency = plan.AlphaIsTransparency;

            // 메시 타입은 임포터에 직접 뚫린 프로퍼티가 없어 설정 묶음을 통해야 한다.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = plan.MeshType;
            importer.SetTextureSettings(settings);

            // 시트일 때만 자르기 모드를 건드린다. 낱장까지 `Single` 로 못박으면 사람이
            // 스프라이트 에디터에서 나눠 둔 것을 임포터가 매번 되돌린다 — 실제로 UI
            // 스프라이트 열 장의 하위 스프라이트가 통째로 사라졌고, 참조가 끊긴 프리팹은
            // 그림 없이 조용히 떴다. **임포터는 «빠지면 언제나 틀린 것» 만 정한다.**
            if (plan.SpriteMode != SpriteImportMode.Multiple)
            {
                return;
            }

            // 모드를 설정 묶음보다 뒤에 쓴다. SetTextureSettings 가 이 값을 되돌려 놓기
            // 때문이며, 순서를 바꾸면 시트가 조용히 한 장으로 들어온다.
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritesheet = SliceOf(importer);
        }

        /// <summary>
        /// 시트를 칸으로 나눈다. 원본 크기는 <b>임포트 전에</b> 알아야 하는데, 이 시점에는
        /// 아직 텍스처 객체가 없다 — 임포터가 파일에서 직접 읽어 주는 값을 쓴다.
        /// </summary>
        private static SpriteMetaData[] SliceOf(TextureImporter importer)
        {
            importer.GetSourceTextureWidthAndHeight(out var width, out var height);
            var baseName = Path.GetFileNameWithoutExtension(importer.assetPath);
            return SpriteSheetSlicer.Slice(baseName, width, height,
                                           PixelArtImportSettings.PixelsPerUnit);
        }
    }
}
