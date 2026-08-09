using NUnit.Framework;
using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Data
{
    public sealed class CustomerDataTests
    {
        private CustomerData _data;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<CustomerData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void OnValidate_NegativeTargetingMin_ClampsToZero()
        {
            SerializedFieldSetter.SetTargetingBand(_data, -50, 300);

            _data.OnValidate();

            Assert.AreEqual(0, _data.TargetingMin);
        }

        [Test]
        public void OnValidate_ZeroMaxSaturation_ClampsToOne()
        {
            SerializedFieldSetter.SetInt(_data, "_maxSaturation", 0);

            _data.OnValidate();

            Assert.AreEqual(1, _data.MaxSaturation);
        }

        [Test]
        public void OnValidate_NegativeDigestSeconds_ClampsToZero()
        {
            SerializedFieldSetter.SetFloat(_data, "_digestSeconds", -2f);

            _data.OnValidate();

            Assert.AreEqual(0f, _data.DigestSeconds);
        }

        [Test]
        public void OnValidate_NegativeReach_ClampsToZero()
        {
            SerializedFieldSetter.SetFloat(_data, "_reach", -1.5f);

            _data.OnValidate();

            Assert.AreEqual(0f, _data.Reach);
        }

        [Test]
        public void OnValidate_NegativeRecruitCost_ClampsToZero()
        {
            SerializedFieldSetter.SetInt(_data, "_recruitCost", -7);

            _data.OnValidate();

            Assert.AreEqual(0, _data.RecruitCost);
        }

        [Test]
        public void OnValidate_PopulationBelowOne_ClampsToOne()
        {
            // 0 이면 배치 한도가 이 손님을 세지 않아, 한 자리에 무한히 앉힐 수 있게 된다.
            // 밸런스가 아니라 구조 붕괴라 검증 대상이다.
            SerializedFieldSetter.SetInt(_data, "_population", 0);

            _data.OnValidate();

            Assert.AreEqual(1, _data.Population);
        }

        [Test]
        public void Population_Untouched_IsOne()
        {
            // 값을 넣은 적 없는 손님이 «0 명» 으로 읽히면 한도가 통째로 무력화된다.
            // 애셋에 키가 없을 때 살아남는 것이 이 초기값이므로, 클램프와 따로 못박는다.
            Assert.AreEqual(1, _data.Population);
        }

        [Test]
        public void OnValidate_PopulationTwo_Kept()
        {
            // 반례. 클램프가 «항상 1» 로 뭉개면 먹보가 사라진다.
            SerializedFieldSetter.SetInt(_data, "_population", 2);

            _data.OnValidate();

            Assert.AreEqual(2, _data.Population);
        }

        [Test]
        public void OnValidate_MaxBelowMin_RaisesMaxToMin()
        {
            // min ≤ max 가 대역의 유일한 구조 불변식이다. 뒤집힌 대역은 "안" 이 공집합이라
            // 모든 초밥이 대역 밖이 되고, 폭이 음수라 정렬 키 4가 뒤집힌다.
            SerializedFieldSetter.SetTargetingBand(_data, 300, 100);

            _data.OnValidate();

            Assert.AreEqual(300, _data.TargetingMin);
            Assert.AreEqual(300, _data.TargetingMax, "상한을 하한까지 끌어올린다");
        }

        [Test]
        public void OnValidate_MaxEqualsMin_Kept()
        {
            // 폭 0 = 완전 전문가. 유효한 설정이므로 걸러내지 않는다.
            SerializedFieldSetter.SetTargetingBand(_data, 250, 250);

            _data.OnValidate();

            Assert.AreEqual(250, _data.TargetingMin);
            Assert.AreEqual(250, _data.TargetingMax);
        }

        [Test]
        public void OnValidate_BandFarFromAnyPrice_IsNotRejected()
        {
            // 타겟팅은 선호도일 뿐 자격 게이트가 아니다 (CLAUDE.md §1.1-3a).
            // 덱에 존재하지 않는 가격대여도 유효하며, 검증이 이를 제한해서는 안 된다.
            // 그런 손님은 굶는 것이 아니라 '대역 밖 중 가장 가까운 것' 을 먹는다.
            SerializedFieldSetter.SetTargetingBand(_data, 99998, 99999);

            _data.OnValidate();

            Assert.AreEqual(99998, _data.TargetingMin);
            Assert.AreEqual(99999, _data.TargetingMax);
        }
    }
}
