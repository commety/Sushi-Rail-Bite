using TMPro;
using UnityEngine;

namespace SushiDefense.UI
{
    /// <summary>
    /// 화면 라벨을 다루는 공용 조각.
    ///
    /// <para>
    /// <see cref="TMP_Text"/> 로 받는 이유는 그것이 <c>TextMeshProUGUI</c>(Canvas)와
    /// <c>TextMeshPro</c>(월드 스페이스)의 공통 조상이기 때문이다. HUD·보상·전환은 Canvas 로
    /// 가지만 손님 대역 라벨은 자리 옆에 붙어 있어야 해서 월드 스페이스로 남는다 — 한 조각이
    /// 둘 다 다룰 수 있어야 한다.
    /// </para>
    /// <para>
    /// <b>폰트를 코드에서 강제하지 않는다.</b> 비어 있으면 TMP 가 프로젝트 기본 폰트로
    /// 대신하며, 어느 폰트를 쓸지는 씬·프리팹이 정할 일이다. 코드가 특정 애셋을 이름으로
    /// 집으면 폰트를 갈아 끼울 때마다 여기를 고쳐야 한다.
    /// </para>
    /// </summary>
    internal static class HudLabel
    {
        /// <summary>
        /// 인스펙터에서 비어 있으면 <b>자기 하위 계층에서만</b> 이름으로 찾아 채운다.
        /// <c>Find</c>/<c>FindObjectOfType</c> 같은 씬 전역 탐색이 아니다 (§4.3 금지 대상).
        /// </summary>
        public static TMP_Text Resolve(Transform parent, TMP_Text assigned, string childName)
        {
            if (assigned != null)
            {
                return assigned;
            }

            var child = parent.Find(childName);
            return child != null && child.TryGetComponent<TMP_Text>(out var label) ? label : null;
        }

        /// <summary>라벨이 없어도 조용히 넘어간다 — 표시는 로직의 전제 조건이 아니다.</summary>
        public static void Write(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }
    }
}
