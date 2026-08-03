using NUnit.Framework;
using UnityEditor;

namespace SushiDefense.Tests.EditMode.Data
{
    /// <summary>
    /// 직렬화된 private 필드에 이상값을 밀어 넣는 테스트 도구.
    ///
    /// SO 의 검증은 "인스펙터로 음수를 넣어도 막히는가"가 핵심이라, 테스트가 필드에 직접
    /// 써 볼 수 있어야 한다. 이를 위해 프로덕션에 public 세터를 여는 것은 금지되어 있으므로
    /// (<c>.claude/rules/tests.md</c> §7) 직렬화 계층으로 우회한다.
    ///
    /// 필드명을 문자열로 받으므로 <b>필드 이름을 바꾸면 여기서 깨진다</b>. 조용히 통과하지 않도록
    /// 프로퍼티를 못 찾으면 즉시 실패시킨다.
    /// </summary>
    internal static class SerializedFieldSetter
    {
        public static void SetInt(UnityEngine.Object target, string fieldName, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).AssertFound(fieldName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(UnityEngine.Object target, string fieldName, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).AssertFound(fieldName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty AssertFound(this SerializedProperty property, string fieldName)
        {
            Assert.IsNotNull(property, $"직렬화 필드 '{fieldName}' 를 찾지 못했습니다 — 필드명이 바뀌었는지 확인하세요.");
            return property;
        }
    }
}
