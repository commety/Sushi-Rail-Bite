using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님이 쉬는 중임을 그 위에 작게 알린다.
    ///
    /// <para>
    /// <b>상태를 모른다.</b> 언제 뜰지는 <see cref="CustomerView"/> 가 정하고 여기는
    /// <see cref="Show"/>/<see cref="Hide"/> 만 받는다 — 뷰가 상태를 되물으면 상태 머신의
    /// 진실이 둘이 된다 (<c>CLAUDE.md</c> §3.2·§3.5).
    /// </para>
    /// <para>
    /// <b>글자가 아니라 아이콘이다.</b> 요구는 16×16 px 인데 이 프로젝트의 픽셀 폰트는
    /// 12 px 이라 «소화중» 세 글자가 그 안에 들어가지 않는다. 말풍선 폭을 다시 정하는 것은
    /// 별도 기획이므로, 지금은 아이콘 하나가 그 상태를 말한다.
    /// </para>
    /// </summary>
    public sealed class DigestingBadgeView : MonoBehaviour
    {
        /// <summary>켜고 끌 대상. 비어 있으면 이 오브젝트다.</summary>
        [SerializeField] private GameObject _visual;

        /// <summary>지금 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>배지를 띄운다.</summary>
        public void Show()
        {
            Apply(true);
        }

        /// <summary>배지를 내린다.</summary>
        public void Hide()
        {
            Apply(false);
        }

        private void Awake()
        {
            if (_visual == null)
            {
                _visual = gameObject;
            }

            Hide();
        }

        /// <summary>
        /// 켜고 끄는 유일한 경로. <see cref="Awake"/> 가 돌기 전에 불릴 수 있어
        /// (프리팹에서 꺼진 채로 시작한다) 대상을 여기서 한 번 더 확인한다.
        /// </summary>
        private void Apply(bool showing)
        {
            IsShowing = showing;

            var target = _visual != null ? _visual : gameObject;
            target.SetActive(showing);
        }
    }
}
