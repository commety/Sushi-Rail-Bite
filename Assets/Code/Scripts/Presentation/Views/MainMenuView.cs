using SushiDefense.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SushiDefense.UI
{
    /// <summary>
    /// 메인 화면 — 제목과 버튼 셋.
    ///
    /// <para>
    /// <b>규칙이 하나도 없다.</b> 어디로 갈지는 <see cref="MainMenuPresenter"/> 가 정한다 —
    /// 여기서 씬을 로드하면 그 판단을 EditMode 로 확인할 수 없다 (<c>CLAUDE.md</c> §3.2·§3.6).
    /// </para>
    /// <para>
    /// <b>프레젠터가 없으면 스스로 세운다.</b> 메인 씬에는 아직 진입점이 없고, 이 화면이
    /// 열리지 않으면 게임에 입구가 없다. 그 폴백은 참조를 물려 주는 테스트가 지나치므로
    /// (<c>.claude/rules/tests.md</c> §1 «직접 주입으로 우회되는 폴백»),
    /// <b>물리지 않은 채로 뜨는 경로</b>를 보는 테스트를 따로 둔다.
    /// </para>
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string TitleLabelName = "TitleLabel";

        /// <summary>제목 문구. 로고 스프라이트를 쓰면 라벨을 비워 두면 된다.</summary>
        private const string TitleText = "스시 레일 바이트";

        [SerializeField] private GameObject _menuRoot;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _codexButton;

        /// <summary>씬 전환의 유일한 구현. 인스펙터에서 물린다.</summary>
        [SerializeField] private SceneRouter _router;

        private MainMenuPresenter _presenter;

        /// <summary>메뉴가 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>지금 표시 중인 제목 문구. 검증용이다.</summary>
        public string TitleShown { get; private set; } = string.Empty;

        /// <summary>이 화면이 물고 있는 프레젠터. 설정·백과사전이 여기 구독한다.</summary>
        public MainMenuPresenter Presenter => _presenter;

        /// <summary>입력을 받을 프레젠터를 물린다. 씬 진입점이 부른다.</summary>
        public void Bind(MainMenuPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <summary>
        /// 인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.
        ///
        /// <para>
        /// <b>구독을 다시 건다.</b> <see cref="Awake"/> 가 먼저 돌므로, 여기서 버튼을 갈아
        /// 끼우면 그때 걸어 둔 구독이 엉뚱한 오브젝트에 남는다 — 새 버튼은 눌러도 아무 일이
        /// 없고, 화면에는 버튼이 멀쩡히 보이므로 눈으로 구분되지 않는다.
        /// </para>
        /// </summary>
        public void Initialize(GameObject menuRoot, TMP_Text titleLabel,
                               Button startButton, Button settingsButton, Button codexButton)
        {
            Unsubscribe();

            _menuRoot = menuRoot;
            _titleLabel = titleLabel;
            _startButton = startButton;
            _settingsButton = settingsButton;
            _codexButton = codexButton;

            Subscribe();
        }

        /// <inheritdoc />
        public void ShowMenu()
        {
            IsShowing = true;
            TitleShown = TitleText;

            HudLabel.Write(_titleLabel, TitleShown);
            SetMenuActive(true);
        }

        /// <inheritdoc />
        public void Hide()
        {
            IsShowing = false;

            // 제목 문구는 지우지 않는다 — 패널이 덮을 뿐이고, 뒤에 남은 제목은 가려진다.
            SetMenuActive(false);
        }

        private void Awake()
        {
            if (_menuRoot == null)
            {
                _menuRoot = gameObject;
            }

            _titleLabel = HudLabel.Resolve(transform, _titleLabel, TitleLabelName);

            Subscribe();
        }

        /// <summary>
        /// 아무도 물려 주지 않았으면 여기서 세운다. <b>메인 화면은 씬이 열리는 순간
        /// 떠 있어야 한다</b> — 열리지 않으면 게임에 입구가 없다.
        /// </summary>
        private void Start()
        {
            if (_presenter == null && _router != null)
            {
                _presenter = new MainMenuPresenter(this, _router);
            }

            _presenter?.Open();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            AddListener(_startButton, OnStart);
            AddListener(_settingsButton, OnSettings);
            AddListener(_codexButton, OnCodex);
        }

        private void Unsubscribe()
        {
            RemoveListener(_startButton, OnStart);
            RemoveListener(_settingsButton, OnSettings);
            RemoveListener(_codexButton, OnCodex);
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        private void OnStart()
        {
            _presenter?.StartGame();
        }

        private void OnSettings()
        {
            _presenter?.OpenSettings();
        }

        private void OnCodex()
        {
            _presenter?.OpenCodex();
        }

        private void SetMenuActive(bool active)
        {
            // 자기 자신이 뿌리면 끄지 않는다 — 꺼 버리면 다시 켤 주체가 사라진다.
            if (_menuRoot != null && _menuRoot != gameObject)
            {
                _menuRoot.SetActive(active);
                return;
            }

            SetButtonsActive(active);
        }

        private void SetButtonsActive(bool active)
        {
            SetActive(_startButton, active);
            SetActive(_settingsButton, active);
            SetActive(_codexButton, active);
        }

        private static void SetActive(Button button, bool active)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }
    }
}
