using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using SushiDefense.Run;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.Run
{
    /// <summary>
    /// 손님 명부는 <c>SushiDeck</c> 과 같은 규칙으로 자란다 — 중복·<c>null</c> 을 거부하고
    /// 추가 순서를 유지한다.
    /// </summary>
    public sealed class CustomerDeckTests
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
        public void Members_NewDeck_IsEmptyNotNull()
        {
            var deck = new CustomerDeck();

            Assert.IsNotNull(deck.Members);
            Assert.IsEmpty(deck.Members);
            Assert.AreEqual(0, deck.Count);
        }

        [Test]
        public void TryAdd_NewMember_AddsAndReturnsTrue()
        {
            var deck = new CustomerDeck();
            var member = CreateCustomer();

            Assert.IsTrue(deck.TryAdd(member));

            Assert.AreEqual(1, deck.Count);
            Assert.IsTrue(deck.Contains(member));
        }

        [Test]
        public void TryAdd_DuplicateMember_ReturnsFalseAndKeepsCount()
        {
            var deck = new CustomerDeck();
            var member = CreateCustomer();
            deck.TryAdd(member);

            Assert.IsFalse(deck.TryAdd(member));

            Assert.AreEqual(1, deck.Count);
        }

        [Test]
        public void TryAdd_Null_ReturnsFalse()
        {
            var deck = new CustomerDeck();

            Assert.IsFalse(deck.TryAdd(null));

            Assert.AreEqual(0, deck.Count);
        }

        [Test]
        public void Contains_MemberNeverAdded_ReturnsFalse()
        {
            var deck = new CustomerDeck();
            deck.TryAdd(CreateCustomer());

            Assert.IsFalse(deck.Contains(CreateCustomer()));
        }

        [Test]
        public void Members_MultipleAdds_PreservesInsertionOrder()
        {
            var deck = new CustomerDeck();
            var first = CreateCustomer();
            var second = CreateCustomer();
            var third = CreateCustomer();

            deck.TryAdd(third);
            deck.TryAdd(first);
            deck.TryAdd(second);

            Assert.AreSame(third, deck.Members[0]);
            Assert.AreSame(first, deck.Members[1]);
            Assert.AreSame(second, deck.Members[2]);
        }

        [Test]
        public void Constructor_WithMembers_CopiesThemInOrder()
        {
            var first = CreateCustomer();
            var second = CreateCustomer();

            var deck = new CustomerDeck(new[] { first, second });

            Assert.AreEqual(2, deck.Count);
            Assert.AreSame(first, deck.Members[0]);
            Assert.AreSame(second, deck.Members[1]);
        }

        [Test]
        public void Constructor_WithDuplicatesAndNulls_KeepsOnlyDistinctMembers()
        {
            var member = CreateCustomer();

            var deck = new CustomerDeck(new[] { member, null, member });

            Assert.AreEqual(1, deck.Count);
            Assert.AreSame(member, deck.Members[0]);
        }

        private CustomerData CreateCustomer()
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(customer);
            return customer;
        }
    }
}
