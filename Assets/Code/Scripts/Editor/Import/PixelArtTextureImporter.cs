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
        }
    }
}
