using NUnit.Framework;
using SushiDefense.Customers;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// 디스크의 <c>Customer.prefab</c> 이 <b>포화도 칸과 배지를 실제로 들고 있는지</b> 본다.
    ///
    /// <para>
    /// <c>SaturationBarViewTests</c>·<c>DigestingBadgeViewTests</c> 는 오브젝트를 코드로
    /// 세우므로 이 애셋이 비어 있어도 초록이다. 뷰가 자식을 <b>이름으로</b> 찾는 폴백을
    /// 갖고 있어, 프리팹에 자식이 없으면 표시가 통째로 빠지는데 예외는 나지 않는다
    /// (<c>.claude/rules/tests.md</c> §1 «애셋 등록·설정»).
    /// </para>
    /// <para>
    /// <b>칸 수는 단언한다.</b> 위치·색은 연출이지만 개수는 계약이다 — 먹보의 최대 포화도가
    /// 8 이라 그보다 적으면 그 손님의 포화도가 비례로 접혀 한 입이 한 칸이 아니게 된다.
    /// </para>
    /// </summary>
    public sealed class CustomerPrefabTests
    {
        private const string PrefabPath = "Assets/Code/Scripts/Presentation/Customers/Customer.prefab";

        /// <summary>먹보의 <c>MaxSaturation</c>. 이보다 적으면 그 손님만 근사로 그려진다.</summary>
        private const int BiggestAppetite = 8;

        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        [Test]
        public void Prefab_Exists()
        {
            Assert.IsNotNull(_prefab, $"{PrefabPath} 를 로드하지 못했다");
        }

        [Test]
        public void Prefab_HasSaturationBarChild()
        {
            var bar = _prefab.transform.Find("SaturationBar");

            Assert.IsNotNull(bar, "SaturationBar 자식이 없다");
            Assert.IsNotNull(bar.GetComponent<SaturationBarView>());
        }

        [Test]
        public void Prefab_SaturationBar_CoversTheBiggestAppetite()
        {
            var bar = _prefab.transform.Find("SaturationBar");

            Assert.GreaterOrEqual(bar.childCount, BiggestAppetite,
                                  "칸이 먹보의 포화도보다 적으면 한 입이 한 칸이 아니게 된다");
        }

        [Test]
        public void Prefab_SaturationCells_HaveRenderers()
        {
            var bar = _prefab.transform.Find("SaturationBar");

            for (var i = 0; i < bar.childCount; i++)
            {
                Assert.IsNotNull(bar.GetChild(i).GetComponent<SpriteRenderer>(),
                                 $"{bar.GetChild(i).name} 에 렌더러가 없다");
            }
        }

        /// <summary>
        /// <b>렌더러가 있는 것만으로는 아무것도 안 보인다.</b> 실플레이에서 포화도가 통째로
        /// 안 뜬 절반의 원인이 이것이었다 — 칸 여덟 개의 스프라이트가 전부 비어 있었고,
        /// 위의 테스트는 렌더러의 <i>존재</i>만 봐서 초록이었다
        /// (<c>.claude/rules/tests.md</c> §3 «공허하게 통과하는 테스트»).
        /// </summary>
        [Test]
        public void Prefab_SaturationCells_HaveSprites()
        {
            var bar = _prefab.transform.Find("SaturationBar");

            for (var i = 0; i < bar.childCount; i++)
            {
                var cell = bar.GetChild(i).GetComponent<SpriteRenderer>();
                Assert.IsNotNull(cell.sprite,
                                 $"{bar.GetChild(i).name} 의 그림이 비었다 — 켜도 안 보인다");
            }
        }

        [Test]
        public void Prefab_DigestingBadge_HasSprite()
        {
            var badge = _prefab.transform.Find("DigestingBadge").GetComponent<SpriteRenderer>();

            Assert.IsNotNull(badge.sprite, "배지의 그림이 비었다 — 켜도 안 보인다");
        }

        /// <summary>
        /// 칸과 배지는 손님 몸통 <b>앞</b>에 그려져야 한다. 뒤면 그림을 물려도 몸통에 가려
        /// 안 보이는데, 이것 역시 예외 없이 «표시만 빠지는» 형태다.
        /// </summary>
        [Test]
        public void Prefab_OverlaysDrawInFrontOfTheBody()
        {
            var body = _prefab.GetComponent<SpriteRenderer>();
            var bar = _prefab.transform.Find("SaturationBar");
            var badge = _prefab.transform.Find("DigestingBadge").GetComponent<SpriteRenderer>();

            Assert.IsNotNull(body, "전제: 몸통에 렌더러가 있다");
            Assert.Greater(badge.sortingOrder, body.sortingOrder, "배지가 몸통 뒤에 있다");

            for (var i = 0; i < bar.childCount; i++)
            {
                Assert.Greater(bar.GetChild(i).GetComponent<SpriteRenderer>().sortingOrder,
                               body.sortingOrder, $"{bar.GetChild(i).name} 이 몸통 뒤에 있다");
            }
        }

        [Test]
        public void Prefab_HasDigestingBadgeChild()
        {
            var badge = _prefab.transform.Find("DigestingBadge");

            Assert.IsNotNull(badge, "DigestingBadge 자식이 없다");
            Assert.IsNotNull(badge.GetComponent<DigestingBadgeView>());
            Assert.IsNotNull(badge.GetComponent<SpriteRenderer>());
        }

        /// <summary>
        /// 배지는 손님 <b>위</b>에, 포화도 바는 <b>아래</b>에 둔다. 겹치면 둘 다 못 읽는다.
        /// </summary>
        [Test]
        public void Prefab_BadgeSitsAboveBar()
        {
            var badge = _prefab.transform.Find("DigestingBadge");
            var bar = _prefab.transform.Find("SaturationBar");

            Assert.Greater(badge.localPosition.y, bar.localPosition.y);
        }

        /// <summary>
        /// <b>대역 문구가 화면에 나오는지</b> 본다.
        ///
        /// <para>
        /// <c>CustomerView</c> 는 라벨을 <see cref="TMP_Text"/> 로 찾는데, 이 자식에 레거시
        /// <c>TextMesh</c> 만 있으면 <b>찾기가 조용히 <c>null</c> 을 돌려주고 글자가 어디에도
        /// 쓰이지 않는다.</b> 그런데 <c>CustomerViewTests</c>·<c>Stage01SceneTests</c> 는 둘 다
        /// <c>BandText</c> <i>프로퍼티</i>를 보므로 전부 초록이다 — 계산은 맞고 화면만 빈다
        /// (<c>.claude/rules/tests.md</c> §1 «애셋 등록·설정»).
        /// </para>
        /// <para>
        /// 실제로 M6 내내 그 상태였다. 프로퍼티가 아니라 <b>애셋의 컴포넌트 타입</b>을 보는
        /// 테스트가 여기 말고는 없다.
        /// </para>
        /// </summary>
        [Test]
        public void Prefab_BandLabel_IsTmpText()
        {
            var label = _prefab.transform.Find("BandLabel");

            Assert.IsNotNull(label, "BandLabel 자식이 없다");
            Assert.IsNotNull(label.GetComponent<TMP_Text>(),
                             "BandLabel 이 TMP_Text 가 아니다 — 대역 문구가 화면에 안 나온다");
        }

        /// <summary>
        /// <b>폰트가 비면 두부(□)가 된다.</b> WebGL 은 OS 폰트에 접근할 수 없어 TMP 가
        /// 기본 폰트(LiberationSans, 한글 없음)로 폴백하는데, 에디터에서는 시스템 폰트가
        /// 메워 주므로 <b>빌드해야만 드러난다</b>
        /// (<c>.claude/domain/presentation-and-audio.md</c> §5).
        /// </summary>
        [Test]
        public void Prefab_BandLabel_HasKoreanFont()
        {
            var label = _prefab.transform.Find("BandLabel").GetComponent<TMP_Text>();

            Assert.IsNotNull(label.font, "폰트가 비었다 — 한글이 두부가 된다");
        }
    }
}
