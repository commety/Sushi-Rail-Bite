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

        private const string StateLabelName = "StateLabel";

        private const string PausedText = "일시정지";

        private const string RunningText = "진행 중";

        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _stateLabel;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;

        private StageMenuPresenter _presenter;

        /// <summary>메뉴가 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>지금 표시 중인 상태 문구. 검증용이다.</summary>
        public string StateText { get; private set; } = string.Empty;

        /// <summary>입력을 받을 프레젠터를 물린다. 씬 진입점이 부른다.</summary>
        public void Bind(StageMenuPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <summary>인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.</summary>
        public void Initialize(GameObject panelRoot, TMP_Text stateLabel)
        {
            _panelRoot = panelRoot;
            _stateLabel = stateLabel;
        }

        /// <inheritdoc />
        public void ShowMenu(bool paused)
        {
            IsShowing = true;
            StateText = paused ? PausedText : RunningText;

            HudLabel.Write(_stateLabel, StateText);
            SetPanelActive(true);
        }

        /// <inheritdoc />
        public void Hide()
        {
            IsShowing = false;
            StateText = string.Empty;

            HudLabel.Write(_stateLabel, StateText);
            SetPanelActive(false);
        }

        private void Awake()
        {
            if (_panelRoot == null)
            {
                var child = transform.Find(PanelRootName);
                _panelRoot = child != null ? child.gameObject : null;
            }

            _stateLabel = HudLabel.Resolve(_panelRoot != null ? _panelRoot.transform : transform,
                                           _stateLabel, StateLabelName);

            Subscribe(_openButton, OnOpen);
            Subscribe(_closeButton, OnClose);
            Subscribe(_pauseButton, OnPause);
            Subscribe(_resumeButton, OnResume);
            Subscribe(_restartButton, OnRestart);
            Subscribe(_quitButton, OnQuit);

            Hide();
        }

        private void OnDestroy()
        {
            Unsubscribe(_openButton, OnOpen);
            Unsubscribe(_closeButton, OnClose);
            Unsubscribe(_pauseButton, OnPause);
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

        private void OnOpen()
        {
            _presenter?.Open();
        }

        private void OnClose()
        {
            _presenter?.Close();
        }

        private void OnPause()
        {
            _presenter?.Pause();
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
    }
}
