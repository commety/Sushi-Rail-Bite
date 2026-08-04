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

        [Test]
        public void OnValidate_ZeroSpawnInterval_ClampsToPositive()
        {
            SerializedFieldSetter.SetFloat(_config, "_spawnIntervalSeconds", 0f);

            _config.OnValidate();

            Assert.That(_config.SpawnIntervalSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void OnValidate_NegativeSpawnInterval_ClampsToPositive()
        {
            SerializedFieldSetter.SetFloat(_config, "_spawnIntervalSeconds", -2f);

            _config.OnValidate();

            Assert.That(_config.SpawnIntervalSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void OnValidate_ZeroBeltLength_ClampsToPositive()
        {
            SerializedFieldSetter.SetFloat(_config, "_beltLength", 0f);

            _config.OnValidate();

            Assert.That(_config.BeltLength, Is.GreaterThan(0f));
        }

        [Test]
        public void OnValidate_NegativeRecognitionLatch_ClampsToZero()
        {
            SerializedFieldSetter.SetFloat(_config, "_recognitionLatchSeconds", -3f);

            _config.OnValidate();

            Assert.AreEqual(0f, _config.RecognitionLatchSeconds);
        }

        [Test]
        public void RecognitionLatchSeconds_Zero_MeansNoCap()
        {
            // 0 은 "상한 없음" 규약이다. 클램프가 0 을 양수로 밀어 올리면
            // 상한을 끌 방법이 사라진다 — 그 규약을 여기서 고정한다.
            SerializedFieldSetter.SetFloat(_config, "_recognitionLatchSeconds", 0f);

            _config.OnValidate();

            Assert.AreEqual(0f, _config.RecognitionLatchSeconds);
        }
    }
}
