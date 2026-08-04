using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Data
{
    public sealed class CustomerDataTests
    {
        private CustomerData _data;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<CustomerData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void OnValidate_NegativeTargetingPrice_ClampsToZero()
        {
            SerializedFieldSetter.SetInt(_data, "_targetingPrice", -50);

            _data.OnValidate();

            Assert.AreEqual(0, _data.TargetingPrice);
        }

        [Test]
        public void OnValidate_ZeroMaxSaturation_ClampsToOne()
        {
            SerializedFieldSetter.SetInt(_data, "_maxSaturation", 0);

            _data.OnValidate();

            Assert.AreEqual(1, _data.MaxSaturation);
        }

        [Test]
        public void OnValidate_NegativeDigestSeconds_ClampsToZero()
        {
            SerializedFieldSetter.SetFloat(_data, "_digestSeconds", -2f);

            _data.OnValidate();

            Assert.AreEqual(0f, _data.DigestSeconds);
        }

        [Test]
        public void OnValidate_NegativeReach_ClampsToZero()
        {
            SerializedFieldSetter.SetFloat(_data, "_reach", -1.5f);

            _data.OnValidate();

            Assert.AreEqual(0f, _data.Reach);
        }

        [Test]
        public void OnValidate_NegativeRecruitCost_ClampsToZero()
        {
            SerializedFieldSetter.SetInt(_data, "_recruitCost", -7);

            _data.OnValidate();

            Assert.AreEqual(0, _data.RecruitCost);
        }

        [Test]
        public void OnValidate_TargetingPriceFarFromAnyPrice_IsNotRejected()
        {
            // 타겟팅은 선호도일 뿐 자격 게이트가 아니다 (CLAUDE.md §1.1-3a).
            // 어떤 값이든 유효하며, 검증이 이를 제한해서는 안 된다.
            SerializedFieldSetter.SetInt(_data, "_targetingPrice", 99999);

            _data.OnValidate();

            Assert.AreEqual(99999, _data.TargetingPrice);
        }
    }
}
