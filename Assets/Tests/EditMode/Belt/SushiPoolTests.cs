using System;
using NUnit.Framework;
using SushiDefense.Belt;

namespace SushiDefense.Tests.EditMode.Belt
{
    public sealed class SushiPoolTests
    {
        private FakeSushiInstanceFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new FakeSushiInstanceFactory();
        }

        [Test]
        public void Rent_EmptyPool_CreatesNewInstance()
        {
            var pool = new SushiPool<object>(_factory);

            var instance = pool.Rent();

            Assert.IsNotNull(instance);
            Assert.AreEqual(1, _factory.CreateCallCount);
        }

        [Test]
        public void Rent_AfterReturn_ReusesReturnedInstance()
        {
            var pool = new SushiPool<object>(_factory);
            var first = pool.Rent();
            pool.Return(first);

            var second = pool.Rent();

            Assert.AreSame(first, second);
        }

        [Test]
        public void Rent_AfterReturn_DoesNotCallFactoryAgain()
        {
            var pool = new SushiPool<object>(_factory);
            pool.Return(pool.Rent());

            pool.Rent();

            Assert.AreEqual(1, _factory.CreateCallCount);
        }

        [Test]
        public void Rent_Twice_ReturnsDistinctInstances()
        {
            var pool = new SushiPool<object>(_factory);

            var first = pool.Rent();
            var second = pool.Rent();

            Assert.AreNotSame(first, second);
            Assert.AreEqual(2, _factory.CreateCallCount);
        }

        [Test]
        public void Return_UnknownInstance_Throws()
        {
            var pool = new SushiPool<object>(_factory);

            Assert.Throws<ArgumentException>(() => pool.Return(new object()));
        }

        [Test]
        public void Return_SameInstanceTwice_Throws()
        {
            var pool = new SushiPool<object>(_factory);
            var instance = pool.Rent();
            pool.Return(instance);

            Assert.Throws<ArgumentException>(() => pool.Return(instance));
        }

        [Test]
        public void Return_Instance_DoesNotDisposeIt()
        {
            var pool = new SushiPool<object>(_factory);

            pool.Return(pool.Rent());

            Assert.IsEmpty(_factory.Disposed);
        }

        [Test]
        public void Prewarm_WithCount_CreatesThatManyUpFront()
        {
            var pool = new SushiPool<object>(_factory, 4);

            Assert.AreEqual(4, _factory.CreateCallCount);
            Assert.AreEqual(4, pool.CountInactive);
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void Rent_AfterPrewarm_DoesNotCreateMore()
        {
            var pool = new SushiPool<object>(_factory, 2);

            pool.Rent();
            pool.Rent();

            Assert.AreEqual(2, _factory.CreateCallCount);
        }

        [Test]
        public void Clear_AfterPrewarm_DisposesAll()
        {
            var pool = new SushiPool<object>(_factory, 3);

            pool.Clear();

            Assert.AreEqual(3, _factory.Disposed.Count);
            Assert.AreEqual(0, pool.CountAll);
        }

        [Test]
        public void Clear_WithRentedInstances_DisposesThoseToo()
        {
            var pool = new SushiPool<object>(_factory, 2);
            pool.Rent();

            pool.Clear();

            Assert.AreEqual(2, _factory.Disposed.Count);
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void CountActive_AfterRentAndReturn_TracksCorrectly()
        {
            var pool = new SushiPool<object>(_factory);

            var first = pool.Rent();
            pool.Rent();

            Assert.AreEqual(2, pool.CountActive);
            Assert.AreEqual(0, pool.CountInactive);
            Assert.AreEqual(2, pool.CountAll);

            pool.Return(first);

            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual(2, pool.CountAll);
        }

        [Test]
        public void Ctor_NullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SushiPool<object>(null));
        }

        [Test]
        public void Ctor_NegativePrewarm_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SushiPool<object>(_factory, -1));
        }
    }
}
