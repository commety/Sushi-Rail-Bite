using UnityEditor;
using UnityEngine;

namespace SushiDefense.EditorTools.Import
{
    /// <summary>
    /// 오디오 클립 한 개에 적용할 임포트 설정.
    ///
    /// <para>
    /// 배포 타깃(WebGL)은 <c>Streaming</c> 을 지원하지 않는다 — 지정해도 조용히 다른 모드로
    /// 떨어지므로 여기서는 아예 후보에 두지 않는다.
    /// </para>
    /// </summary>
    public readonly struct AudioImportPlan
    {
        private AudioImportPlan(AudioClipLoadType loadType)
        {
            IsManaged = true;
            LoadType = loadType;
            CompressionFormat = AudioCompressionFormat.Vorbis;
            PreloadAudioData = true;
        }

        /// <summary>이 계획을 적용해야 하는가. <c>false</c> 면 임포터는 손을 뗀다.</summary>
        public bool IsManaged { get; }

        /// <summary>
        /// 클립을 메모리에 어떻게 올릴지. <b>배경음과 효과음이 갈리는 유일한 지점</b>이다.
        /// </summary>
        public AudioClipLoadType LoadType { get; }

        /// <summary>압축 형식. 짧은 효과음도 한 번만 풀리므로 재생 비용 차이가 없다.</summary>
        public AudioCompressionFormat CompressionFormat { get; }

        /// <summary>첫 재생에서 끊기지 않게 미리 올린다.</summary>
        public bool PreloadAudioData { get; }

        /// <summary>
        /// 배경음. <b>통째로 풀지 않는다</b> — 32초치가 메모리에 남는데 한 번에 한 곡만
        /// 울리므로 푸는 이득이 없다.
        /// </summary>
        public static AudioImportPlan Music()
        {
            return new AudioImportPlan(AudioClipLoadType.CompressedInMemory);
        }

        /// <summary>
        /// 효과음. <b>미리 풀어 둔다</b> — 짧고 자주 나므로 재생할 때마다 푸는 비용이 더 크다.
        /// </summary>
        public static AudioImportPlan Sound()
        {
            return new AudioImportPlan(AudioClipLoadType.DecompressOnLoad);
        }
    }
}
