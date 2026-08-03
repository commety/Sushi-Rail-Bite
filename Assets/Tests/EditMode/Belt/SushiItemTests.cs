using NUnit.Framework;
using SushiDefense.Belt;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Belt
{
    public sealed class SushiItemTests
    {
        private const int AnyCustomer = 4;
        private const int OtherCustomer = 9;

        private SushiData _data;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<SushiData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void Ctor_NewItem_StartsOnBeltUnclaimed()
        {
            var item = new SushiItem(_data, 3);

            Assert.AreEqual(SushiState.OnBelt, item.State);
            Assert.AreEqual(3, item.SequenceNumber);
            Assert.AreEqual(SushiItem.NoCustomer, item.ClaimedByCustomerSequenceNumber);
        }

        [Test]
        public void TryClaim_OnBelt_TransitionsToClaimed()
        {
            var item = new SushiItem(_data, 0);

            var claimed = item.TryClaim(AnyCustomer);

            Assert.IsTrue(claimed);
            Assert.AreEqual(SushiState.Claimed, item.State);
            Assert.AreEqual(AnyCustomer, item.ClaimedByCustomerSequenceNumber);
        }

        [Test]
        public void TryClaim_AlreadyClaimed_ReturnsFalseAndKeepsState()
        {
            var item = new SushiItem(_data, 0);
            item.TryClaim(AnyCustomer);

            var claimedAgain = item.TryClaim(OtherCustomer);

            Assert.IsFalse(claimedAgain);
            Assert.AreEqual(AnyCustomer, item.ClaimedByCustomerSequenceNumber);
        }

        [Test]
        public void TryClaim_Consumed_ReturnsFalse()
        {
            var item = new SushiItem(_data, 0);
            item.TryClaim(AnyCustomer);
            item.TryConsume();

            var claimed = item.TryClaim(OtherCustomer);

            Assert.IsFalse(claimed);
            Assert.AreEqual(SushiState.Consumed, item.State);
        }

        [Test]
        public void TryConsume_Claimed_TransitionsToConsumed()
        {
            var item = new SushiItem(_data, 0);
            item.TryClaim(AnyCustomer);

            var consumed = item.TryConsume();

            Assert.IsTrue(consumed);
            Assert.AreEqual(SushiState.Consumed, item.State);
        }

        [Test]
        public void TryConsume_OnBelt_ReturnsFalse()
        {
            var item = new SushiItem(_data, 0);

            var consumed = item.TryConsume();

            Assert.IsFalse(consumed);
            Assert.AreEqual(SushiState.OnBelt, item.State);
        }

        [Test]
        public void ResetForReuse_ConsumedItem_ReturnsToOnBelt()
        {
            var item = new SushiItem(_data, 0);
            item.TryClaim(AnyCustomer);
            item.TryConsume();
            item.BeltPosition = 12.5f;

            item.ResetForReuse(_data, 7);

            Assert.AreEqual(SushiState.OnBelt, item.State);
            Assert.AreEqual(7, item.SequenceNumber);
            Assert.AreEqual(SushiItem.NoCustomer, item.ClaimedByCustomerSequenceNumber);
            Assert.AreEqual(0f, item.BeltPosition);
        }
    }
}
