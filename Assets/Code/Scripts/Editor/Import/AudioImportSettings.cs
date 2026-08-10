using System;

namespace SushiDefense.EditorTools.Import
{
    /// <summary>
    /// 오디오 클립에 적용할 임포트 설정을 <b>결정만</b> 한다.
    /// <see cref="PixelArtImportSettings"/> 와 같은 모양이며 같은 이유로 둘로 나뉘어 있다 —
    /// 임포터를 만지지 않으므로 "이 경로에 어떤 설정이 붙나" 를 EditMode 로 확인할 수 있다.
    ///
    /// <para>
    /// 사람 손에 맡기지 않는 이유: 음원을 다시 만들거나 추가할 때마다 파일 세 항목을 기억해야
    /// 하고, 빠져도 소리는 그대로 나서 <b>메모리에서만 손해가 난다.</b> 화면에 증상이 없는
    /// 종류의 실수라 더 오래 남는다.
    /// </para>
    /// </summary>
    public static class AudioImportSettings
    {
        /// <summary>
        /// 배경음이 사는 곳. 여기와 <see cref="SoundRoot"/> 만 이 규칙의 대상이며,
        /// <b>끝 슬래시가 의미를 갖는다</b> — 없으면 형제 폴더까지 빨려 들어간다.
        /// </summary>
        private const string MusicRoot = "Assets/Audio/Music/";

        /// <summary>효과음이 사는 곳.</summary>
        private const string SoundRoot = "Assets/Audio/Sound/";

        /// <summary>이 경로가 오디오 규칙의 대상인가.</summary>
        public static bool AppliesTo(string assetPath)
        {
            return StartsWith(assetPath, MusicRoot) || StartsWith(assetPath, SoundRoot);
        }

        /// <summary>
        /// 이 경로에 적용할 설정을 계산한다. 대상이 아니면
        /// <see cref="AudioImportPlan.IsManaged"/> 가 <c>false</c> 인 계획을 돌려주고,
        /// 부르는 쪽은 아무것도 하지 않는다.
        /// </summary>
        public static AudioImportPlan PlanFor(string assetPath)
        {
            if (StartsWith(assetPath, MusicRoot))
            {
                return AudioImportPlan.Music();
            }

            return StartsWith(assetPath, SoundRoot) ? AudioImportPlan.Sound() : default;
        }

        private static bool StartsWith(string assetPath, string root)
        {
            return !string.IsNullOrEmpty(assetPath)
                   && assetPath.StartsWith(root, StringComparison.Ordinal);
        }
    }
}
