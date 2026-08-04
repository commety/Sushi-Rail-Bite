using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Data
{
    public sealed class SushiDataTests
    {
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
        public void Price_FreshData_DefaultsToMinimum()
        {
            // 새로 만든 애셋이 곧바로 유효해야 한다. 0 에서 시작하면 사람이 값을 채우기 전까지
            // 영입 재화가 한 푼도 안 붙는 초밥이 되어 경제가 조용히 죽는다.
            Assert.AreEqual(100, _data.Price);
        }

        [Test]
        public void OnValidate_NegativePrice_ClampsToMinimum()
        {
            SerializedFieldSetter.SetInt(_data, "_price", -10);

            _data.OnValidate();

            Assert.AreEqual(100, _data.Price);
        }

        [Test]
        public void OnValidate_PriceBelowMinimum_ClampsToHundred()
        {
            // 가격은 엔 단위이며 100 미만은 오류다 (착수 시 사람 판단).
            // 이 하한이 있어야 영입 재화의 정수 나눗셈(가격/10)이 잔여분을 잃지 않는다.
            SerializedFieldSetter.SetInt(_data, "_price", 50);

            _data.OnValidate();

            Assert.AreEqual(100, _data.Price);
        }

        [Test]
        public void OnValidate_PriceAtMinimum_Kept()
        {
            SerializedFieldSetter.SetInt(_data, "_price", 100);

            _data.OnValidate();

            Assert.AreEqual(100, _data.Price);
        }

        [Test]
        public void OnValidate_NegativeSaturationAmount_ClampsToZero()
        {
            SerializedFieldSetter.SetInt(_data, "_saturationAmount", -3);

            _data.OnValidate();

            Assert.AreEqual(0, _data.SaturationAmount);
        }

        [Test]
        public void OnValidate_PositivePrice_LeavesValueUntouched()
        {
            SerializedFieldSetter.SetInt(_data, "_price", 120);

            _data.OnValidate();

            Assert.AreEqual(120, _data.Price);
        }

        [Test]
        public void Trait_Default_IsNone()
        {
            Assert.AreEqual(SushiTrait.None, _data.Trait);
        }
    }
}
