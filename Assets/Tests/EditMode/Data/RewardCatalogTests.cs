using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SushiDefense.Tests.EditMode.Data
{
    /// <summary>
    /// <see cref="RewardCatalog"/> 는 <b>구조 불변식만</b> 검증한다.
    ///
    /// <para>
    /// 풀이 비어 있는 것도, 제시 개수가 풀보다 큰 것도 오류가 아니라 기획이다
    /// (<c>.claude/rules/scriptable-object.md</c> §6). 이 파일이 그 경계를 고정한다.
    /// </para>
    /// </summary>
    public sealed class RewardCatalogTests
    {
        private RewardCatalog _catalog;
        private List<Object> _disposables;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<RewardCatalog>();
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
        public void OfferCount_Zero_ClampsToOne()
        {
            SerializedFieldSetter.SetInt(_catalog, "_offerCount", 0);

            _catalog.OnValidate();

            Assert.AreEqual(1, _catalog.OfferCount);
        }

        [Test]
        public void OfferCount_Negative_ClampsToOne()
        {
            SerializedFieldSetter.SetInt(_catalog, "_offerCount", -3);

            _catalog.OnValidate();

            Assert.AreEqual(1, _catalog.OfferCount);
        }

        [Test]
        public void OfferCount_AlreadyPositive_IsLeftAlone()
        {
            SerializedFieldSetter.SetInt(_catalog, "_offerCount", 4);

            _catalog.OnValidate();

            Assert.AreEqual(4, _catalog.OfferCount);
        }

        [Test]
        public void SushiPool_FreshCatalog_IsEmptyNotNull()
        {
            Assert.IsNotNull(_catalog.SushiPool);
            Assert.IsEmpty(_catalog.SushiPool);
        }

        [Test]
        public void CustomerPool_FreshCatalog_IsEmptyNotNull()
        {
            Assert.IsNotNull(_catalog.CustomerPool);
            Assert.IsEmpty(_catalog.CustomerPool);
        }

        [Test]
        public void SushiPool_PopulatedByInspector_PreservesOrder()
        {
            var first = CreateSushi();
            var second = CreateSushi();
            var third = CreateSushi();

            AppendSushi(first);
            AppendSushi(second);
            AppendSushi(third);

            Assert.AreEqual(3, _catalog.SushiPool.Count);
            Assert.AreSame(first, _catalog.SushiPool[0]);
            Assert.AreSame(second, _catalog.SushiPool[1]);
            Assert.AreSame(third, _catalog.SushiPool[2]);
        }

        [Test]
        public void CustomerPool_PopulatedByInspector_PreservesOrder()
        {
            var first = CreateCustomer();
            var second = CreateCustomer();
            var third = CreateCustomer();

            AppendCustomer(first);
            AppendCustomer(second);
            AppendCustomer(third);

            Assert.AreEqual(3, _catalog.CustomerPool.Count);
            Assert.AreSame(first, _catalog.CustomerPool[0]);
            Assert.AreSame(second, _catalog.CustomerPool[1]);
            Assert.AreSame(third, _catalog.CustomerPool[2]);
        }

        /// <summary>
        /// 풀보다 큰 제시 개수는 <b>오류가 아니다</b>. 있는 만큼 제시하는 것이 정상 동작이고,
        /// 그 판단은 생성기(<c>Runtime</c>)의 몫이다 — 여기서 경고하면 밸런스를 검증에 섞게 된다.
        /// </summary>
        [Test]
        public void OfferCount_LargerThanPool_IsNotAnError()
        {
            AppendSushi(CreateSushi());
            SerializedFieldSetter.SetInt(_catalog, "_offerCount", 5);

            _catalog.OnValidate();

            Assert.AreEqual(5, _catalog.OfferCount);
            Assert.AreEqual(1, _catalog.SushiPool.Count);
        }

        /// <summary>
        /// 빈 슬롯은 인스펙터에서 목록을 늘리면 자연히 생긴다. 거르는 책임은 생성기에 있으므로
        /// 스키마는 <c>null</c> 을 그대로 담는다.
        /// </summary>
        [Test]
        public void SushiPool_NullEntry_IsKeptNotRejected()
        {
            AppendSushi(null);

            _catalog.OnValidate();

            Assert.AreEqual(1, _catalog.SushiPool.Count);
            Assert.IsNull(_catalog.SushiPool[0]);
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
            SerializedFieldSetter.AppendObject(_catalog, "_sushiPool", sushi);
        }

        private void AppendCustomer(CustomerData customer)
        {
            SerializedFieldSetter.AppendObject(_catalog, "_customerPool", customer);
        }
    }
}
