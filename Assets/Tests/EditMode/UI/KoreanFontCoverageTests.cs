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
    /// 정적 서브셋의 유일한 실패 모드가 <b>"나중에 추가한 문구의 글자가 빠지는 것"</b>이고,
    /// 그 증상은 빌드해야만 드러난다 — 에디터에서는 시스템 폰트가 메워 주기 때문이다.
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
                Assert.Ignore($"폰트 애셋이 아직 없습니다: {FontPath} — step-09 의 §7 승인 대기 중");
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
            AssertCovers("보상 선택 받을 보상 없음 (Esc) 영입");
        }

        [Test]
        public void Font_CoversDigitsAndSeparators()
        {
            AssertCovers("0123456789/~()");
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
