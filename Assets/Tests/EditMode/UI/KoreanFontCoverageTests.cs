using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SushiDefense.Data;
using TMPro;
using UnityEditor;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 폰트 애셋이 <b>화면에 나갈 글자를 전부 갖고 있는지</b> 본다.
    ///
    /// <para>
    /// 배포 타깃(WebGL)은 OS 폰트에 접근할 수 없어, 폰트에 없는 글자는 두부(□)로 나온다.
    /// 그 증상은 <b>빌드해야만 드러난다</b> — 에디터에서는 시스템 폰트가 메워 주기 때문이다.
    /// </para>
    /// <para>
    /// <b>M6 에서 서브셋의 출처가 바뀌었다.</b> 예전에는 소스의 문자열 리터럴에서 글자를
    /// 모아, 문구를 한 줄 고칠 때마다 사람이 폰트를 다시 구워야 했다 (§7). 지금은
    /// <b>폰트가 그릴 수 있는 글자를 전부</b> 굽는다 — 실측해 보니 전체를 담아도 아틀라스
    /// 한 장(1024×1024, 48% 점유)이라 아낄 대상이 아니었다.
    /// </para>
    /// <para>
    /// 그래서 남은 실패 모드는 둘이다: <b>목록을 다시 뽑고 폰트를 안 구운 경우</b>
    /// (<see cref="Font_CoversEveryCharacterInTheCharsetFile"/> 가 잡는다)와,
    /// <b>이 폰트에 아예 없는 글자를 쓴 경우</b> — 현대 한글 11,172 음절 중 2,791 자만
    /// 있어서 드문 음절은 서브셋을 넓혀도 두부가 된다. 후자는 개별 문구 테스트가 잡는다.
    /// </para>
    /// <para>
    /// <b>이 테스트는 디스크의 애셋을 로드한다.</b> <c>.claude/rules/tests.md</c> §4 의 금지는
    /// 밸런스 <i>수치</i> 에 대한 것이고, 여기서 보는 것은 커버리지다. 문구를 바꾸면
    /// 폰트를 다시 구워야 하는 것이 맞으므로 <b>깨져야 정상인 테스트</b>다.
    /// </para>
    /// </summary>
    public sealed class KoreanFontCoverageTests
    {
        private const string FontPath = "Assets/Art/Fonts/SushiRailBite-KR.asset";
        private const string CharsetPath = "Assets/Art/Fonts/charset-ko.txt";

        private TMP_FontAsset _font;

        [SetUp]
        public void SetUp()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (_font == null)
            {
                Assert.Ignore($"폰트 애셋이 아직 없습니다: {FontPath} — 굽는 것은 사람의 몫이다 (§7)");
            }
        }

        [Test]
        public void Font_CoversHudLabels()
        {
            AssertCovers("매출 남은 시간 영입 재화 대기 손님 배치 예정 없음");
        }

        [Test]
        public void Font_CoversOutcomeLabels()
        {
            AssertCovers("클리어 실패");
        }

        [Test]
        public void Font_CoversTransitionLabels()
        {
            AssertCovers("스테이지 클리어 — 다음: 런 완료! (Enter)");
        }

        [Test]
        public void Font_CoversRewardLabels()
        {
            AssertCovers("보상 선택 받을 보상 없음 (Esc) 영입 건너뛰기");
        }

        [Test]
        public void Font_CoversDigitsAndSeparators()
        {
            AssertCovers("0123456789/~()");
        }

        /// <summary>
        /// 메인 화면의 문구. <b>버튼 라벨은 코드가 아니라 씬에 있다</b> (step-12) — 추출기의
        /// 리터럴 검사가 보지 못하는 자리라 여기 직접 박아 둔다.
        /// </summary>
        [Test]
        public void Font_CoversMainMenuLabels()
        {
            AssertCovers("스시 레일 바이트 게임 시작 설정 백과사전");
        }

        [Test]
        public void Font_CoversStageMenuLabels()
        {
            AssertCovers("메뉴 일시정지 진행 중 다시 시작 나가기 닫기 덱 멈춤 재개 재시작");
        }

        [Test]
        public void Font_CoversSettingsLabels()
        {
            AssertCovers("설정 소리 전체화면");
        }

        /// <summary>
        /// 손님 정보 창의 문구. <b>재굽기가 필요 없다</b> — M6 에서 폰트를 전체 커버리지로
        /// 굽는 방식으로 바꾼 덕에 이 글자들이 이미 들어 있다. 이 테스트는 그 사실을 고정하는
        /// 그물이지 새 작업을 부르는 신호가 아니다.
        /// </summary>
        [Test]
        public void Font_CoversInspectorLabels()
        {
            AssertCovers("손님 정보 유형 범위 대역 먹는 시간 포화도 소화 영입 비용 인구수 "
                         + "상태 대기 중 닫기 기본 소식 먹보");
        }

        [Test]
        public void Font_CoversCodexLabels()
        {
            AssertCovers("백과사전 포화 영입");
        }

        /// <summary>카드의 스탯 행 문구 (M6.5). 행 단위로 나뉘면서 라벨이 늘었다.</summary>
        [Test]
        public void Font_CoversCardStatRows()
        {
            AssertCovers("가격 포화 범위 대역 포화도 소화 초 인구수");
        }

        [Test]
        public void Font_CoversSushiDisplayNames()
        {
            AssertCovers(DisplayNamesOf<SushiData>());
        }

        [Test]
        public void Font_CoversCustomerDisplayNames()
        {
            AssertCovers(DisplayNamesOf<CustomerData>());
        }

        /// <summary>
        /// <b>SDF 로 구우면 픽셀이 죽는다.</b> 모서리가 둥글려져 픽셀 폰트의 각이 사라지는데,
        /// 에디터의 작은 라벨에서는 <i>"좀 흐린가?"</i> 정도로만 보여 눈으로 구분되지 않는다.
        ///
        /// <para>
        /// 재굽기는 사람이 TMP Font Asset Creator 에서 하는 일이라(§7) 설정이 말로만
        /// 전달된다 — <b>그래서 여기 숫자로 박아 둔다.</b> 값은 M5 가 처음 구운 애셋에서
        /// 읽은 것이고, 바뀌었다면 굽기 설정이 어긋난 것이다.
        /// </para>
        /// </summary>
        [Test]
        public void Font_RenderMode_StaysRaster()
        {
            Assert.AreEqual(4118, (int)_font.atlasRenderMode,
                            "굽기 설정이 바뀌었습니다 — Render Mode 를 RASTER 로 되돌리세요");
        }

        /// <summary>폰트 네이티브 높이의 정수배가 아니면 글자가 흐려진다.</summary>
        [Test]
        public void Font_SamplingPointSize_Is12()
        {
            Assert.AreEqual(12, _font.creationSettings.pointSize);
        }

        [Test]
        public void Font_AtlasPadding_Is1()
        {
            Assert.AreEqual(1, _font.atlasPadding);
        }

        /// <summary>
        /// 아틀라스가 <b>한 장</b>이어야 한다. 여러 장으로 갈리면 드로우콜이 늘고, 이 폰트의
        /// 글자 수(3,247)는 1024×1024 에 48% 로 들어가므로 갈릴 이유가 없다 — 갈렸다면
        /// 해상도를 작게 잡고 구운 것이다.
        /// </summary>
        [Test]
        public void Font_Atlas_IsSingleTexture()
        {
            Assert.AreEqual(1, _font.atlasTextures.Length,
                            "아틀라스가 여러 장입니다 — 1024×1024 로 다시 구우세요");
        }

        [Test]
        public void Font_CoversEveryCharacterInTheCharsetFile()
        {
            // 서브셋을 굽는 입력이 목록 파일이므로, 폰트와 목록이 어긋나면 여기서 잡힌다 —
            // 목록만 다시 뽑고 폰트를 안 구운 경우가 그것이다.
            Assert.IsTrue(File.Exists(CharsetPath), $"{CharsetPath} 가 없습니다");
            AssertCovers(File.ReadAllText(CharsetPath).Trim());
        }

        /// <summary>
        /// 밸런스 애셋의 표시 이름을 모은다. 초밥·손님을 추가하고 이름에 새 글자를 쓰면
        /// 여기서 잡힌다 — <b>코드로 세운 하네스는 애셋을 보지 않는다.</b>
        /// </summary>
        private static string DisplayNamesOf<T>() where T : UnityEngine.Object
        {
            var names = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);

                var display = asset switch
                {
                    SushiData sushi => sushi.DisplayName,
                    CustomerData customer => customer.DisplayName,
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(display))
                {
                    names.Add(display);
                }
            }

            Assert.IsNotEmpty(names, $"{typeof(T).Name} 애셋을 하나도 찾지 못했습니다");
            return string.Join(" ", names);
        }

        private void AssertCovers(string text)
        {
            Assert.IsTrue(_font.HasCharacters(text, out var missing),
                          $"폰트에 없는 글자: {(missing == null ? "?" : string.Join("", missing))}");
        }
    }
}
