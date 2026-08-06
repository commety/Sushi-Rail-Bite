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

        public static void SetString(UnityEngine.Object target, string fieldName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).AssertFound(fieldName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 오브젝트 참조 리스트 끝에 한 개를 붙인다.
        ///
        /// <c>InsertArrayElementAtIndex</c> 는 직전 원소를 복제하므로, 붙인 직후 값을 덮어써야
        /// 같은 참조가 두 번 들어가지 않는다.
        /// </summary>
        public static void AppendObject(UnityEngine.Object target, string listFieldName,
                                        UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var list = serialized.FindProperty(listFieldName).AssertFound(listFieldName);

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = value;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// <c>CustomerData</c> 의 타겟팅 대역을 한 번에 밀어 넣는다.
        ///
        /// 두 필드가 항상 짝으로 움직이는데다, M2.5 에서 <c>_targetingPrice</c> 를 대역으로
        /// 쪼갤 때 <b>문자열 필드명이 네 개 테스트 파일에 흩어져 있어</b> 컴파일러가 아무것도
        /// 잡아 주지 못했다. 이름이 또 바뀔 때 고칠 곳을 한 군데로 모아 둔다.
        /// </summary>
        public static void SetTargetingBand(UnityEngine.Object customerData, int min, int max)
        {
            SetInt(customerData, "_targetingMin", min);
            SetInt(customerData, "_targetingMax", max);
        }

        private static SerializedProperty AssertFound(this SerializedProperty property, string fieldName)
        {
            Assert.IsNotNull(property, $"직렬화 필드 '{fieldName}' 를 찾지 못했습니다 — 필드명이 바뀌었는지 확인하세요.");
            return property;
        }
    }
}
