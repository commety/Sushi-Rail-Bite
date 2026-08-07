using UnityEditor;

namespace SushiDefense.EditorTools.Import
{
    /// <summary>
    /// <see cref="AudioImportSettings"/> 가 정한 계획을 임포터에 옮긴다.
    /// <b>판단하지 않는다.</b>
    ///
    /// <para>
    /// 이름에 <c>Game</c> 을 붙인 것은 <c>UnityEditor.AudioImporter</c> 와 헷갈리지 않게
    /// 하기 위해서다 — 이 클래스는 그 임포터를 <i>설정하는</i> 쪽이지 임포터가 아니다.
    /// </para>
    /// </summary>
    internal sealed class GameAudioImporter : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            var plan = AudioImportSettings.PlanFor(assetPath);
            if (!plan.IsManaged)
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;

            // 샘플 설정은 값 타입이라 꺼내 고친 뒤 되돌려 넣어야 반영된다.
            var settings = importer.defaultSampleSettings;
            settings.loadType = plan.LoadType;
            settings.compressionFormat = plan.CompressionFormat;
            settings.preloadAudioData = plan.PreloadAudioData;
            importer.defaultSampleSettings = settings;
        }
    }
}
