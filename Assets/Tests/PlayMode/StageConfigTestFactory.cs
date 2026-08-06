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
        /// <summary>판정이 끼어들지 않을 만큼 큰 목표. 밸런스 값이 아니라 테스트 도구다.</summary>
        private const int OpenEndedTargetRevenue = 1_000_000;

        /// <summary>같은 목적의 제한 시간. PlayMode 테스트가 이보다 길게 돌지 않는다.</summary>
        private const float OpenEndedTimeLimitSeconds = 3600f;

        public static StageConfig Create(float beltSpeed, float spawnInterval, float beltLength,
                                         SushiData spawnSushi)
        {
            var config = ScriptableObject.CreateInstance<StageConfig>();

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(config);
            serialized.FindProperty("_beltSpeed").floatValue = beltSpeed;
            serialized.FindProperty("_spawnIntervalSeconds").floatValue = spawnInterval;
            serialized.FindProperty("_beltLength").floatValue = beltLength;

            // 목표 매출·제한 시간을 넉넉히 준다. SO 기본값은 각각 1 과 1초라, M3 에서
            // StageBootstrap 이 StageController 로 시간을 흘리게 되면서 **초밥 하나만
            // 먹어도 클리어**되고 1초 뒤 시뮬레이션이 멎는다. 이 조립기를 쓰는 테스트는
            // 벨트·집기를 보는 것이지 클리어 판정을 보는 것이 아니므로, 판정이 끼어들지
            // 않도록 밀어 둔다. 판정을 보는 테스트는 값을 직접 덮어쓴다.
            serialized.FindProperty("_targetRevenue").intValue = OpenEndedTargetRevenue;
            serialized.FindProperty("_timeLimitSeconds").floatValue = OpenEndedTimeLimitSeconds;

            var table = serialized.FindProperty("_spawnTable");
            table.InsertArrayElementAtIndex(0);
            var entry = table.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("_sushi").objectReferenceValue = spawnSushi;

            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return config;
        }

        /// <summary>
        /// 자리 정의를 하나 붙인다. <b>씬에 박힌 값과 다른 값을 주는 것이 쓰임새다</b> —
        /// 같은 값을 주면 바인딩을 아예 안 하는 구현도 통과한다.
        /// </summary>
        public static void AddTableSlot(StageConfig config, int slotIndex, float beltPosition,
                                        Vector2 position)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(config);
            var slots = serialized.FindProperty("_tableSlots");

            slots.InsertArrayElementAtIndex(slots.arraySize);
            var slot = slots.GetArrayElementAtIndex(slots.arraySize - 1);
            slot.FindPropertyRelative("_slotIndex").intValue = slotIndex;
            slot.FindPropertyRelative("_beltPosition").floatValue = beltPosition;
            slot.FindPropertyRelative("_position").vector2Value = position;

            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        /// <summary>주어진 스테이지를 <b>그 순서대로</b> 담은 런 구성을 만든다.</summary>
        public static RunConfig CreateRun(params StageConfig[] stages)
        {
            var runConfig = ScriptableObject.CreateInstance<RunConfig>();

#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(runConfig);
            var list = serialized.FindProperty("_stages");

            for (var i = 0; i < stages.Length; i++)
            {
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif

            return runConfig;
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

        /// <summary>직렬화된 실수 필드에 값을 밀어 넣는다.</summary>
        public static void SetFloat(StageConfig config, string fieldName, float value)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(config);
            serialized.FindProperty(fieldName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }
    }
}
