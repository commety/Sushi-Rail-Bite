using SushiDefense.Data;
using SushiDefense.Settings;
using SushiDefense.UI;
using UnityEngine;

namespace SushiDefense
{
    /// <summary>
    /// 메인 씬의 진입점. 세 화면을 세우고 <b>서로 오가는 길만</b> 잇는다.
    ///
    /// <para>
    /// 이 컴포넌트가 필요한 이유는 <see cref="MainMenuPresenter"/> 가 설정·백과사전을
    /// <b>모르기 때문</b>이다. 알게 하면 메인 화면이 두 패널에 묶여 EditMode 로 세울 때마다
    /// 셋을 다 만들어야 한다 — 그래서 메인은 «열어 달라» 는 요청만 내보내고, 무엇을 열지는
    /// 여기서 정한다 (<c>StageBootstrap</c> 이 스테이지에서 하는 일과 같다).
    /// </para>
    /// <para>
    /// <b>닫히면 메뉴를 다시 연다.</b> 패널을 여는 쪽이 메뉴를 내리므로, 돌아오는 길을
    /// 잇지 않으면 플레이어가 빈 화면에 갇힌다.
    /// </para>
    /// <para>
    /// <b>판정을 하나도 하지 않는다.</b> 무엇을 보여줄지·언제 저장할지는 프레젠터들이 이미
    /// 답한다 — 여기서 되물으면 판정이 두 곳에 살게 된다 (<c>CLAUDE.md</c> §3.2).
    /// </para>
    /// </summary>
    public sealed class MainBootstrap : MonoBehaviour
    {
        [SerializeField] private MainMenuView _menuView;
        [SerializeField] private SettingsView _settingsView;
        [SerializeField] private CodexView _codexView;
        [SerializeField] private CardCatalog _catalog;

        private SettingsPresenter _settings;
        private CodexPresenter _codex;

        /// <summary>설정 화면의 로직. 검증용이다.</summary>
        public SettingsPresenter Settings => _settings;

        /// <summary>백과사전의 로직. 검증용이다.</summary>
        public CodexPresenter Codex => _codex;

        /// <summary>인스펙터 없이 참조를 물린다. 테스트·씬 조립용 진입점이다.</summary>
        public void Initialize(MainMenuView menuView, SettingsView settingsView,
                               CodexView codexView, CardCatalog catalog)
        {
            _menuView = menuView;
            _settingsView = settingsView;
            _codexView = codexView;
            _catalog = catalog;

            Build();
        }

        /// <summary>
        /// <see cref="MainMenuView"/> 가 <c>Start</c> 에서 자기 프레젠터를 세우므로 여기도
        /// <c>Start</c> 다. <c>Awake</c> 에서 잡으면 아직 없는 것을 잡는다.
        /// </summary>
        private void Start()
        {
            if (_settings == null && _codex == null)
            {
                Build();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Build()
        {
            Unsubscribe();

            if (_settingsView != null)
            {
                _settings = new SettingsPresenter(_settingsView, new GameSettings(),
                                                  new PlayerPrefsSettingsStore(),
                                                  new SettingsApplier());
                _settingsView.Bind(_settings);
                _settings.Closed += OnPanelClosed;
            }

            if (_codexView != null && _catalog != null)
            {
                _codex = new CodexPresenter(_codexView, _catalog);
                _codexView.Bind(_codex);
                _codex.Closed += OnPanelClosed;
            }

            var menu = _menuView != null ? _menuView.Presenter : null;
            if (menu != null)
            {
                menu.SettingsRequested += OnSettingsRequested;
                menu.CodexRequested += OnCodexRequested;
            }
        }

        private void Unsubscribe()
        {
            if (_settings != null)
            {
                _settings.Closed -= OnPanelClosed;
            }

            if (_codex != null)
            {
                _codex.Closed -= OnPanelClosed;
            }

            var menu = _menuView != null ? _menuView.Presenter : null;
            if (menu != null)
            {
                menu.SettingsRequested -= OnSettingsRequested;
                menu.CodexRequested -= OnCodexRequested;
            }
        }

        private void OnSettingsRequested()
        {
            _settings?.Open();
        }

        private void OnCodexRequested()
        {
            _codex?.Open();
        }

        /// <summary>둘 다 같은 곳으로 돌아온다 — 어느 패널이 닫혔는지 구분할 이유가 없다.</summary>
        private void OnPanelClosed()
        {
            if (_menuView != null && _menuView.Presenter != null)
            {
                _menuView.Presenter.Open();
            }
        }
    }
}
