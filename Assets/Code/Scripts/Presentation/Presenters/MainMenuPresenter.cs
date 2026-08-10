using System;
using SushiDefense.Navigation;

namespace SushiDefense.UI
{
    /// <summary>
    /// 메인 화면의 로직 — 시작 · 설정 · 백과사전. <b>Unity API 를 모른다</b>
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// 게임에 입구가 없었다. 브라우저를 열면 스테이지가 이미 돌고 있어, 시작하기 전에
    /// 소리를 줄이거나 무엇이 있는지 보는 길이 아예 없었다.
    /// </para>
    /// <para>
    /// <b>씬을 직접 로드하지 않는다.</b> <see cref="ISceneRouter"/> 뒤로 밀어야
    /// <i>"시작을 누르면 스테이지로 간다"</i> 를 씬을 띄우지 않고 EditMode 로 확인할 수 있다.
    /// </para>
    /// <para>
    /// <b>설정·백과사전 패널을 직접 열지 않는다.</b> 두 화면은 step-10 의 것이고, 여기서
    /// 그 타입을 참조하면 메인 화면이 아직 없는 코드에 묶인다 — 요청만 알리고, 무엇을 열지는
    /// 씬 진입점이 물린다 (<c>RewardSelectionPresenter</c> 가 결과를 이벤트로 내보내는 것과
    /// 같은 방향이다).
    /// </para>
    /// </summary>
    public sealed class MainMenuPresenter
    {
        private readonly IMainMenuView _view;
        private readonly ISceneRouter _router;

        /// <summary>메뉴가 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>설정을 열어 달라. 구독자가 없어도 메뉴는 내려간다.</summary>
        public event Action SettingsRequested;

        /// <summary>백과사전을 열어 달라.</summary>
        public event Action CodexRequested;

        public MainMenuPresenter(IMainMenuView view, ISceneRouter router)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        /// <summary>
        /// 메뉴를 띄운다. 패널을 닫고 <b>돌아오는 길</b>이기도 하다.
        ///
        /// <para>
        /// 이미 떠 있으면 다시 그리지 않는다 — 같은 상태를 두 번 그리면 구독자가 같은
        /// 전이를 여러 번 처리한다 (<c>StageMenuPresenter.Pause</c> 와 같은 판단).
        /// </para>
        /// </summary>
        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            _view.ShowMenu();
        }

        /// <summary>
        /// 스테이지로 간다. <b>메뉴를 내리지 않는다</b> — 씬이 통째로 갈리므로 내릴 화면이
        /// 남지 않고, 여기서 내리면 로드가 거부됐을 때 아무것도 없는 화면만 남는다.
        /// </summary>
        public void StartGame()
        {
            _router.LoadStage();
        }

        /// <summary>
        /// 설정을 연다. <b>메뉴를 내린다</b> — 패널 뒤에 남은 버튼은 눈에 보이지 않으면서도
        /// 눌리므로, 설정을 만지다 스테이지가 시작되는 사고가 난다.
        /// </summary>
        public void OpenSettings()
        {
            HideForPanel();
            SettingsRequested?.Invoke();
        }

        /// <summary>백과사전을 연다. 설정과 같은 이유로 메뉴를 내린다.</summary>
        public void OpenCodex()
        {
            HideForPanel();
            CodexRequested?.Invoke();
        }

        private void HideForPanel()
        {
            IsOpen = false;
            _view.Hide();
        }
    }
}
