using System;
using SushiDefense.Navigation;
using SushiDefense.Stages;

namespace SushiDefense.UI
{
    /// <summary>
    /// 인스테이지 메뉴 — 멈추기 · 재개 · 다시 시작 · 메인으로. <b>Unity API 를 모른다</b>
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// 스테이지에 출구가 없었다. 시작하면 실패하거나 클리어할 때까지 나갈 수 없고 멈출 수도
    /// 없어, 웹 데모에서는 새로고침 말고 방법이 없었다.
    /// </para>
    /// <para>
    /// <b>시간을 직접 멈추지 않는다.</b> <see cref="PauseState"/> 를 뒤집을 뿐이고, 실제로
    /// 시간을 흘릴지는 씬 진입점이 그 값을 보고 정한다 — 전역 시간 배율을 쓰지 않는 이유는
    /// <c>PauseState</c> 의 클래스 주석에 있다.
    /// </para>
    /// </summary>
    public sealed class StageMenuPresenter
    {
        private readonly IStageMenuView _view;
        private readonly PauseState _pause;
        private readonly IStageRestarter _restarter;
        private readonly ISceneRouter _router;

        /// <summary>메뉴가 떠 있나.</summary>
        public bool IsOpen { get; private set; }

        public StageMenuPresenter(IStageMenuView view, PauseState pause,
                                  IStageRestarter restarter, ISceneRouter router)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _restarter = restarter ?? throw new ArgumentNullException(nameof(restarter));
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        /// <summary>
        /// 메뉴를 연다. <b>여는 순간 멈춘다</b> — 메뉴를 보는 동안 벨트가 흐르면 메뉴가 곧
        /// 페널티가 된다. 다시 흘려 보고 싶을 때 쓰는 것이 <see cref="Resume"/> 다.
        /// </summary>
        public void Open()
        {
            IsOpen = true;
            _pause.Pause();
            _view.ShowMenu(_pause.IsPaused);
        }

        /// <summary>메뉴를 닫는다. <b>닫으면 다시 흐른다.</b></summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            _pause.Resume();
            _view.Hide();
        }

        /// <summary>
        /// 멈춘다. 이미 멈춰 있으면 <b>화면을 다시 그리지 않는다</b> — 같은 상태를 두 번
        /// 그리면 구독자가 같은 전이를 여러 번 처리한다.
        /// </summary>
        public void Pause()
        {
            if (_pause.IsPaused)
            {
                return;
            }

            _pause.Pause();
            _view.ShowMenu(_pause.IsPaused);
        }

        /// <summary>
        /// 다시 흐르게 한다. <b>메뉴는 열어 둔다</b> — 닫아 버리면 다시 멈출 방법이 없다.
        /// </summary>
        public void Resume()
        {
            if (!_pause.IsPaused)
            {
                return;
            }

            _pause.Resume();
            _view.ShowMenu(_pause.IsPaused);
        }

        /// <summary>
        /// 같은 판을 다시 시작한다. <b>메뉴를 먼저 내린다</b> — 판을 다시 세우는 동안
        /// 메뉴가 떠 있으면 새 판 위에 겹쳐 남는다 (<c>StageTransitionPresenter.Proceed</c>
        /// 가 같은 사고를 이미 막아 둔 자리다).
        /// </summary>
        public void Restart()
        {
            Close();
            _restarter.Restart();
        }

        /// <summary>
        /// 메인 화면으로 나간다. <b>런은 버려진다</b> — 저장·이어하기는 M6 의 범위가 아니다.
        ///
        /// <para>
        /// 확인 절차를 두지 않는다. 화면이 하나 더 느는 일이고 요구에 없다 — 진행이
        /// 아깝다는 판단이 서면 그때 넣는다.
        /// </para>
        /// </summary>
        public void QuitToMain()
        {
            Close();
            _router.LoadMain();
        }
    }
}
