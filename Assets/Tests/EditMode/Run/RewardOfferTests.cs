using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Tests.EditMode.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.Run
{
    /// <summary>
    /// 제시된 보상 한 장. <b>종류가 어느 칸이 채워졌는지를 말한다</b> — 둘 다 채워지거나
    /// 둘 다 비는 상태가 없어야 화면이 분기 없이 그릴 수 있다.
    /// </summary>
    public sealed class RewardOfferTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
            {
                Object.DestroyImmediate(disposable);
            }

            _disposables.Clear();
        }

        [Test]
        public void OfSushi_Built_KindIsSushiCardAndCustomerIsNull()
        {
            var sushi = CreateSushi("연어");

            var offer = RewardOffer.OfSushi(sushi);

            Assert.AreEqual(RewardKind.SushiCard, offer.Kind);
            Assert.AreSame(sushi, offer.Sushi);
            Assert.IsNull(offer.Customer);
        }

        [Test]
        public void OfCustomer_Built_KindIsCustomerAndSushiIsNull()
        {
            var customer = CreateCustomer("소식좌");

            var offer = RewardOffer.OfCustomer(customer);

            Assert.AreEqual(RewardKind.Customer, offer.Kind);
            Assert.AreSame(customer, offer.Customer);
            Assert.IsNull(offer.Sushi);
        }

        [Test]
        public void DisplayName_SushiWithName_UsesIt()
        {
            var offer = RewardOffer.OfSushi(CreateSushi("장어"));

            Assert.AreEqual("장어", offer.DisplayName);
        }

        [Test]
        public void DisplayName_CustomerWithName_UsesIt()
        {
            var offer = RewardOffer.OfCustomer(CreateCustomer("먹보", recruitCost: 60));

            StringAssert.Contains("먹보", offer.DisplayName);
        }

        /// <summary>
        /// 표시 이름은 비어 있을 수 있다 (아직 안 채운 애셋). 그때 빈 문자열을 그리면
        /// 보상 화면에 아무것도 안 보이므로 애셋 이름으로 대신한다.
        /// </summary>
        /// <summary>
        /// 손님 보상에는 <b>영입 비용이 함께</b> 보여야 한다. 유형마다 비용이 다르므로
        /// (기본 20 · 소식 40 · 먹보 60), 값을 모르면 "얻고 나서 예산이 모자라 못 앉히는"
        /// 상황을 고르는 시점에 예측할 수 없다.
        /// </summary>
        [Test]
        public void DisplayName_Customer_IncludesRecruitCost()
        {
            var offer = RewardOffer.OfCustomer(CreateCustomer("먹보", recruitCost: 60));

            StringAssert.Contains("60", offer.DisplayName);
        }

        /// <summary>초밥에는 붙이지 않는다 — 덱에 들어갈 뿐 비용이 없다.</summary>
        [Test]
        public void DisplayName_Sushi_HasNoRecruitCost()
        {
            var offer = RewardOffer.OfSushi(CreateSushi("장어"));

            Assert.AreEqual("장어", offer.DisplayName);
        }

        [Test]
        public void DisplayName_SushiWithBlankName_FallsBackToAssetName()
        {
            var sushi = CreateSushi(string.Empty);
            sushi.name = "Sushi.Unnamed";

            var offer = RewardOffer.OfSushi(sushi);

            Assert.AreEqual("Sushi.Unnamed", offer.DisplayName);
        }

        [Test]
        public void DisplayName_CustomerWithBlankName_FallsBackToAssetName()
        {
            var customer = CreateCustomer(null);
            customer.name = "Customer.Unnamed";

            var offer = RewardOffer.OfCustomer(customer);

            StringAssert.Contains("Customer.Unnamed", offer.DisplayName);
        }

        private SushiData CreateSushi(string displayName)
        {
            var sushi = StageConfigBuilder.CreateSushi(_disposables);
            SerializedFieldSetter.SetString(sushi, "_displayName", displayName);
            return sushi;
        }

        private CustomerData CreateCustomer(string displayName, int recruitCost = 0)
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(customer);
            SerializedFieldSetter.SetString(customer, "_displayName", displayName);
            SerializedFieldSetter.SetInt(customer, "_recruitCost", recruitCost);
            return customer;
        }
    }
}
