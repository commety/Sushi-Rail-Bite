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
    }
}
