using UnityEngine;

namespace SushiDefense.UI
{
    /// <summary>
    /// <see cref="TextMesh"/> placeholder 라벨을 다루는 공용 조각.
    ///
    /// <para>
    /// 씬에 Canvas 가 없고 전부 월드 스페이스라 <c>TextMesh</c> 를 쓴다. 폰트가 비어 있으면
    /// <c>TextMesh</c> 는 아무것도 그리지 않고, 라벨의 머티리얼도 폰트의 것으로 맞춰야
    /// 글자가 나온다 — 씬에서 이 둘을 일일이 물리지 않아도 되게 여기서 채운다.
    /// </para>
    /// <para>
    /// <b>M6(메인화면·덱빌딩)에서 제대로 된 UI 로 교체되며 사라진다.</b> 두 뷰가 같은
    /// 12줄을 각자 갖고 있으면 그때 한쪽만 지워질 위험이 있어 한곳에 모아 둔다.
    /// </para>
    /// </summary>
    internal static class PlaceholderLabel
    {
        /// <summary>Unity 내장 폰트.</summary>
        private const string FallbackFontName = "LegacyRuntime.ttf";

        /// <summary>
        /// 인스펙터에서 비어 있으면 <b>자기 하위 계층에서만</b> 이름으로 찾아 채운다.
        /// <c>Find</c>/<c>FindObjectOfType</c> 같은 씬 전역 탐색이 아니다 (§4.3 금지 대상).
        /// </summary>
        public static TextMesh Resolve(Transform parent, TextMesh assigned, string childName)
        {
            if (assigned != null)
            {
                EnsureFont(assigned);
                return assigned;
            }

            var child = parent.Find(childName);
            if (child == null || !child.TryGetComponent<TextMesh>(out var label))
            {
                return null;
            }

            EnsureFont(label);
            return label;
        }

        /// <summary>라벨이 없어도 조용히 넘어간다 — 표시는 로직의 전제 조건이 아니다.</summary>
        public static void Write(TextMesh label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        private static void EnsureFont(TextMesh label)
        {
            if (label.font != null)
            {
                return;
            }

            var font = Resources.GetBuiltinResource<Font>(FallbackFontName);
            if (font == null)
            {
                return;
            }

            label.font = font;
            if (label.TryGetComponent<MeshRenderer>(out var renderer))
            {
                renderer.sharedMaterial = font.material;
            }
        }
    }
}
