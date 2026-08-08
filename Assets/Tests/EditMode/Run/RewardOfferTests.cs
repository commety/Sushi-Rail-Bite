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
