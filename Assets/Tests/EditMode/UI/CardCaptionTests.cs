using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Tests.EditMode.Data;
using SushiDefense.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 카드에 쓸 문구를 만드는 순수 계산. <b>뷰 없이 검증된다</b> — 뷰를 세워야만 확인되는
    /// 문구라면 이 클래스가 제 역할을 못 하는 것이다.
    ///
    /// <para>
    /// 이름 폴백(<i>표시 이름이 비면 애셋 이름</i>)이 M5 까지 세 곳에 흩어져 있었다.
    /// 카드가 네 번째가 되지 않도록 여기 한 벌로 모았으므로, 그 규칙의 테스트도 여기 있다.
    /// </para>
    /// </summary>
    public sealed class CardCaptionTests
    {
        private SushiData _sushi;
        private CustomerData _customer;

        [SetUp]
        public void SetUp()
        {
            _sushi = ScriptableObject.CreateInstance<SushiData>();
            _sushi.name = "Sushi.Tuna";
            _customer = ScriptableObject.CreateInstance<CustomerData>();
            _customer.name = "Customer.Standard";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sushi);
            Object.DestroyImmediate(_customer);
        }

        [Test]
        public void NameOf_Sushi_UsesDisplayName()
        {
            SerializedFieldSetter.SetString(_sushi, "_displayName", "참치");

            Assert.AreEqual("참치", CardCaption.NameOf(_sushi));
        }

        /// <summary>
        /// 아직 이름을 안 채운 애셋에서 빈 칸이 그려지면 카드가 고장 난 것처럼 보인다.
        /// </summary>
        [Test]
        public void NameOf_EmptyDisplayName_FallsBackToAssetName()
        {
            SerializedFieldSetter.SetString(_sushi, "_displayName", string.Empty);

            Assert.AreEqual("Sushi.Tuna", CardCaption.NameOf(_sushi));
        }

        /// <summary>
        /// 공백만 든 이름도 빈 것이다. <c>== null</c> 로 판단하는 구현을 배제한다.
        /// </summary>
        [Test]
        public void NameOf_WhitespaceDisplayName_FallsBackToAssetName()
        {
            SerializedFieldSetter.SetString(_customer, "_displayName", "   ");

            Assert.AreEqual("Customer.Standard", CardCaption.NameOf(_customer));
        }

        [Test]
        public void NameOf_Customer_UsesDisplayName()
        {
            SerializedFieldSetter.SetString(_customer, "_displayName", "기본");

            Assert.AreEqual("기본", CardCaption.NameOf(_customer));
        }

        [Test]
        public void NameOf_Null_ReturnsEmpty()
        {
            Assert.IsEmpty(CardCaption.NameOf((SushiData)null));
            Assert.IsEmpty(CardCaption.NameOf((CustomerData)null));
        }

        /// <summary>
        /// 초밥 카드가 답해야 할 것은 <b>얼마짜리인가</b>와 <b>얼마나 배부른가</b>다.
        /// 가격은 이 게임의 핵심 규칙(대역)의 입력이고, 포화도는 몇 개나 먹힐지를 정한다.
        /// </summary>
        [Test]
        public void DetailOf_Sushi_ContainsPriceAndSaturation()
        {
            SerializedFieldSetter.SetInt(_sushi, "_price", 250);
            SerializedFieldSetter.SetInt(_sushi, "_saturationAmount", 2);

            var detail = CardCaption.DetailOf(_sushi);

            StringAssert.Contains("250", detail);
            StringAssert.Contains("2", detail);
        }

        /// <summary>
        /// 손님 카드가 답해야 할 것은 <b>무엇을 노리는가</b>와 <b>얼마나 드는가</b>다.
        /// 비용을 모르면 얻고 나서 예산이 모자라 못 앉히는 상황을 고르는 시점에 예측할 수 없다.
        /// </summary>
        [Test]
        public void DetailOf_Customer_ContainsBandAndRecruitCost()
        {
            SerializedFieldSetter.SetTargetingBand(_customer, 100, 300);
            SerializedFieldSetter.SetInt(_customer, "_recruitCost", 20);

            var detail = CardCaption.DetailOf(_customer);

            StringAssert.Contains("100", detail);
            StringAssert.Contains("300", detail);
            StringAssert.Contains("20", detail);
        }

        /// <summary>
        /// 대역 하한과 상한을 <b>구분해</b> 적어야 한다. 한쪽만 쓰면 좁은 손님과 넓은 손님이
        /// 카드에서 같아 보인다.
        /// </summary>
        [Test]
        public void DetailOf_NarrowBand_DiffersFromWideBand()
        {
            SerializedFieldSetter.SetTargetingBand(_customer, 250, 320);
            SerializedFieldSetter.SetInt(_customer, "_recruitCost", 40);
            var narrow = CardCaption.DetailOf(_customer);

            SerializedFieldSetter.SetTargetingBand(_customer, 100, 300);
            var wide = CardCaption.DetailOf(_customer);

            Assert.AreNotEqual(narrow, wide);
        }

        [Test]
        public void DetailOf_Null_ReturnsEmpty()
        {
            Assert.IsEmpty(CardCaption.DetailOf((SushiData)null));
            Assert.IsEmpty(CardCaption.DetailOf((CustomerData)null));
        }
    }
}
