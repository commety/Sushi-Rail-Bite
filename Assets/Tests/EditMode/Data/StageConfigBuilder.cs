using System.Collections.Generic;
using NUnit.Framework;
using SushiDefense.Data;
using UnityEditor;
using UnityEngine;

namespace SushiDefense.Tests.EditMode.Data
{
    /// <summary>
    /// 테스트용 <see cref="StageConfig"/> 조립기.
    ///
    /// <para>
    /// 디스크의 밸런스 애셋을 로드하지 않는다 — 밸런스가 바뀔 때마다 테스트가 깨지기 때문이다
    /// (<c>.claude/rules/tests.md</c> §4). 프로덕션에 세터를 열지 않으려고 직렬화 계층으로
    /// 값을 밀어 넣는다.
    /// </para>
    /// <para>
    /// 만들어진 <see cref="StageConfig"/> 는 호출자가 <c>DestroyImmediate</c> 로 정리한다.
    /// </para>
    /// </summary>
    internal sealed class StageConfigBuilder
    {
        private readonly StageConfig _config = ScriptableObject.CreateInstance<StageConfig>();

        public StageConfigBuilder WithBeltSpeed(float value) => SetFloat("_beltSpeed", value);

        public StageConfigBuilder WithSpawnInterval(float value) => SetFloat("_spawnIntervalSeconds", value);

        public StageConfigBuilder WithBeltLength(float value) => SetFloat("_beltLength", value);

        public StageConfigBuilder WithRecognitionLatch(float value) => SetFloat("_recognitionLatchSeconds", value);

        public StageConfigBuilder WithMaxPlacedCustomers(int value) => SetInt("_maxPlacedCustomers", value);

        /// <summary>스폰 표에 항목을 하나 붙인다. 넣은 순서가 그대로 스폰 순서가 된다.</summary>
        public StageConfigBuilder WithSpawnEntry(SushiData sushi, int weight)
        {
            var serialized = new SerializedObject(_config);
            var table = Find(serialized, "_spawnTable");

            table.InsertArrayElementAtIndex(table.arraySize);
            var entry = table.GetArrayElementAtIndex(table.arraySize - 1);
            entry.FindPropertyRelative("_sushi").objectReferenceValue = sushi;
            entry.FindPropertyRelative("_weight").intValue = weight;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return this;
        }

        /// <summary>자리를 하나 붙인다. 벨트 좌표는 집기 범위 판정의 기준점이 된다.</summary>
        public StageConfigBuilder WithTableSlot(int slotIndex, float beltPosition)
        {
            var serialized = new SerializedObject(_config);
            var slots = Find(serialized, "_tableSlots");

            slots.InsertArrayElementAtIndex(slots.arraySize);
            var slot = slots.GetArrayElementAtIndex(slots.arraySize - 1);
            slot.FindPropertyRelative("_slotIndex").intValue = slotIndex;
            slot.FindPropertyRelative("_beltPosition").floatValue = beltPosition;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return this;
        }

        public StageConfig Build() => _config;

        private StageConfigBuilder SetFloat(string fieldName, float value)
        {
            var serialized = new SerializedObject(_config);
            Find(serialized, fieldName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return this;
        }

        private StageConfigBuilder SetInt(string fieldName, int value)
        {
            var serialized = new SerializedObject(_config);
            Find(serialized, fieldName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return this;
        }

        private static SerializedProperty Find(SerializedObject serialized, string fieldName)
        {
            var property = serialized.FindProperty(fieldName);
            Assert.IsNotNull(property, $"직렬화 필드 '{fieldName}' 를 찾지 못했습니다 — 필드명이 바뀌었는지 확인하세요.");
            return property;
        }

        /// <summary>테스트가 만든 초밥 데이터를 함께 정리할 수 있도록 모아 둔다.</summary>
        public static SushiData CreateSushi(List<Object> disposables)
        {
            var sushi = ScriptableObject.CreateInstance<SushiData>();
            disposables.Add(sushi);
            return sushi;
        }
    }
}
