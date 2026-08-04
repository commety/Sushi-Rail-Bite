using System;
using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Data
{
    public sealed class SushiEatenEventChannelTests
    {
        private SushiEatenEventChannelSO _channel;

        [SetUp]
        public void SetUp()
        {
            _channel = ScriptableObject.CreateInstance<SushiEatenEventChannelSO>();
        }

        [TearDown]
        public void TearDown()
        {
            _channel.ClearSubscribers();
            UnityEngine.Object.DestroyImmediate(_channel);
        }

        [Test]
        public void Raise_WithSubscriber_InvokesHandlerOnce()
        {
            var callCount = 0;
            _channel.OnRaised += _ => callCount++;

            _channel.Raise(new SushiEatenPayload(100, 1, 0, 0));

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Raise_AfterUnsubscribe_DoesNotInvokeHandler()
        {
            var callCount = 0;
            Action<SushiEatenPayload> handler = _ => callCount++;
            _channel.OnRaised += handler;
            _channel.OnRaised -= handler;

            _channel.Raise(new SushiEatenPayload(100, 1, 0, 0));

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Raise_NoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _channel.Raise(new SushiEatenPayload(100, 1, 0, 0)));
        }

        [Test]
        public void Raise_MultipleSubscribers_InvokesAll()
        {
            var first = 0;
            var second = 0;
            _channel.OnRaised += _ => first++;
            _channel.OnRaised += _ => second++;

            _channel.Raise(new SushiEatenPayload(100, 1, 0, 0));

            Assert.AreEqual(1, first);
            Assert.AreEqual(1, second);
        }

        [Test]
        public void Raise_PassesPayloadUnchanged()
        {
            var received = default(SushiEatenPayload);
            _channel.OnRaised += payload => received = payload;

            _channel.Raise(new SushiEatenPayload(250, 3, 7, 2));

            Assert.AreEqual(250, received.Price);
            Assert.AreEqual(3, received.SaturationAmount);
            Assert.AreEqual(7, received.SushiSequenceNumber);
            Assert.AreEqual(2, received.CustomerSequenceNumber);
        }

        [Test]
        public void ClearSubscribers_ThenRaise_DoesNotInvokeHandler()
        {
            var callCount = 0;
            _channel.OnRaised += _ => callCount++;

            _channel.ClearSubscribers();
            _channel.Raise(new SushiEatenPayload(100, 1, 0, 0));

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Raise_TwiceWithSameSubscriber_InvokesTwice()
        {
            var callCount = 0;
            _channel.OnRaised += _ => callCount++;

            _channel.Raise(new SushiEatenPayload(100, 1, 0, 0));
            _channel.Raise(new SushiEatenPayload(100, 1, 1, 0));

            Assert.AreEqual(2, callCount);
        }
    }
}
