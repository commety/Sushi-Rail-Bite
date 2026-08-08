using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 포화도를 칸으로 그린다. <b>계산하지 않는다</b> — 몇 칸인지는
    /// <see cref="SaturationGauge"/> 가 정한다.
    ///
    /// <para>
    /// 손님이 얼마나 찼는지가 화면에 없으면, 안 먹는 이유가 <i>포화</i>인지 <i>대역</i>인지
    /// 구분되지 않는다. 상태 틴트만으로는 회색빛(소화 중)과 파란빛(대기 중)을 나란히 놓고
    /// 봐야 겨우 갈린다.
    /// </para>
    /// <para>
    /// <b>칸을 만들지 않는다.</b> 프리팹에 미리 놓인 것을 켜고 끈다 (<c>CLAUDE.md</c> §3.4).
    /// 그래서 개수에 상한이 있고, 최대 포화도가 그 상한을 넘는 경우가 실제로 있다.
    /// </para>
    /// <para>
    /// <b>한 입이 한 칸이 아니다.</b> 초밥마다 채우는 양이 다르므로(장어 3 · 성게 4) 칸은
    /// 먹은 개수가 아니라 포화도를 센다.
    /// </para>
    /// </summary>
    public sealed class SaturationBarView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _cells;

        /// <summary>아직 안 찬 칸. 연출 수치라 프리팹에 있다 (M5 D7).</summary>
        [SerializeField] private Color _emptyColor = new(0.32f, 0.34f, 0.40f);

        /// <summary>찬 칸.</summary>
        [SerializeField] private Color _filledColor = new(1f, 0.84f, 0.25f);

        private CustomerRuntimeState _state;

        /// <summary>지금 켜져 있는 칸 수. 검증용이다.</summary>
        public int ShownVisibleCells { get; private set; }

        /// <summary>지금 찬 것으로 그려진 칸 수. 검증용이다.</summary>
        public int ShownFilledCells { get; private set; }

        /// <summary>인스펙터 없이 칸을 물린다. 테스트용 진입점이다.</summary>
        public void InitializeCells(SpriteRenderer[] cells)
        {
            _cells = cells;
        }

        /// <summary>
        /// 이 손님의 포화도를 그리기 시작한다. <c>null</c> 이면 바를 끈다 — 자리를 갈아탈 때
        /// 직전 손님의 칸이 남으면 방금 앉은 손님이 이미 찬 것처럼 보인다.
        /// </summary>
        public void Bind(CustomerRuntimeState state)
        {
            _state = state;

            var capacity = _cells != null ? _cells.Length : 0;
            var max = state != null ? state.Data.MaxSaturation : 0;

            ShownVisibleCells = SaturationGauge.VisibleCells(max, capacity);

            for (var i = 0; i < capacity; i++)
            {
                var cell = _cells[i];
                if (cell != null)
                {
                    // 표시 칸을 넘는 칸은 끈다. 회색으로 두면 "아직 못 채운 칸" 으로 읽혀
                    // 소식가가 기본 손님처럼 보인다.
                    cell.gameObject.SetActive(i < ShownVisibleCells);
                }
            }

            Refresh();
        }

        /// <summary>
        /// 찬 칸을 다시 계산해 반영한다. <b>값이 바뀐 프레임에만</b> 색을 쓴다 — 매 프레임
        /// 렌더러에 대입하면 배포 타깃(WebGL)에서 손해다 (<c>CustomerView</c> 와 같은 판단).
        /// </summary>
        public void Refresh()
        {
            var filled = _state == null
                ? 0
                : SaturationGauge.FilledCells(_state.CurrentSaturation,
                                              _state.Data.MaxSaturation,
                                              _cells != null ? _cells.Length : 0);

            if (filled == ShownFilledCells)
            {
                return;
            }

            ShownFilledCells = filled;

            if (_cells == null)
            {
                return;
            }

            for (var i = 0; i < _cells.Length; i++)
            {
                var cell = _cells[i];
                if (cell != null)
                {
                    cell.color = i < filled ? _filledColor : _emptyColor;
                }
            }
        }

        private void Awake()
        {
            if (_cells == null || _cells.Length == 0)
            {
                // 씬 전역 탐색이 아니다 — 자기 하위의 칸만 모은다 (§4.3).
                _cells = GetComponentsInChildren<SpriteRenderer>(true);
            }
        }
    }
}
