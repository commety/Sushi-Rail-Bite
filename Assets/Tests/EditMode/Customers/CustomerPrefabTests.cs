using System.Collections.Generic;
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

        /// <summary>
        /// <c>Animator</c> 는 <b>몸통 렌더러와 같은 오브젝트</b>여야 한다. 클립의 곡선이 빈
        /// 경로(=«컨트롤러가 달린 오브젝트 자신»)에 묶여 있어서, 자식에 달면 아무것도
        /// 움직이지 않는데 <b>예외도 경고도 나지 않는다.</b>
        /// </summary>
        [Test]
        public void Prefab_Animator_SitsOnTheBodyRenderer()
        {
            Assert.IsNotNull(_prefab.GetComponent<Animator>(), "뿌리에 Animator 가 없다");
            Assert.IsNotNull(_prefab.GetComponent<SpriteRenderer>(),
                             "전제: 몸통 렌더러가 뿌리에 있다");
            Assert.IsNotNull(_prefab.GetComponent<CustomerMotionView>(),
                             "뿌리에 동작 재생기가 없다");
        }

        /// <summary>
        /// 말풍선은 <b>꺼진 채로</b> 저장돼 있어야 한다. 켜 두면 자리에 앉는 손님이 전부
        /// 고민하는 것으로 보이고, 조율자가 «대기 아님» 이라고 답해도 뷰는 <b>바뀐 프레임에만</b>
        /// 쓰므로 첫 프레임의 오해가 그대로 남는다.
        /// </summary>
        [Test]
        public void Prefab_ThinkingBubble_IsPresentButOff()
        {
            var thinking = _prefab.transform.Find(CustomerMotionView.ThinkingChildName);

            Assert.IsNotNull(thinking, $"{CustomerMotionView.ThinkingChildName} 자식이 없다");
            Assert.IsFalse(thinking.gameObject.activeSelf, "말풍선이 켜진 채로 저장됐다");
        }

        /// <summary>
        /// 렌더러와 컨트롤러가 <b>둘 다</b> 있어야 무언가 보인다. 하나만 보면 «켜도 안 보이는»
        /// 말풍선이 그대로 통과한다 — 포화도 칸에서 실제로 겪은 형태다.
        /// </summary>
        [Test]
        public void Prefab_ThinkingBubble_HasSpriteAndController()
        {
            var thinking = _prefab.transform.Find(CustomerMotionView.ThinkingChildName);
            var renderer = thinking.GetComponent<SpriteRenderer>();

            Assert.IsNotNull(renderer, "말풍선에 렌더러가 없다");
            Assert.IsNotNull(renderer.sprite, "말풍선의 그림이 비었다 — 켜도 첫 프레임이 빈다");
            Assert.IsNotNull(thinking.GetComponent<Animator>()?.runtimeAnimatorController,
                             "말풍선에 물릴 클립이 없다 — 떠도 움직이지 않는다");
        }

        /// <summary>말풍선도 몸통 <b>앞</b>에 그려져야 한다. 뒤면 몸통에 가려 안 보인다.</summary>
        [Test]
        public void Prefab_ThinkingBubble_DrawsInFrontOfTheBody()
        {
            var body = _prefab.GetComponent<SpriteRenderer>();
            var thinking = _prefab.transform.Find(CustomerMotionView.ThinkingChildName)
                                  .GetComponent<SpriteRenderer>();

            Assert.Greater(thinking.sortingOrder, body.sortingOrder, "말풍선이 몸통 뒤에 있다");
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
        /// 배지는 손님 <b>위</b>에, 포화도 바는 <b>옆</b>에 선다 — 둘이 <b>다른 축</b>을 쓴다.
        ///
        /// <para>
        /// 예전에는 배지가 위, 바가 아래였다. 머리 위로 나란히 쌓는 안도 있었지만 세로 예산이
        /// 없다 — 머리 끝에서 벨트 레일 바닥까지 0.8 유닛인데 배지가 이미 0.5 를 쓴다.
        /// 축을 가르면 <b>어느 쪽이 켜지든 서로를 신경 쓸 필요가 없다.</b>
        /// </para>
        /// </summary>
        [Test]
        public void Prefab_BadgeAndBar_UseDifferentAxes()
        {
            var badge = _prefab.transform.Find("DigestingBadge");
            var bar = _prefab.transform.Find("SaturationBar");
            var body = _prefab.GetComponent<SpriteRenderer>();

            Assert.Greater(badge.localPosition.y, body.bounds.extents.y, "배지가 머리 위가 아니다");
            Assert.Less(Mathf.Abs(badge.localPosition.x), body.bounds.extents.x, "배지가 옆으로 새 있다");
            Assert.Greater(Mathf.Abs(bar.localPosition.x), body.bounds.extents.x, "바가 옆이 아니다");
        }

        /// <summary>
        /// 배지 안의 숫자 라벨. 이름은 <c>DigestingBadgeView</c> 의 상수와 맺은 약속이라
        /// 하나만 틀려도 <b>배지는 뜨는데 숫자만 빈다</b> — 예외는 나지 않는다.
        /// </summary>
        [Test]
        public void Prefab_DigestingBadge_HasRemainingLabel()
        {
            var label = _prefab.transform.Find("DigestingBadge/RemainingLabel");

            Assert.IsNotNull(label, "RemainingLabel 자식이 없다");
            Assert.IsNotNull(label.GetComponent<TMP_Text>(),
                             "RemainingLabel 이 TMP_Text 가 아니다 — 남은 초가 안 나온다");
            Assert.IsNotNull(label.GetComponent<TMP_Text>().font,
                             "폰트가 비었다 — 숫자가 두부가 된다");
        }

        /// <summary>
        /// 손님 위에 <b>상시 라벨을 두지 않는다</b> (M6.6). 유형은 실루엣이(M5), 자세한 값은
        /// 눌러 여는 창이(M6.5) 지므로, 셋을 다 띄우면 자리마다 글자 뭉치가 앉아 정작
        /// 포화도·소화가 안 읽힌다.
        ///
        /// <para>
        /// <b>«BandLabel 자식이 없다» 로 쓰지 않는다.</b> 이름을 바꿔 다시 넣으면 그대로
        /// 통과한다. 프리팹 전체의 라벨을 세어 <b>소화 배지 안의 숫자 하나뿐</b>임을 본다.
        /// </para>
        /// <para>
        /// 이 검사가 <c>CustomerViewTests</c> 가 아니라 여기 있는 이유: 그 하네스는 뷰를
        /// 자식 없이 세우므로 «라벨이 없다» 가 구현과 무관하게 늘 참이다
        /// (<c>.claude/rules/tests.md</c> §3).
        /// </para>
        /// </summary>
        [Test]
        public void Prefab_HasNoAlwaysOnLabel()
        {
            var labels = _prefab.GetComponentsInChildren<TMP_Text>(true);

            Assert.AreEqual(1, labels.Length,
                            "손님 위 라벨은 소화 배지의 숫자 하나뿐이어야 한다 — "
                            + "상시 표시할 글자를 다시 넣지 않는다");
            Assert.AreEqual("RemainingLabel", labels[0].name, "남은 라벨이 배지의 것이 아니다");
        }

        /// <summary>
        /// 포화도 칸은 <b>세로로</b> 쌓인다 (M6.6). 가로로 두면 손님 아래 테이블 위에 얹혀
        /// 자리마다 배경이 달라지고, 무엇보다 소화 배지와 같은 축을 두고 다툰다.
        ///
        /// <para>
        /// <b>«x 가 전부 같다» 만 보지 않는다.</b> 칸 여덟이 한 자리에 겹쳐 있어도 그것은
        /// 참이다 — <c>y</c> 가 서로 다르다는 것을 함께 본다.
        /// </para>
        /// </summary>
        [Test]
        public void Prefab_SaturationCells_AreStackedVertically()
        {
            var bar = _prefab.transform.Find("SaturationBar");
            var heights = new HashSet<float>();

            for (var i = 0; i < bar.childCount; i++)
            {
                Assert.AreEqual(bar.GetChild(0).localPosition.x, bar.GetChild(i).localPosition.x, 0.001f,
                                $"{bar.GetChild(i).name} 이 다른 칸과 다른 x 에 있다 — 세로바가 아니다");
                heights.Add(bar.GetChild(i).localPosition.y);
            }

            Assert.AreEqual(bar.childCount, heights.Count, "칸들이 같은 높이에 겹쳐 있다");
        }

        /// <summary>
        /// 게이지는 <b>아래에서 위로</b> 찬다. <c>SaturationBarView</c> 는 배열 순서대로
        /// 칠하므로, 칸 순서와 높이 순서가 어긋나면 <b>위에서부터 차오르거나 중간이 뛴다</b> —
        /// 예외는 나지 않고 화면에서만 드러난다.
        /// </summary>
        [Test]
        public void Prefab_SaturationCells_FillFromTheBottom()
        {
            var bar = _prefab.transform.Find("SaturationBar");

            for (var i = 1; i < bar.childCount; i++)
            {
                Assert.Greater(bar.GetChild(i).localPosition.y, bar.GetChild(i - 1).localPosition.y,
                               $"{bar.GetChild(i).name} 이 앞 칸보다 아래에 있다 — 게이지가 뒤집힌다");
            }
        }

        /// <summary>
        /// 바는 손님 <b>옆</b>에 선다. 아래에 두면 테이블 스프라이트 한복판에 얹힌다 —
        /// 테이블이 자리 기준 좌우 ±1 유닛, 아래로 −1.85 유닛을 덮는다.
        /// </summary>
        [Test]
        public void Prefab_SaturationBar_SitsBesideTheBody()
        {
            var bar = _prefab.transform.Find("SaturationBar");
            var body = _prefab.GetComponent<SpriteRenderer>();

            Assert.Greater(Mathf.Abs(bar.localPosition.x), body.bounds.extents.x,
                           "바가 몸통 폭 안에 있다 — 손님과 겹친다");
        }

        /// <summary>
        /// 칸마다 <b>배경판</b>이 붙어 있다. 뒤가 나무든 벽이든 금색 칸이 같게 읽히려면
        /// 위치가 아니라 배경으로 풀어야 한다 — 세로바는 테이블을 벗어날 수 없다.
        ///
        /// <para>
        /// <b>판은 «칸의 자식» 이다.</b> 바 밑에 형제로 두면 칸이 꺼져도 판이 남아,
        /// 최대 포화도가 작은 손님(소식가 3칸)에게 빈 판이 길게 붙는다 —
        /// <c>SaturationBarView</c> 가 칸 수를 손님마다 다르게 켜기 때문이다.
        /// </para>
        /// </summary>
        [Test]
        public void Prefab_EverySaturationCell_HasABacking()
        {
            var bar = _prefab.transform.Find("SaturationBar");

            for (var i = 0; i < bar.childCount; i++)
            {
                var cell = bar.GetChild(i);
                var backing = cell.Find("Backing");

                Assert.IsNotNull(backing, $"{cell.name} 에 배경판이 없다");
                Assert.IsNotNull(backing.GetComponent<SpriteRenderer>()?.sprite,
                                 $"{cell.name} 의 배경판에 그림이 없다 — 켜도 안 보인다");
            }
        }

        /// <summary>
        /// 배경판은 칸 <b>뒤</b>, 몸통 <b>앞</b>이다.
        ///
        /// <para>
        /// <b>세 층을 다 본다.</b> «판 &lt; 칸» 만 보면 판이 몸통 뒤로 숨어도 통과하는데,
        /// 그러면 화면에서 판이 통째로 사라진다.
        /// </para>
        /// </summary>
        [Test]
        public void Prefab_CellBacking_DrawsBehindItsCellButInFrontOfTheBody()
        {
            var bar = _prefab.transform.Find("SaturationBar");
            var body = _prefab.GetComponent<SpriteRenderer>();

            for (var i = 0; i < bar.childCount; i++)
            {
                var cell = bar.GetChild(i).GetComponent<SpriteRenderer>();
                var backing = bar.GetChild(i).Find("Backing").GetComponent<SpriteRenderer>();

                Assert.Less(backing.sortingOrder, cell.sortingOrder,
                            $"{bar.GetChild(i).name} 의 판이 칸을 덮는다");
                Assert.Greater(backing.sortingOrder, body.sortingOrder,
                               $"{bar.GetChild(i).name} 의 판이 몸통 뒤로 숨었다");
            }
        }
    }
}
