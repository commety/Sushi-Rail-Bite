using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Data
{
    public sealed class StageConfigTests
    {
        private StageConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<StageConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void OnValidate_ZeroTargetRevenue_ClampsToOne()
        {
            SerializedFieldSetter.SetInt(_config, "_targetRevenue", 0);

            _config.OnValidate();

            Assert.AreEqual(1, _config.TargetRevenue);
        }

        [Test]
        public void OnValidate_ZeroTimeLimit_ClampsToPositive()
        {
            SerializedFieldSetter.SetFloat(_config, "_timeLimitSeconds", 0f);

            _config.OnValidate();

            Assert.That(_config.TimeLimitSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void OnValidate_ZeroMaxPlacedCustomers_ClampsToOne()
        {
            SerializedFieldSetter.SetInt(_config, "_maxPlacedCustomers", 0);

            _config.OnValidate();

            Assert.AreEqual(1, _config.MaxPlacedCustomers);
        }

        [Test]
        public void OnValidate_NegativeInitialBudget_ClampsToZero()
        {
            SerializedFieldSetter.SetInt(_config, "_initialRecruitBudget", -100);

            _config.OnValidate();

            Assert.AreEqual(0, _config.InitialRecruitBudget);
        }

        [Test]
        public void OnValidate_NegativeBeltSpeed_ClampsToZero()
        {
            SerializedFieldSetter.SetFloat(_config, "_beltSpeed", -4f);

            _config.OnValidate();

            Assert.AreEqual(0f, _config.BeltSpeed);
        }

        [Test]
        public void TableSlots_FreshConfig_IsEmptyNotNull()
        {
            Assert.IsNotNull(_config.TableSlots);
            Assert.IsEmpty(_config.TableSlots);
        }

        [Test]
        public void SpawnTable_FreshConfig_IsEmptyNotNull()
        {
            Assert.IsNotNull(_config.SpawnTable);
            Assert.IsEmpty(_config.SpawnTable);
        }
    }
}
