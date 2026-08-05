using NUnit.Framework;
using SushiDefense.Customers;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Customers
{
    public sealed class CustomerRuntimeStateTests
    {
        private CustomerData _data;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<CustomerData>();
            SetMaxSaturation(3);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void Ctor_NewCustomer_StartsIdleWithZeroSaturation()
        {
            var state = new CustomerRuntimeState(_data, 2);

            Assert.AreEqual(CustomerState.Idle, state.State);
            Assert.AreEqual(0, state.CurrentSaturation);
            Assert.AreEqual(2, state.SequenceNumber);
        }

        [Test]
        public void HasSaturationHeadroom_Empty_ReturnsTrue()
        {
            var state = new CustomerRuntimeState(_data, 0);

            Assert.IsTrue(state.HasSaturationHeadroom);
        }

        [Test]
        public void HasSaturationHeadroom_BelowMax_ReturnsTrue()
        {
            var state = new CustomerRuntimeState(_data, 0) { CurrentSaturation = 2 };

            Assert.IsTrue(state.HasSaturationHeadroom);
        }

        [Test]
        public void HasSaturationHeadroom_AtMax_ReturnsFalse()
        {
            var state = new CustomerRuntimeState(_data, 0) { CurrentSaturation = 3 };

            Assert.IsFalse(state.HasSaturationHeadroom);
        }

        [Test]
        public void HasSaturationHeadroom_TargetingBandIrrelevant_StaysTrue()
        {
            // 자격은 범위·포화도·상태만 본다. 가격은 자격에 끼지 않는다 (CLAUDE.md §1.1-3a).
            // 덱에 없는 대역이어도 자격은 그대로다 — 대역은 무엇을·언제만 정한다.
            SetTargetingBand(99998, 99999);
            var state = new CustomerRuntimeState(_data, 0);

            Assert.IsTrue(state.HasSaturationHeadroom);
        }

        private void SetMaxSaturation(int value)
        {
            SushiDefense.Tests.EditMode.Data.SerializedFieldSetter.SetInt(_data, "_maxSaturation", value);
        }

        private void SetTargetingBand(int min, int max)
        {
            SushiDefense.Tests.EditMode.Data.SerializedFieldSetter.SetTargetingBand(_data, min, max);
        }
    }
}
