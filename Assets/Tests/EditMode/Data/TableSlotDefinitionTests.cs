using NUnit.Framework;
using SushiDefense.Data;
using UnityEditor;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Data
{
    /// <summary>
    /// <see cref="TableSlotDefinition"/> 은 <c>UnityEngine.Object</c> 가 아니라 SerializedObject 로
    /// 직접 감쌀 수 없다. 실제로 값이 저작되는 경로 그대로 — <see cref="StageConfig"/> 의
    /// 자리 목록 원소로서 — 검증한다.
    /// </summary>
    public sealed class TableSlotDefinitionTests
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
        public void BeltPosition_FreshSlot_IsZero()
        {
            AddSlot();

            Assert.AreEqual(0f, _config.TableSlots[0].BeltPosition);
        }

        [Test]
        public void BeltPosition_NegativeValue_IsAllowed()
        {
            // 벨트 좌표 원점은 씬 조립에서 정해진다. 시작점보다 앞에 있는 자리가
            // 정상일 수 있으므로 클램프를 걸지 않는다.
            AddSlot();
            SetSlotBeltPosition(0, -5f);

            _config.OnValidate();

            Assert.AreEqual(-5f, _config.TableSlots[0].BeltPosition);
        }

        [Test]
        public void BeltPosition_AndScreenPosition_AreIndependent()
        {
            // 화면 좌표와 판정용 1차원 좌표는 역할이 다르다. 하나를 바꿔도
            // 다른 하나가 따라 움직이지 않아야 한다.
            AddSlot();
            SetSlotBeltPosition(0, 12f);

            Assert.AreEqual(12f, _config.TableSlots[0].BeltPosition);
            Assert.AreEqual(Vector2.zero, _config.TableSlots[0].Position);
        }

        [Test]
        public void BeltPosition_MultipleSlots_HoldIndependentValues()
        {
            AddSlot();
            AddSlot();

            SetSlotBeltPosition(0, 3f);
            SetSlotBeltPosition(1, 9f);

            Assert.AreEqual(3f, _config.TableSlots[0].BeltPosition);
            Assert.AreEqual(9f, _config.TableSlots[1].BeltPosition);
        }

        private void AddSlot()
        {
            var serialized = new SerializedObject(_config);
            var slots = serialized.FindProperty("_tableSlots");
            Assert.IsNotNull(slots, "직렬화 필드 '_tableSlots' 를 찾지 못했습니다.");

            slots.InsertArrayElementAtIndex(slots.arraySize);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetSlotBeltPosition(int index, float value)
        {
            var serialized = new SerializedObject(_config);
            var slot = serialized.FindProperty("_tableSlots").GetArrayElementAtIndex(index);
            var beltPosition = slot.FindPropertyRelative("_beltPosition");
            Assert.IsNotNull(beltPosition, "직렬화 필드 '_beltPosition' 를 찾지 못했습니다.");

            beltPosition.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
