using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.Data
{
    /// <summary>
    /// <see cref="CardCatalog"/> 는 <b>목록을 순서대로 들고 있기만</b> 한다.
    ///
    /// <para>
    /// 비어 있는 것도, 빈 슬롯이 섞이는 것도 오류가 아니다 —
    /// <c>RewardCatalog</c> 가 이미 내린 판단과 같다
    /// (<c>.claude/rules/scriptable-object.md</c> §6). 이 파일이 그 경계를 고정한다.
    /// </para>
    /// </summary>
    public sealed class CardCatalogTests
    {
        private CardCatalog _catalog;
        private List<Object> _disposables;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<CardCatalog>();
            _disposables = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            for (var i = 0; i < _disposables.Count; i++)
            {
                Object.DestroyImmediate(_disposables[i]);
            }
        }

        [Test]
        public void AllSushi_FreshCatalog_IsEmptyNotNull()
        {
            Assert.IsNotNull(_catalog.AllSushi);
            Assert.IsEmpty(_catalog.AllSushi);
        }

        [Test]
        public void AllCustomers_FreshCatalog_IsEmptyNotNull()
        {
            Assert.IsNotNull(_catalog.AllCustomers);
            Assert.IsEmpty(_catalog.AllCustomers);
        }

        /// <summary>
        /// 사전은 목록 순서대로 그려진다. 순서가 흔들리면 같은 화면이 열 때마다 달라 보인다.
        /// </summary>
        [Test]
        public void AllSushi_PopulatedByInspector_PreservesOrder()
        {
            var first = CreateSushi();
            var second = CreateSushi();
            var third = CreateSushi();

            AppendSushi(first);
            AppendSushi(second);
            AppendSushi(third);

            Assert.AreEqual(3, _catalog.AllSushi.Count);
            Assert.AreSame(first, _catalog.AllSushi[0]);
            Assert.AreSame(second, _catalog.AllSushi[1]);
            Assert.AreSame(third, _catalog.AllSushi[2]);
        }

        [Test]
        public void AllCustomers_PopulatedByInspector_PreservesOrder()
        {
            var first = CreateCustomer();
            var second = CreateCustomer();

            AppendCustomer(first);
            AppendCustomer(second);

            Assert.AreEqual(2, _catalog.AllCustomers.Count);
            Assert.AreSame(first, _catalog.AllCustomers[0]);
            Assert.AreSame(second, _catalog.AllCustomers[1]);
        }

        /// <summary>
        /// 빈 슬롯은 인스펙터에서 목록을 늘리면 자연히 생긴다. 거르는 책임은 읽는 쪽에 있으므로
        /// 스키마는 <c>null</c> 을 그대로 담는다.
        /// </summary>
        [Test]
        public void AllSushi_NullEntry_IsKeptNotRejected()
        {
            AppendSushi(null);

            Assert.AreEqual(1, _catalog.AllSushi.Count);
            Assert.IsNull(_catalog.AllSushi[0]);
        }

        private SushiData CreateSushi()
        {
            var sushi = ScriptableObject.CreateInstance<SushiData>();
            _disposables.Add(sushi);
            return sushi;
        }

        private CustomerData CreateCustomer()
        {
            var customer = ScriptableObject.CreateInstance<CustomerData>();
            _disposables.Add(customer);
            return customer;
        }

        private void AppendSushi(SushiData sushi)
        {
            SerializedFieldSetter.AppendObject(_catalog, "_allSushi", sushi);
        }

        private void AppendCustomer(CustomerData customer)
        {
            SerializedFieldSetter.AppendObject(_catalog, "_allCustomers", customer);
        }
    }
}
