using System;
using System.Collections.Generic;

namespace SushiDefense.UI
{
    /// <summary>
    /// 스테이지 위에 창이 <b>둘 이상 겹쳐 뜨지 않게</b> 한다. <b>Unity API 를 모른다</b>
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// 덱 화면을 열어 둔 채 메뉴 버튼을 누르면 두 패널이 겹쳐 글자가 서로를 뚫고 나왔다.
    /// 창끼리 서로를 알게 해서 고치면 배선이 창 수의 제곱으로 늘고, 창을 하나 더할 때마다
    /// 기존 전부를 고쳐야 한다 — <b>규칙을 아는 곳을 하나로</b> 둔다.
    /// </para>
    /// <para>
    /// <b>보상 선택만 예외다.</b> 보상 위에는 창 하나를 더 겹칠 수 있다. 보상은 «닫으면
    /// 다음 판» 이라 임의로 내릴 수 없는 창이고, 그 위에서 덱을 확인하고 고르는 것이
    /// 자연스러운 조작이기 때문이다. 예외가 <b>보상 하나뿐</b>인 덕에 «동시에 떠 있는 창은
    /// 최대 둘» 이 불변식으로 유지된다.
    /// </para>
    /// <para>
    /// <b>창을 직접 내리지 않는다.</b> 밀려난 창에게 <see cref="CloseRequested"/> 로 알리기만
    /// 하고, 실제로 내리는 것은 그 창의 프레젠터다 — 조정자가 뷰를 알게 되면 창마다 다른
    /// 닫기 절차(저장·이벤트 발행)를 여기서 다시 알아야 한다.
    /// </para>
    /// </summary>
    public sealed class StageWindowArbiter
    {
        /// <summary>
        /// 위에 무엇도 겹칠 수 없는 창. 정보 조회는 <b>가장 약한 요구</b>라, 이미 무엇이
        /// 떠 있으면 그것을 밀어내지 않고 스스로 물러난다.
        /// </summary>
        private const StageWindow Lowest = StageWindow.CustomerInfo;

        /// <summary>위에 창 하나를 더 허용하는 유일한 창.</summary>
        private const StageWindow Underlay = StageWindow.Reward;

        private readonly List<StageWindow> _open = new();

        /// <summary>지금 가려진 것으로 알린 창들. 같은 알림을 두 번 보내지 않기 위한 기록이다.</summary>
        private readonly List<StageWindow> _covered = new();

        /// <summary>
        /// 이 창을 내려 달라. <b>목록에서 빠진 뒤에</b> 발생하므로, 받은 쪽이 곧바로
        /// <see cref="Close"/> 를 불러도 되돌아오지 않는다.
        /// </summary>
        public event Action<StageWindow> CloseRequested;

        /// <summary>
        /// 이 창이 가려졌거나 다시 드러났다 — <c>(창, 가려졌나)</c>.
        ///
        /// <para>
        /// <b>우선순위는 «누가 살아남나» 만 정하는 것이 아니었다.</b> 밑에 깔린 창이 화면에
        /// 남아 있으면 그 버튼도 그대로 눌린다 — 실플레이에서 메뉴를 연 채 보상 창의
        /// «건너뛰기» 가 먹혔고, 그 결과 다음 판이 시작되며 멈춤까지 풀렸다. 조정자는
        /// 규칙을 아는 유일한 곳이므로 <b>여기서 알리고</b>, 실제로 끄는 것은 그 창이 한다.
        /// </para>
        /// </summary>
        public event Action<StageWindow, bool> CoverageChanged;

        /// <summary>지금 떠 있는 창 수. 검증용이다.</summary>
        public int OpenCount => _open.Count;

        /// <summary>이 창이 지금 떠 있나.</summary>
        public bool IsOpen(StageWindow window)
        {
            return _open.Contains(window);
        }

        /// <summary>
        /// 이 창이 다른 창에 가려져 있나. <b>가려진 창은 조작할 수 없다.</b>
        ///
        /// <para>
        /// 떠 있는 창 중 <b>우선순위가 가장 높은 것 하나만</b> 조작 대상이다. 겹침이 생기는
        /// 경우가 «보상 위에 하나» 뿐이라 지금은 보상만 가려지지만, 규칙을 그렇게 적으면
        /// 겹치는 창이 하나 늘 때 여기도 함께 고쳐야 한다.
        /// </para>
        /// </summary>
        public bool IsCovered(StageWindow window)
        {
            return _open.Count > 1 && _open.Contains(window) && window != Topmost();
        }

        /// <summary>떠 있는 창 중 가장 위. 하나도 없으면 가장 아래 값을 돌려준다.</summary>
        private StageWindow Topmost()
        {
            var top = Lowest;
            for (var i = 0; i < _open.Count; i++)
            {
                if (i == 0 || _open[i] > top)
                {
                    top = _open[i];
                }
            }

            return top;
        }

        /// <summary>
        /// 가려짐이 바뀐 창에만 알린다.
        ///
        /// <para>
        /// <b>목록이 바뀌는 모든 경로 끝에서 부른다.</b> 여는 쪽만 부르면 위에 있던 창이
        /// 닫혔을 때 밑의 창이 <b>가려진 채로 남아</b> 영영 눌리지 않는다.
        /// </para>
        /// <para>
        /// <b>기록을 먼저 고치고 나중에 알린다</b> — <see cref="Evict"/> 와 같은 규칙이다.
        /// 알리는 도중에 받은 쪽이 창을 여닫으면 순회하던 목록이 바뀐다.
        /// </para>
        /// </summary>
        private void RefreshCoverage()
        {
            // 닫힌 창의 기록을 먼저 지운다. 남겨 두면 다시 열렸을 때 «이미 가려짐» 으로
            // 보여 알림이 빠진다.
            for (var i = _covered.Count - 1; i >= 0; i--)
            {
                if (!_open.Contains(_covered[i]))
                {
                    _covered.RemoveAt(i);
                }
            }

            List<StageWindow> changed = null;
            for (var i = 0; i < _open.Count; i++)
            {
                if (_covered.Contains(_open[i]) != IsCovered(_open[i]))
                {
                    changed ??= new List<StageWindow>();
                    changed.Add(_open[i]);
                }
            }

            if (changed == null)
            {
                return;
            }

            for (var i = 0; i < changed.Count; i++)
            {
                if (_covered.Contains(changed[i]))
                {
                    _covered.Remove(changed[i]);
                }
                else
                {
                    _covered.Add(changed[i]);
                }
            }

            for (var i = 0; i < changed.Count; i++)
            {
                CoverageChanged?.Invoke(changed[i], _covered.Contains(changed[i]));
            }
        }

        /// <summary>
        /// 이 창을 열어도 되는지 묻고, 된다면 자리를 비운다.
        ///
        /// <para>
        /// 이미 떠 있으면 <b>아무것도 밀어내지 않고</b> <c>true</c> 를 돌려준다 — 열려 있는
        /// 창을 다시 그리는 것은 흔한 일이고, 그때마다 밑에 깔린 보상까지 흔들리면 안 된다.
        /// </para>
        /// </summary>
        /// <returns>열어도 되면 <c>true</c>. 더 높은 창에 밀려 못 열면 <c>false</c>.</returns>
        public bool TryOpen(StageWindow window)
        {
            if (_open.Contains(window))
            {
                return true;
            }

            // 가장 아래 창은 남의 위에 겹치지 않는다. 여기서 막지 않으면 정보 창이
            // 메뉴를 밀어내고 열려, 멈춰 놓고 손님을 눌렀을 때 판이 다시 흐른다.
            if (window == Lowest && _open.Count > 0)
            {
                return false;
            }

            // 밑에 깔리는 창은 거절당하지 않는다. 보상은 «닫으면 다음 판» 이라 나중에
            // 다시 열 방법이 없어, 여기서 막으면 클리어 보상이 통째로 사라진다.
            if (window == Underlay)
            {
                _open.Add(window);
                RefreshCoverage();
                return true;
            }

            // 우선순위가 승자를 정한다 — 나중에 연 것이 아니다. 이 줄이 없으면 «메뉴가
            // 떠 있는데 덱 버튼이 메뉴를 밀어낸다» 가 된다.
            if (HasHigherThan(window))
            {
                return false;
            }

            Evict();
            _open.Add(window);
            RefreshCoverage();
            return true;
        }

        /// <summary>
        /// 이 창이 닫혔음을 알린다. 떠 있지 않았으면 아무 일도 하지 않는다 —
        /// <see cref="CloseRequested"/> 를 받고 부르는 경로가 그대로 지나갈 수 있어야 한다.
        /// </summary>
        public void Close(StageWindow window)
        {
            _open.Remove(window);
            RefreshCoverage();
        }

        /// <summary>
        /// 이 창보다 위에 있는 창이 떠 있나. <b>밑에 깔리는 창은 세지 않는다</b> — 보상은
        /// 우선순위가 낮지만 위를 막지 않는 자리라, 세면 그 위에 아무것도 못 열게 된다.
        /// </summary>
        private bool HasHigherThan(StageWindow window)
        {
            for (var i = 0; i < _open.Count; i++)
            {
                if (_open[i] != Underlay && _open[i] > window)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 새로 열릴 창에 자리를 내준다. <b>보상은 남긴다</b> — 그것이 겹침을 허용하는
        /// 유일한 예외다. 부르는 쪽이 이미 «더 높은 창은 없다» 를 확인했으므로 남는 것은
        /// 전부 밀어낼 대상이다.
        ///
        /// <para>
        /// 목록을 <b>먼저 비우고</b> 알린다. 알리는 도중에 목록을 건드리면 순회가 깨지고,
        /// 받은 쪽이 부르는 <see cref="Close"/> 가 방금 넣은 창을 지울 수도 있다.
        /// </para>
        /// </summary>
        private void Evict()
        {
            List<StageWindow> victims = null;
            for (var i = 0; i < _open.Count; i++)
            {
                if (_open[i] == Underlay)
                {
                    continue;
                }

                victims ??= new List<StageWindow>();
                victims.Add(_open[i]);
            }

            if (victims == null)
            {
                return;
            }

            for (var i = 0; i < victims.Count; i++)
            {
                _open.Remove(victims[i]);
            }

            for (var i = 0; i < victims.Count; i++)
            {
                CloseRequested?.Invoke(victims[i]);
            }
        }
    }
}
