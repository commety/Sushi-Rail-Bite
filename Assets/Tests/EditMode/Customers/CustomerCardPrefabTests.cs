using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.UI;
using UnityEditor;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Customers
{
    /// <summary>
    /// 손님 카드는 <c>Card.prefab</c> 의 <b>배리언트</b>다 — 틀은 그대로 두고 끌기만 얹는다
    /// (README D3).
    ///
    /// <para>
    /// <c>CustomerHandViewTests</c> 는 카드를 코드로 세우므로 이 애셋이 어떻게 생겼든
    /// 초록이다. 배리언트가 원본과의 연결을 잃거나 끌기 컴포넌트가 빠져도 조용하고,
    /// 증상은 <b>재생해서 끌어 봐야</b> 드러난다
    /// (<c>.claude/rules/tests.md</c> §1 «애셋 등록·설정»).
    /// </para>
    /// </summary>
    public sealed class CustomerCardPrefabTests
    {
        private const string VariantPath =
            "Assets/Code/Scripts/Presentation/Customers/CustomerCard.prefab";

        private const string BasePath = "Assets/Code/Scripts/Presentation/UI/Card.prefab";

        private GameObject _variant;

        [SetUp]
        public void SetUp()
        {
            _variant = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
        }

        [Test]
        public void Variant_Exists()
        {
            Assert.IsNotNull(_variant, $"{VariantPath} 를 로드하지 못했다");
        }

        /// <summary>
        /// <b>배리언트여야 한다.</b> 복사본이 되면 카드 틀을 고쳐도 손님 카드만 옛 모양으로
        /// 남고, 그 차이는 화면을 나란히 놓고 봐야 보인다.
        /// </summary>
        [Test]
        public void Variant_InheritsCardPrefab()
        {
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(_variant));

            var source = PrefabUtility.GetCorrespondingObjectFromSource(_variant);
            Assert.AreEqual(BasePath, AssetDatabase.GetAssetPath(source));
        }

        [Test]
        public void Variant_HasDragHandler()
        {
            Assert.IsNotNull(_variant.GetComponent<CustomerCardDrag>());
        }

        /// <summary>틀에서 물려받은 것이 살아 있어야 카드가 그려진다.</summary>
        [Test]
        public void Variant_KeepsCardView()
        {
            Assert.IsNotNull(_variant.GetComponent<CardView>());
        }

        /// <summary>
        /// 놓을 수 없는 카드를 흐리게 만드는 장치다. 없으면 <c>SetAvailable</c> 이 조용히
        /// 아무것도 하지 않아, 잔액이 모자라도 카드가 멀쩡해 보인다.
        /// </summary>
        [Test]
        public void Variant_HasCanvasGroup()
        {
            Assert.IsNotNull(_variant.GetComponent<CanvasGroup>());
        }

        /// <summary>
        /// <b>여기서만 크기를 단언한다.</b> 다른 곳에서 레이아웃 값을 박지 않는 것은 그것이
        /// 사람이 만질 연출 수치이기 때문인데, 이 테스트가 보는 것은 값 자체가 아니라
        /// <b>배리언트가 틀과 어긋났는가</b>다.
        ///
        /// <para>
        /// 배리언트는 크기를 <b>오버라이드로</b> 들고 있어서, 틀을 128×192 로 키워도 손패만
        /// 옛 크기로 남는다. 그러면 같은 카드가 화면마다 다른 크기로 나오는데 — 카드 틀을
        /// 하나로 유지하는 이유가 정확히 그것을 막는 것이다 (M6 D3).
        /// </para>
        /// </summary>
        [Test]
        public void Variant_RootSize_MatchesTheCardPrefab()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath);

            Assert.AreEqual(((RectTransform)basePrefab.transform).sizeDelta,
                            ((RectTransform)_variant.transform).sizeDelta,
                            "손패 카드만 옛 크기로 남았다");
        }

        /// <summary>틀에서 물려받은 비용 자리도 살아 있어야 한다.</summary>
        [Test]
        public void Variant_KeepsCostLabelAndCoin()
        {
            Assert.IsNotNull(_variant.transform.Find("CostLabel"), "CostLabel 이 없다");
            Assert.IsNotNull(_variant.transform.Find("Coin"), "Coin 이 없다");
        }
    }
}
