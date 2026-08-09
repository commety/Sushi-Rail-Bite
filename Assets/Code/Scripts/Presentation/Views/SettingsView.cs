using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SushiDefense.UI
{
    /// <summary>
    /// 설정 화면 — 마스터 볼륨 슬라이더와 전체화면 토글.
    ///
    /// <para>
    /// <b>규칙이 하나도 없다.</b> 값을 자르는 것도 저장 시점도
    /// <see cref="SettingsPresenter"/> 가 정한다 (<c>CLAUDE.md</c> §3.2·§3.6).
    /// </para>
    /// <para>
    /// <b>되돌려 그릴 때는 알림 없이 쓴다</b> (<c>SetValueWithoutNotify</c>). 그냥 대입하면
    /// 슬라이더가 다시 콜백을 쏘고, 그 콜백이 또 화면을 그려 무한히 돈다 — 값이 잘리는
    /// 구간(1 을 넘겨 흔들 때)에서 특히 잘 드러난다.
    /// </para>
    /// <para>
    /// <b>헤더는 고정이고 내용만 스크롤한다</b> (원문 §M6). 지금은 항목이 둘뿐이라 스크롤이
    /// 필요 없지만, 헤더를 나중에 얹으면 레이아웃을 다시 짜야 한다.
    /// </para>
    /// </summary>
    public sealed class SettingsView : MonoBehaviour, ISettingsView
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string PanelRootName = "Panel";

        private const string HeaderLabelName = "SettingsHeader";

        private const string VolumeLabelName = "VolumeLabel";

        private const string BgmLabelName = "BgmLabel";

        private const string SfxLabelName = "SfxLabel";

        /// <summary>패널 위쪽 고정 제목.</summary>
        private const string HeaderText = "설정";

        private const string VolumeCaption = "전체";

        private const string BgmCaption = "배경음";

        private const string SfxCaption = "효과음";

        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _headerLabel;
        [SerializeField] private TMP_Text _volumeLabel;
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private TMP_Text _bgmLabel;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private TMP_Text _sfxLabel;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private Button _closeButton;

        private SettingsPresenter _presenter;

        /// <summary>화면이 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>지금 표시 중인 전체 볼륨 문구. 검증용이다.</summary>
        public string VolumeText { get; private set; } = string.Empty;

        /// <summary>지금 표시 중인 배경음 문구. 검증용이다.</summary>
        public string BgmText { get; private set; } = string.Empty;

        /// <summary>지금 표시 중인 효과음 문구. 검증용이다.</summary>
        public string SfxText { get; private set; } = string.Empty;

        /// <summary>입력을 받을 프레젠터를 물린다. 씬 진입점이 부른다.</summary>
        public void Bind(SettingsPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <summary>
        /// 인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.
        ///
        /// <para>
        /// <b>구독을 다시 건다.</b> <see cref="Awake"/> 가 먼저 돌므로, 여기서 참조를 갈아
        /// 끼우면 그때 걸어 둔 구독이 엉뚱한 오브젝트에 남는다 — 새 슬라이더는 움직여도
        /// 아무 일이 없고, 화면에는 멀쩡히 보이므로 눈으로 구분되지 않는다.
        /// </para>
        /// </summary>
        public void Initialize(GameObject panelRoot, TMP_Text volumeLabel,
                               Slider volumeSlider, Toggle fullscreenToggle, Button closeButton,
                               TMP_Text bgmLabel = null, Slider bgmSlider = null,
                               TMP_Text sfxLabel = null, Slider sfxSlider = null)
        {
            Unsubscribe();

            _panelRoot = panelRoot;
            _volumeLabel = volumeLabel;
            _volumeSlider = volumeSlider;
            _bgmLabel = bgmLabel;
            _bgmSlider = bgmSlider;
            _sfxLabel = sfxLabel;
            _sfxSlider = sfxSlider;
            _fullscreenToggle = fullscreenToggle;
            _closeButton = closeButton;

            Subscribe();
        }

        /// <inheritdoc />
        public void ShowSettings(float masterVolume, float bgmVolume, float sfxVolume,
                                 bool fullscreen)
        {
            IsShowing = true;

            SetSlider(_volumeSlider, masterVolume);
            SetSlider(_bgmSlider, bgmVolume);
            SetSlider(_sfxSlider, sfxVolume);

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            }

            VolumeText = Caption(VolumeCaption, masterVolume);
            BgmText = Caption(BgmCaption, bgmVolume);
            SfxText = Caption(SfxCaption, sfxVolume);

            HudLabel.Write(_volumeLabel, VolumeText);
            HudLabel.Write(_bgmLabel, BgmText);
            HudLabel.Write(_sfxLabel, SfxText);
            SetPanelActive(true);
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        /// <inheritdoc />
        public void Hide()
        {
            IsShowing = false;
            SetPanelActive(false);
        }

        private void Awake()
        {
            if (_panelRoot == null)
            {
                var child = transform.Find(PanelRootName);
                _panelRoot = child != null ? child.gameObject : null;
            }

            var labelRoot = _panelRoot != null ? _panelRoot.transform : transform;
            _headerLabel = HudLabel.Resolve(labelRoot, _headerLabel, HeaderLabelName);
            _volumeLabel = HudLabel.Resolve(labelRoot, _volumeLabel, VolumeLabelName);
            _bgmLabel = HudLabel.Resolve(labelRoot, _bgmLabel, BgmLabelName);
            _sfxLabel = HudLabel.Resolve(labelRoot, _sfxLabel, SfxLabelName);

            // 헤더는 고정 문구라 한 번만 쓴다. 열 때마다 쓰면 문자열이 하나씩 생긴다 (§4.3).
            HudLabel.Write(_headerLabel, HeaderText);

            Subscribe();
            Hide();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>
        /// 브라우저가 스스로 전체화면을 빠져나오는 경우(<c>Esc</c>)를 잡는다. 그 경로는
        /// 콜백이 없어 <b>물어보는 수밖에 없다.</b>
        ///
        /// <para>
        /// 패널이 닫혀 있어도 돈다. 닫힌 동안 창으로 돌아왔는데 모델이 «전체화면» 인 채로
        /// 남으면, 다음에 설정을 열었을 때 토글이 거짓을 말한다.
        /// </para>
        /// </summary>
        private void Update()
        {
            _presenter?.SyncFullscreen();
        }

        private void Subscribe()
        {
            if (_volumeSlider != null)
            {
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            if (_bgmSlider != null)
            {
                _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void Unsubscribe()
        {
            if (_volumeSlider != null)
            {
                _volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            }

            if (_bgmSlider != null)
            {
                _bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            }

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void OnVolumeChanged(float value)
        {
            _presenter?.SetMasterVolume(value);
        }

        private void OnBgmChanged(float value)
        {
            _presenter?.SetBgmVolume(value);
        }

        private void OnSfxChanged(float value)
        {
            _presenter?.SetSfxVolume(value);
        }

        private void OnFullscreenChanged(bool value)
        {
            _presenter?.SetFullscreen(value);
        }

        private void OnCloseClicked()
        {
            _presenter?.Close();
        }

        /// <summary>
        /// 백분율로 보여 준다. <c>0.7</c> 보다 <c>70%</c> 가 슬라이더 위치와 대조하기 쉽다.
        /// </summary>
        private static string Caption(string name, float volume)
        {
            var percent = Mathf.RoundToInt(volume * 100f);
            return name + " " + percent.ToString(CultureInfo.InvariantCulture) + "%";
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
