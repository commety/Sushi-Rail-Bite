using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SushiDefense.EditorTools.Animations
{
    /// <summary>
    /// 시트에서 잘린 칸들을 <see cref="AnimationClip"/> 으로 엮고, 손님 유형마다 상태 기계를
    /// 하나씩 만든다.
    ///
    /// <para>
    /// <b>아직 아무도 이 클립을 재생하지 않는다.</b> 프리팹에 <c>Animator</c> 를 달고
    /// <see cref="PickingParameter"/> 를 흔드는 일은 남겨 두었다 — 손님의 상태는
    /// <c>CustomerLogic</c> 이 이미 들고 있으므로, 붙일 때 판정을 새로 만들 이유가 없다.
    /// </para>
    /// <para>
    /// 손으로 만들지 않는 이유: 클립 일곱 개에 프레임을 하나씩 끌어다 놓는 일이고, 프레임
    /// <b>순서가 한 칸만 어긋나도</b> 손님이 딸꾹질하듯 움직인다. 그 증상은 애니메이션 창을
    /// 열기 전에는 원인을 알 수 없다.
    /// </para>
    /// </summary>
    public static class CustomerAnimationBuilder
    {
        /// <summary>만들어진 클립·컨트롤러가 사는 곳.</summary>
        public const string OutputFolder = "Assets/Level/Animations";

        /// <summary>«지금 집는 중인가». 상태 기계가 두 클립을 오가는 유일한 조건이다.</summary>
        public const string PickingParameter = "Picking";

        private const string SheetFolder = "Assets/Art/Animations";

        /// <summary>
        /// 초당 프레임. 네 칸짜리 대기 동작이 0.5 초에 한 번 도는 속도다 — 더 빠르면
        /// 앉아 있는 손님이 떨고, 더 느리면 멈춰 있는 것처럼 보인다.
        /// </summary>
        private const float FrameRate = 8f;

        private static readonly string[] Kinds = { "standard", "bigeater", "smalleater" };

        private static readonly string[] Motions = { "idle", "picking" };

        [MenuItem("SushiRailBite/Art/Rebuild Customer Animations")]
        public static void Rebuild()
        {
            EnsureFolder(OutputFolder);

            foreach (var kind in Kinds)
            {
                var clips = Motions
                    .Select(motion => BuildClip($"customer-{kind}-{motion}", loop: true))
                    .ToArray();

                BuildController(kind, clips[0], clips[1]);
            }

            BuildClip("eating-interface", loop: true);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SushiRailBite] 애니메이션을 다시 만들었습니다 — {OutputFolder}");
        }

        /// <summary>
        /// 시트 한 장을 클립 하나로 엮는다. 이미 있으면 <b>덮어쓰지 않고 곡선만 갈아 끼운다</b> —
        /// 새로 만들면 GUID 가 바뀌어 컨트롤러가 물고 있던 클립이 끊어진다.
        /// </summary>
        private static AnimationClip BuildClip(string sheetName, bool loop)
        {
            var sprites = LoadFrames(sheetName);
            var path = $"{OutputFolder}/{sheetName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = FrameRate;

            // 빈 경로는 «컨트롤러가 달린 오브젝트 자신» 을 뜻한다. 손님의 몸통 렌더러가
            // 루트에 있으므로 하위 경로가 필요 없다.
            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            var keys = sprites
                .Select((sprite, index) => new ObjectReferenceKeyframe
                {
                    time = index / FrameRate,
                    value = sprite
                })
                .ToArray();

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        /// <summary>
        /// 유형 하나의 상태 기계. 이미 있으면 그대로 둔다 — 사람이 전이 조건을 손봤을 수 있고,
        /// 다시 만들면 그것이 통째로 날아간다.
        /// </summary>
        private static void BuildController(string kind, AnimationClip idle, AnimationClip picking)
        {
            var path = $"{OutputFolder}/customer-{kind}.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            {
                return;
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter(PickingParameter, AnimatorControllerParameterType.Bool);

            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle");
            idleState.motion = idle;
            var pickingState = machine.AddState("Picking");
            pickingState.motion = picking;
            machine.defaultState = idleState;

            var toPicking = idleState.AddTransition(pickingState);
            toPicking.hasExitTime = false;
            toPicking.duration = 0f;
            toPicking.AddCondition(AnimatorConditionMode.If, 0f, PickingParameter);

            var toIdle = pickingState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PickingParameter);
        }

        /// <summary>
        /// 시트에서 칸들을 번호 순서로 읽는다. <c>LoadAllAssetsAtPath</c> 는 순서를 약속하지
        /// 않으므로 이름 끝의 번호로 다시 세운다 — 순서가 섞이면 손님이 딸꾹질한다.
        /// </summary>
        private static Sprite[] LoadFrames(string sheetName)
        {
            var path = $"{SheetFolder}/{sheetName}.png";
            var frames = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => IndexOf(s.name))
                .ToArray();

            if (frames.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{path} 에 칸이 없습니다. 임포트 모드가 Multiple 인지 확인하세요.");
            }

            return frames;
        }

        private static int IndexOf(string spriteName)
        {
            var underscore = spriteName.LastIndexOf('_');
            return underscore >= 0 && int.TryParse(spriteName[(underscore + 1)..], out var index)
                ? index
                : 0;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
