using UnityEngine;

namespace SushiDefense.Belt
{
    /// <summary>
    /// 초밥 하나의 화면 표현. 대응하는 <see cref="SushiItem"/> 을 들고 좌표만 따라간다.
    /// <b>판정이 없다</b> — 집기·배정은 전부 <c>Runtime</c> 쪽에서 끝난다.
    /// </summary>
    public sealed class SushiItemView : MonoBehaviour
    {
        /// <summary>이 뷰가 그리고 있는 런타임 초밥. 반납 후에는 <c>null</c> 이다.</summary>
        public SushiItem Model { get; private set; }

        /// <summary>대여 직후 어떤 초밥을 그릴지 물린다.</summary>
        public void Bind(SushiItem model)
        {
            Model = model;
        }

        /// <summary>반납 직전 참조를 끊는다. 풀에 누운 뷰가 죽은 모델을 붙들지 않게 한다.</summary>
        public void Release()
        {
            Model = null;
        }
    }
}
