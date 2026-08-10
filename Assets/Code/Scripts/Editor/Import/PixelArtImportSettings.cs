using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SushiDefense.EditorTools.Import
{
    /// <summary>
    /// 픽셀 아트 텍스처에 적용할 임포트 설정을 <b>결정만</b> 한다.
    ///
    /// <para>
    /// 임포터를 만지지 않으므로 "이 경로에 어떤 설정이 붙나" 를 EditMode 로 확인할 수 있다.
    /// 판단을 <see cref="PixelArtTextureImporter"/> 안에 두면 Unity 가 임포트 중에 부르는
    /// 콜백이라 테스트에서 호출할 방법이 없다 — <c>MonoBehaviour</c> 에서 로직을 빼는 것과
    /// 같은 이유다 (<c>CLAUDE.md</c> §3.2).
    /// </para>
    /// <para>
    /// <b>사람 손에 맡기지 않는 이유</b>: 스프라이트를 추가할 때마다 인스펙터에서 네 항목을
    /// 기억해야 하고, 하나만 빠져도 픽셀 아트가 흐릿하게 뭉개진다. 증상이 "약간 이상함" 이라
    /// 발견도 늦다.
    /// </para>
    /// </summary>
    public static class PixelArtImportSettings
    {
        /// <summary>
        /// 1 유닛을 채우는 픽셀 수. <b>밸런스가 아니라 파이프라인 설정</b>이라 SO 가 아니라
        /// 여기 있다 — 사람이 플레이하며 조정할 값이 아니고, 값이 갈리는 것 자체가 버그다.
        ///
        /// <para>
        /// 32 인 이유: 원본 스프라이트를 32×32 로 그리면 초밥 하나가 정확히 1 유닛이 된다.
        /// 벨트 길이 20 에 자리가 4·8·12·16 인 배치가 이 축척을 전제로 서 있다.
        /// </para>
        /// </summary>
        public const int PixelsPerUnit = 32;

        /// <summary>
        /// 규칙이 걸리는 폴더. <b>끝 슬래시가 의미를 갖는다</b> — 없으면
        /// <c>Assets/Art/SpritesOld/</c> 같은 형제 폴더까지 빨려 들어간다.
        ///
        /// <para>
        /// <c>Assets/Art/Fonts/</c> 는 일부러 빠져 있다. 폰트 아틀라스가 이 규칙에 걸리면
        /// 스프라이트로 임포트되어 TMP 폰트가 깨진다.
        /// </para>
        /// </summary>
        private static readonly string[] PixelArtRoots =
        {
            "Assets/Art/Sprites/",
            "Assets/Art/Animations/",
            "Assets/Level/Placeholder/"
        };

        /// <summary>
        /// 칸이 가로로 이어 붙은 시트가 사는 곳. 폴더 통째로 시트인 곳은 여기 하나뿐이다.
        /// </summary>
        private const string SheetRoot = "Assets/Art/Animations/";

        /// <summary>
        /// 폴더가 아니라 <b>이름</b>으로 시트임을 밝히는 접미. 타일 세트는 스프라이트 폴더
        /// 안에 낱장들과 섞여 살기 때문에 경로만으로는 갈리지 않는다.
        ///
        /// <para>
        /// 이름 규칙에 기대는 것이 마음에 걸리지만, 대안은 파일 목록을 코드에 박는 것이고
        /// 그러면 타일 세트를 하나 추가할 때마다 이 파일을 고쳐야 한다.
        /// </para>
        /// <para>
        /// <b>구분자를 포함하지 않는다.</b> 원본 파일이 <c>belt_tileset</c> 과
        /// <c>sushi_outgoing-tileset</c> 으로 갈려 있어, 구분자까지 박으면 둘 중 하나만 걸린다.
        /// </para>
        /// </summary>
        private const string SheetSuffix = "tileset";

        /// <summary>이 경로가 픽셀 아트 규칙의 대상인가.</summary>
        public static bool AppliesTo(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            for (var i = 0; i < PixelArtRoots.Length; i++)
            {
                if (assetPath.StartsWith(PixelArtRoots[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 이 경로에 적용할 설정을 계산한다. 대상이 아니면
        /// <see cref="PixelArtImportPlan.IsPixelArt"/> 가 <c>false</c> 인 계획을 돌려주고,
        /// 부르는 쪽은 아무것도 하지 않는다 — <b>대상 밖 텍스처에는 손대지 않는다.</b>
        /// </summary>
        public static PixelArtImportPlan PlanFor(string assetPath)
        {
            if (!AppliesTo(assetPath))
            {
                return default;
            }

            return IsSheet(assetPath) ? PixelArtImportPlan.Sheet() : PixelArtImportPlan.PixelArt();
        }

        /// <summary>
        /// 이 텍스처가 여러 칸으로 잘려야 하는가. 폴더로 한 번, 파일 이름으로 한 번 본다.
        /// </summary>
        public static bool IsSheet(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (assetPath.StartsWith(SheetRoot, StringComparison.Ordinal))
            {
                return true;
            }

            var name = Path.GetFileNameWithoutExtension(assetPath);
            return name.EndsWith(SheetSuffix, StringComparison.Ordinal);
        }
    }
}
