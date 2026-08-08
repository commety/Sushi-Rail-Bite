using NUnit.Framework;
using SushiDefense.Customers;
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
    }
}
