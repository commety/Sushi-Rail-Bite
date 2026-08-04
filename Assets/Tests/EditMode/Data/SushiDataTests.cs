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
        public void OnValidate_NegativePrice_ClampsToZero()
        {
            SerializedFieldSetter.SetInt(_data, "_price", -10);

            _data.OnValidate();

            Assert.AreEqual(0, _data.Price);
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
