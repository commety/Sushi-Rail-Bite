using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SushiDefense.UI
{
    /// <summary>
    /// 인스테이지 메뉴 화면.
    ///
    /// <para>
    /// <b>규칙이 하나도 없다.</b> 멈춰 있는지도 프레젠터가 넘겨 준다 — 화면이 상태를
    /// 저장하면 프레젠터의 것과 어긋날 수 있고, 그때 어느 쪽이 진실인지 알 수 없다.
    /// </para>
    /// <para>
    /// <b>컴포넌트가 패널 바깥에 산다.</b> 메뉴 버튼은 메뉴가 닫혀 있을 때도 눌려야 하므로,
    /// 이 컴포넌트가 붙은 오브젝트는 늘 켜져 있고 <see cref="_panelRoot"/> 만 켜고 끈다
    /// (<c>DeckPanelView</c> 와 같은 형태다).
    /// </para>
    /// </summary>
    public sealed class StageMenuView : MonoBehaviour, IStageMenuView
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string PanelRootName = "Panel";

        private const string TitleLabelName = "StateLabel";

        /// <summary>멈춰서 열린 메뉴의 제목.</summary>
        private const string PausedTitle = "일시정지";

        /// <summary>실패해서 열린 메뉴의 제목.</summary>
        private const string FailedTitle = "실패";

        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;

        private StageMenuPresenter _presenter;

        /// <summary>메뉴가 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>지금 표시 중인 제목 문구. 검증용이다.</summary>
        public string StateText { get; private set; } = string.Empty;

        /// <summary>재개 버튼이 지금 보이나. 검증용이다.</summary>
        public bool IsResumeShown { get; private set; }

        /// <summary>입력을 받을 프레젠터를 물린다. 씬 진입점이 부른다.</summary>
        public void Bind(StageMenuPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <summary>인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.</summary>
        public void Initialize(GameObject panelRoot, TMP_Text titleLabel)
        {
            _panelRoot = panelRoot;
            _titleLabel = titleLabel;
        }

        /// <summary>
        /// 인스펙터 없이 버튼을 물린다. <b>구독을 다시 건다</b> — <see cref="Awake"/> 가
        /// 먼저 돌므로 그때 걸어 둔 구독이 엉뚱한 오브젝트에 남는다
        /// (<c>SettingsView.Initialize</c> 와 같은 이유다).
        /// </summary>
        public void InitializeButtons(Button openButton, Button closeButton, Button resumeButton,
                                      Button restartButton, Button quitButton)
        {
            UnsubscribeAll();

            _openButton = openButton;
            _closeButton = closeButton;
            _resumeButton = resumeButton;
            _restartButton = restartButton;
            _quitButton = quitButton;

            SubscribeAll();
        }

        /// <inheritdoc />
        public void ShowMenu(bool canResume)
        {
            IsShowing = true;
            StateText = canResume ? PausedTitle : FailedTitle;

            HudLabel.Write(_titleLabel, StateText);
            SetResumeActive(canResume);
            SetPanelActive(true);
        }

        /// <inheritdoc />
        public void Hide()
        {
            IsShowing = false;
            StateText = string.Empty;

            HudLabel.Write(_titleLabel, StateText);
            SetPanelActive(false);
        }

        private void Awake()
        {
            if (_panelRoot == null)
            {
                var child = transform.Find(PanelRootName);
                _panelRoot = child != null ? child.gameObject : null;
            }

            _titleLabel = HudLabel.Resolve(_panelRoot != null ? _panelRoot.transform : transform,
                                           _titleLabel, TitleLabelName);

            SubscribeAll();
            Hide();
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        private void SubscribeAll()
        {
            Subscribe(_openButton, OnToggle);
            Subscribe(_closeButton, OnClose);
            Subscribe(_resumeButton, OnResume);
            Subscribe(_restartButton, OnRestart);
            Subscribe(_quitButton, OnQuit);
        }

        private void UnsubscribeAll()
        {
            Unsubscribe(_openButton, OnToggle);
            Unsubscribe(_closeButton, OnClose);
            Unsubscribe(_resumeButton, OnResume);
            Unsubscribe(_restartButton, OnRestart);
            Unsubscribe(_quitButton, OnQuit);
        }

        private static void Subscribe(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void Unsubscribe(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        /// <summary>
        /// 메뉴 아이콘은 <b>토글</b>이다. 열기만 하면 아이콘으로 닫을 수 없어, 같은 자리의
        /// 덱 아이콘과 동작이 갈린다.
        /// </summary>
        private void OnToggle()
        {
            _presenter?.Toggle();
        }

        private void OnClose()
        {
            _presenter?.Close();
        }

        private void OnResume()
        {
            _presenter?.Resume();
        }

        private void OnRestart()
        {
            _presenter?.Restart();
        }

        private void OnQuit()
        {
            _presenter?.QuitToMain();
        }

        private void SetPanelActive(bool active)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(active);
            }
        }

        /// <summary>
        /// 재개 버튼을 켜고 끈다. 버튼이 없으면 <b>보이지 않는 것으로 기록</b>한다 —
        /// 씬에 아직 놓이지 않은 상태와 «감췄다» 를 같게 두면 검증이 둘을 구분하지 못한다.
        /// </summary>
        private void SetResumeActive(bool active)
        {
            IsResumeShown = _resumeButton != null && active;

            if (_resumeButton != null)
            {
                _resumeButton.gameObject.SetActive(active);
            }
        }
    }
}
