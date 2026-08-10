using System;
using SushiDefense.Navigation;
using SushiDefense.Stages;

namespace SushiDefense.UI
{
    /// <summary>
    /// 인스테이지 메뉴 — 재개 · 다시 시작 · 메인으로. <b>Unity API 를 모른다</b>
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// 스테이지에 출구가 없었다. 시작하면 실패하거나 클리어할 때까지 나갈 수 없고 멈출 수도
    /// 없어, 웹 데모에서는 새로고침 말고 방법이 없었다.
    /// </para>
    /// <para>
    /// <b>«멈춤» 버튼이 없다.</b> 메뉴를 여는 것이 곧 멈추는 것이고 닫는 것이 곧 재개다 —
    /// 열린 메뉴 안에 다시 «멈춤/재개» 를 두면 «메뉴는 열려 있는데 시간은 흐르는» 네 번째
    /// 상태가 생기고, 플레이어는 그 상태를 만들 이유가 없다. 상태가 둘이면 라벨도 하나면 된다.
    /// </para>
    /// <para>
    /// <b>시간을 직접 멈추지 않는다.</b> <see cref="PauseState"/> 를 뒤집을 뿐이고, 실제로
    /// 시간을 흘릴지는 씬 진입점이 그 값을 보고 정한다 — 전역 시간 배율을 쓰지 않는 이유는
    /// <c>PauseState</c> 의 클래스 주석에 있다.
    /// </para>
    /// <para>
    /// <b>실패도 이 창이 받는다.</b> 실패했을 때 필요한 것은 «다시 시작» 과 «나가기» 인데
    /// 둘 다 이미 여기 있다. 전용 화면을 새로 만들면 같은 버튼 둘이 두 곳에 살고,
    /// <see cref="StageWindowArbiter"/> 가 조정할 창도 하나 더 는다. 다른 점은
    /// <b>재개할 수 없다</b> 는 것뿐이라 그것만 인자로 넘긴다.
    /// </para>
    /// </summary>
    public sealed class StageMenuPresenter
    {
        private readonly IStageMenuView _view;
        private readonly PauseState _pause;
        private readonly IStageRestarter _restarter;
        private readonly ISceneRouter _router;
        private readonly StageWindowArbiter _windows;

        /// <summary>메뉴가 떠 있나.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 지금 떠 있는 메뉴에서 판으로 돌아갈 수 있나. 실패해서 열린 메뉴는 <c>false</c> 다.
        ///
        /// <para>
        /// <b>닫을 때 되돌리지 않아도 되게</b> 여는 쪽에서만 쓴다 — 닫힌 메뉴의 값은
        /// 아무도 읽지 않는다.
        /// </para>
        /// </summary>
        public bool CanResume { get; private set; } = true;

        /// <summary>
        /// 설정을 열어 달라. <b>이걸 듣는 쪽이 무엇을 열지 정한다.</b>
        ///
        /// <para>
        /// 메뉴가 설정 화면을 직접 알면 EditMode 로 메뉴를 세울 때마다 설정까지 만들어야
        /// 한다 — <c>MainMenuPresenter.SettingsRequested</c> 와 같은 방향 전환이다.
        /// </para>
        /// </summary>
        public event Action SettingsRequested;

        public StageMenuPresenter(IStageMenuView view, PauseState pause,
                                  IStageRestarter restarter, ISceneRouter router,
                                  StageWindowArbiter windows)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _restarter = restarter ?? throw new ArgumentNullException(nameof(restarter));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _windows = windows ?? throw new ArgumentNullException(nameof(windows));
        }

        /// <summary>
        /// 메뉴를 연다. <b>여는 순간 멈춘다</b> — 메뉴를 보는 동안 벨트가 흐르면 메뉴가 곧
        /// 페널티가 된다.
        /// </summary>
        public void Open()
        {
            Open(true);
        }

        /// <summary>
        /// 판을 못 깼을 때 연다. <b>재개 버튼이 빠진다</b> — 실패한 판으로 돌아갈 곳이 없다.
        /// 남는 출구는 다시 시작과 나가기 둘이고, 그 둘은 이미 이 창에 있다.
        /// </summary>
        public void OpenAfterFailure()
        {
            Open(false);
        }

        /// <summary>
        /// 메뉴를 닫는다. <b>닫으면 다시 흐른다.</b>
        ///
        /// <para>
        /// 실패해서 열린 메뉴도 이 경로로 닫힌다 — 다시 시작이 판을 새로 세우기 전에
        /// 창을 내려야 하기 때문이다. 그때 재개되는 것은 이미 끝난 판이라 아무 일도 없다.
        /// </para>
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            _windows.Close(StageWindow.Menu);
            _pause.Resume();
            _view.Hide();
        }

        /// <summary>
        /// 메뉴 아이콘이 부른다. 열려 있으면 닫고 닫혀 있으면 연다.
        ///
        /// <para>
        /// 아이콘이 여는 일만 하면 <b>메뉴를 아이콘으로 닫을 수 없다.</b> 덱 버튼이 이미
        /// 토글이라, 같은 자리의 두 아이콘이 다르게 동작하면 어느 쪽이 규칙인지 알 수 없다.
        /// </para>
        /// <para>
        /// <b>실패로 열린 창은 예외다 — 아이콘이 아무 일도 하지 않는다.</b> 닫으면 실패한
        /// 판만 남고 <b>출구가 함께 사라진다</b>: 이 창의 다시 시작·나가기가 그 판에서
        /// 유일하게 남은 두 길이다. 다시 열어도 실패했다는 사실을 모르는 <see cref="Open()"/>
        /// 를 지나 «재개» 가 붙은 일시정지가 되어, 이미 끝난 판으로 돌아가는 버튼이 생긴다.
        /// 어차피 다시 시작하거나 나갈 판이므로 토글할 이유 자체가 없다.
        /// </para>
        /// </summary>
        public void Toggle()
        {
            if (IsOpen && !CanResume)
            {
                return;
            }

            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>
        /// 판으로 돌아간다. <b>메뉴가 닫힌다</b> — 메뉴가 곧 멈춤이므로, 열어 둔 채 시간만
        /// 흘리는 상태를 만들지 않는다.
        /// </summary>
        public void Resume()
        {
            Close();
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
        /// <summary>
        /// 설정을 열어 달라. <b>메뉴는 그대로 둔다</b> — 설정은 메뉴 위에 겹쳐 뜨고,
        /// 닫으면 메뉴로 돌아온다. 무엇을 여는지는 씬 진입점이 정한다
        /// (<c>MainMenuPresenter.SettingsRequested</c> 와 같은 형태다).
        /// </summary>
        public void OpenSettings()
        {
            SettingsRequested?.Invoke();
        }

        public void QuitToMain()
        {
            Close();
            _router.LoadMain();
        }

        /// <summary>
        /// 여는 유일한 경로. <b>조정자가 거절하면 열지 않는다</b> — 더 높은 창이 떠 있는데
        /// 열면 두 패널이 겹쳐 글자가 서로를 뚫는다.
        /// </summary>
        private void Open(bool canResume)
        {
            if (!_windows.TryOpen(StageWindow.Menu))
            {
                return;
            }

            IsOpen = true;
            CanResume = canResume;
            _pause.Pause();
            _view.ShowMenu(canResume);
        }
    }
}
