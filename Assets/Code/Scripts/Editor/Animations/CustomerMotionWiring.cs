using SushiDefense.Customers;
using SushiDefense.Data;
using UnityEditor;
using UnityEngine;

namespace SushiDefense.EditorTools.Animations
{
    /// <summary>
    /// <see cref="CustomerAnimationBuilder"/> 가 만든 클립을 <b>실제로 재생되게</b> 잇는다 —
    /// 손님 데이터에 컨트롤러를 물리고, 프리팹에 <c>Animator</c> 와 말풍선을 세운다.
    ///
    /// <para>
    /// <b>빌더와 나눠 둔 이유</b>: 한쪽은 애셋을 굽고 다른 쪽은 그것을 배선한다. 합치면
    /// 클립을 다시 굽고 싶을 때마다 프리팹까지 건드리게 되고, 프리팹은 사람이 손으로
    /// 배치를 조정하는 애셋이라 덮어쓰기가 위험하다.
    /// </para>
    /// <para>
    /// <b>멱등이다.</b> 이미 붙어 있으면 다시 만들지 않고 값만 맞춘다 — 두 번 돌려서
    /// 말풍선이 둘이 되면 안 된다.
    /// </para>
    /// </summary>
    public static class CustomerMotionWiring
    {
        private const string PrefabPath =
            "Assets/Code/Scripts/Presentation/Customers/Customer.prefab";

        /// <summary>
        /// 낱장 그림 이름에서 컨트롤러 이름을 얻는다. <c>customer-standard-front</c> 의
        /// <c>-front</c> 를 떼면 <c>customer-standard</c> 가 되고, 그것이 시트·컨트롤러
        /// 이름과 같다.
        ///
        /// <para>
        /// <c>CustomerData.Id</c> 를 쓰지 않는 이유: 그쪽은 <c>customer-big-eater</c> 인데
        /// 시트는 <c>customer-bigeater</c> 라 <b>구분자가 어긋난다.</b> 그림과 동작은 같은
        /// 사람이 같은 이름으로 그리므로 그림 쪽이 더 튼튼한 열쇠다.
        /// </para>
        /// </summary>
        private const string IconSuffix = "-front";

        /// <summary>말풍선이 머리 위에 서는 높이(유닛). 소화 배지와 같은 축을 피해 왼쪽으로 뺀다.</summary>
        private static readonly Vector3 ThinkingOffset = new(-0.62f, 0.85f, 0f);

        /// <summary>
        /// 말풍선이 몸통보다 앞에 그려지도록 하는 정렬 순서. 소화 배지(10)와 같은 층이다.
        /// </summary>
        private const int ThinkingSortingOrder = 10;

        [MenuItem("SushiRailBite/Art/Wire Customer Motions")]
        public static void Wire()
        {
            var wired = WireCustomerData();
            WirePrefab();

            AssetDatabase.SaveAssets();

            // 몇 개를 물렸는지 <b>세어서 남긴다.</b> 짝을 못 찾은 손님은 조용히 건너뛰므로,
            // 수를 안 내놓으면 «0개 배선» 과 «전부 배선» 이 같은 로그로 보인다 — 화면에서는
            // 손님이 그냥 안 움직이는 것으로만 드러난다.
            Debug.Log($"[SushiRailBite] 손님 동작을 배선했습니다 — 데이터 {wired}종 · 프리팹 1");
        }

        /// <summary>
        /// 손님 데이터마다 자기 유형의 컨트롤러를 물린다. 짝이 없으면 <b>비워 둔다</b> —
        /// 엉뚱한 유형의 동작을 물리느니 낱장 그림이 서 있는 편이 낫다
        /// (placeholder 손님에게는 애초에 시트가 없다).
        /// </summary>
        private static int WireCustomerData()
        {
            var wired = 0;

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(CustomerData)}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<CustomerData>(path);
                if (data == null || data.Icon == null)
                {
                    continue;
                }

                var iconName = data.Icon.name;
                if (!iconName.EndsWith(IconSuffix, System.StringComparison.Ordinal))
                {
                    Debug.LogWarning($"[SushiRailBite] {data.name}: 그림 이름 '{iconName}' 이 "
                                     + $"'{IconSuffix}' 로 끝나지 않아 동작을 못 찾는다");
                    continue;
                }

                var kind = iconName[..^IconSuffix.Length];
                var controllerPath =
                    $"{CustomerAnimationBuilder.OutputFolder}/{kind}.controller";
                var controller =
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);

                if (controller == null)
                {
                    Debug.LogWarning($"[SushiRailBite] {data.name}: {controllerPath} 이 없다");
                    continue;
                }

                var serialized = new SerializedObject(data);
                serialized.FindProperty("_motions").objectReferenceValue = controller;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                wired++;
            }

            return wired;
        }

        /// <summary>
        /// 프리팹 뿌리에 <c>Animator</c> 와 재생기를, 그 밑에 말풍선을 세운다.
        ///
        /// <para>
        /// <b><c>Animator</c> 는 몸통 렌더러와 같은 오브젝트여야 한다.</b> 클립의 곡선이
        /// 빈 경로(=«컨트롤러가 달린 오브젝트 자신»)에 묶여 있어서, 자식에 달면 아무것도
        /// 움직이지 않는데 <b>예외도 경고도 나지 않는다.</b>
        /// </para>
        /// </summary>
        private static void WirePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                var animator = GetOrAdd<Animator>(root);
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;

                var thinking = BuildThinkingChild(root);
                var motionView = GetOrAdd<CustomerMotionView>(root);

                var serialized = new SerializedObject(motionView);
                serialized.FindProperty("_animator").objectReferenceValue = animator;
                serialized.FindProperty("_body").objectReferenceValue =
                    root.GetComponent<SpriteRenderer>();
                serialized.FindProperty("_thinking").objectReferenceValue = thinking;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var view = new SerializedObject(root.GetComponent<CustomerView>());
                view.FindProperty("_motionView").objectReferenceValue = motionView;
                view.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 말풍선 오브젝트. <b>꺼진 채로 둔다</b> — 켜는 것은 조율자가 «기다리는 중» 이라고
        /// 답할 때뿐이며, 켜진 채 저장하면 앉자마자 모든 손님이 고민하는 것으로 보인다.
        /// </summary>
        private static GameObject BuildThinkingChild(GameObject root)
        {
            var existing = root.transform.Find(CustomerMotionView.ThinkingChildName);
            var child = existing != null
                ? existing.gameObject
                : new GameObject(CustomerMotionView.ThinkingChildName);

            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = ThinkingOffset;
            child.transform.localScale = Vector3.one;

            var renderer = GetOrAdd<SpriteRenderer>(child);
            renderer.sortingOrder = ThinkingSortingOrder;
            renderer.sprite = FirstFrameOfThinking();

            var animator = GetOrAdd<Animator>(child);
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    $"{CustomerAnimationBuilder.OutputFolder}/{CustomerAnimationBuilder.ThinkingName}.controller");
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;

            child.SetActive(false);
            return child;
        }

        /// <summary>
        /// 렌더러에 첫 칸을 미리 넣어 둔다. 비워 두면 <c>Animator</c> 가 첫 프레임을 쓰기
        /// 전까지 <b>한 프레임 동안 아무것도 안 보인다</b> — 잠깐 깜빡이는 것으로 나타난다.
        /// </summary>
        private static Sprite FirstFrameOfThinking()
        {
            var sheet = $"Assets/Art/Animations/{CustomerAnimationBuilder.ThinkingName}.png";
            var frames = AssetDatabase.LoadAllAssetsAtPath(sheet);

            Sprite first = null;
            foreach (var asset in frames)
            {
                if (asset is Sprite sprite
                    && (first == null
                        || string.CompareOrdinal(sprite.name, first.name) < 0))
                {
                    first = sprite;
                }
            }

            return first;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent<T>(out var existing) ? existing : target.AddComponent<T>();
        }
    }
}
