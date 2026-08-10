using UnityEngine;

namespace SushiDefense.Belt
{
    /// <summary>
    /// 초밥 하나의 화면 표현. 대응하는 <see cref="SushiItem"/> 을 들고 좌표와 그림만 따라간다.
    /// <b>판정이 없다</b> — 집기·배정은 전부 <c>Runtime</c> 쪽에서 끝난다.
    ///
    /// <para>
    /// <b>어느 그림을 그릴지는 데이터가 정한다.</b> 종류나 가격으로 여기서 분기하지 않는다 —
    /// 그러면 초밥을 추가할 때마다 이 파일을 고쳐야 한다 (<c>CLAUDE.md</c> §3.1).
    /// </para>
    /// </summary>
    public sealed class SushiItemView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;

        /// <summary>
        /// 프리팹이 들고 있던 그림. 데이터에 아이콘이 없을 때 여기로 돌아간다 —
        /// <b>빈 화면을 만들지 않는다.</b> 아이콘을 아직 안 채운 애셋에서 초밥이 사라지면
        /// 벨트가 고장 난 것처럼 보인다.
        /// </summary>
        private Sprite _fallbackSprite;

        /// <summary>이 뷰가 그리고 있는 런타임 초밥. 반납 후에는 <c>null</c> 이다.</summary>
        public SushiItem Model { get; private set; }

        /// <summary>지금 화면에 나가 있는 그림. 검증용이다.</summary>
        public Sprite ShownSprite => _body != null ? _body.sprite : null;

        /// <summary>대여 직후 어떤 초밥을 그릴지 물린다.</summary>
        public void Bind(SushiItem model)
        {
            Model = model;

            // UnityEngine.Object 의 == 오버로드 때문에 ?. 를 쓰지 않는다 (CLAUDE.md §4.3).
            var icon = model != null && model.Data != null ? model.Data.Icon : null;
            Show(icon);
        }

        /// <summary>
        /// 반납 직전 참조를 끊는다. 풀에 누운 뷰가 죽은 모델을 붙들지 않게 하고,
        /// <b>직전 초밥의 그림도 함께 되돌린다</b> — 남겨 두면 재사용 첫 프레임에 엉뚱한
        /// 초밥이 번쩍인다.
        /// </summary>
        public void Release()
        {
            Model = null;
            Show(null);
        }

        private void Awake()
        {
            if (_body == null)
            {
                _body = GetComponent<SpriteRenderer>();
            }

            if (_body != null)
            {
                _fallbackSprite = _body.sprite;
            }
        }

        /// <summary>
        /// 렌더러가 없어도 조용히 넘어간다 — 표시는 로직의 전제 조건이 아니다.
        /// <c>Bind</c> 시점에만 부르고 매 프레임 대입하지 않는다 (<c>scripts.md</c> §4).
        /// </summary>
        private void Show(Sprite icon)
        {
            if (_body == null)
            {
                return;
            }

            _body.sprite = icon != null ? icon : _fallbackSprite;
        }
    }
}
