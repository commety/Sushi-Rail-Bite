using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Tests.PlayMode
{
    /// <summary>
    /// PlayMode 테스트용 <see cref="StageConfig"/> 조립기.
    ///
    /// <para>
    /// EditMode 쪽 조립기를 재사용할 수 없다 — <c>Tests.PlayMode</c> 는 플레이어 대상으로도
    /// 컴파일되므로 <c>UnityEditor</c> 참조를 그대로 들 수 없다. 그래서 에디터 전용 경로를
    /// <c>#if UNITY_EDITOR</c> 로 감싼다.
    /// </para>
    /// <para>
    /// 디스크의 밸런스 애셋을 로드하지 않는다 (<c>.claude/rules/tests.md</c> §4).
    /// </para>
    /// </summary>
    internal static class StageConfigTestFactory
    {
        public static StageConfig Create(float beltSpeed, float spawnInterval, float beltLength,
                                         SushiData spawnSushi)
        {
            var config = ScriptableObject.CreateInstance<StageConfig>();

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(config);
            serialized.FindProperty("_beltSpeed").floatValue = beltSpeed;
            serialized.FindProperty("_spawnIntervalSeconds").floatValue = spawnInterval;
            serialized.FindProperty("_beltLength").floatValue = beltLength;

            var table = serialized.FindProperty("_spawnTable");
            table.InsertArrayElementAtIndex(0);
            var entry = table.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("_sushi").objectReferenceValue = spawnSushi;

            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return config;
        }

        /// <summary>직렬화된 정수 필드에 값을 밀어 넣는다. 프로덕션에 세터를 열지 않기 위해서다.</summary>
        public static void SetInt(StageConfig config, string fieldName, int value)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(config);
            serialized.FindProperty(fieldName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }
    }
}
