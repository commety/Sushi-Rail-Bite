using System;
using SushiDefense.Settings;

namespace SushiDefense.UI
{
    /// <summary>
    /// 설정 화면의 로직 — 마스터 볼륨과 전체화면. <b>Unity API 를 모른다</b>
    /// (<c>CLAUDE.md</c> §3.6).
    ///
    /// <para>
    /// <b>적용은 즉시, 저장은 닫을 때.</b> 슬라이더를 끄는 동안 매 프레임 저장하면
    /// 저장 장치 쓰기가 프레임마다 돈다 — 소리는 바로 들려야 하고 디스크는 그럴 필요가
    /// 없다. 어디에 남기는지는 <see cref="ISettingsStore"/> 뒤의 구현만 안다.
    /// </para>
    /// <para>
    /// <b>세워질 때 한 번 복원한다.</b> 볼륨은 설정 화면을 열기 <i>전부터</i> 맞아 있어야
    /// 하므로, 복원을 화면 여는 시점으로 미룰 수 없다. 반대로 열 때마다 다시 읽지도
    /// 않는다 — 적용해 두고 아직 저장하지 않은 값이 되돌아가, 화면과 실제 소리가 어긋난다.
    /// </para>
    /// </summary>
    public sealed class SettingsPresenter
    {
        private readonly ISettingsView _view;
        private readonly GameSettings _settings;
        private readonly ISettingsStore _store;
        private readonly ISettingsApplier _applier;

        /// <summary>화면이 떠 있나.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// 화면이 닫혔다. <b>이걸 듣는 쪽이 메인 메뉴를 다시 연다.</b>
        ///
        /// <para>
        /// <c>MainMenuPresenter.OpenSettings</c> 가 메뉴를 내리므로, 닫힘을 알리지 않으면
        /// 메뉴가 영영 돌아오지 않고 <b>빈 화면에 갇힌다.</b> 프레젠터가 서로를 직접 알면
        /// 둘 다 EditMode 로 세우기 어려워지므로 이벤트로 뒤집는다
        /// (<c>RewardSelectionPresenter.Closed</c> 와 같은 형태다).
        /// </para>
        /// </summary>
        public event Action Closed;

        public SettingsPresenter(ISettingsView view, GameSettings settings,
                                 ISettingsStore store, ISettingsApplier applier)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _applier = applier ?? throw new ArgumentNullException(nameof(applier));

            _store.Load(_settings);
            _applier.Apply(_settings);
        }

        /// <summary>
        /// 설정을 연다. 이미 떠 있으면 다시 그리지 않는다 — 같은 상태를 두 번 그리면
        /// 슬라이더가 조작 중에 제자리로 튄다.
        /// </summary>
        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            Show();
        }

        /// <summary>
        /// 설정을 닫으며 저장한다. <b>이미 닫혀 있으면 저장하지 않는다</b> — 닫기가 두 번
        /// 배달돼도 쓰기는 한 번이다.
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            _store.Save(_settings);
            _view.Hide();
            Closed?.Invoke();
        }

        /// <summary>
        /// 볼륨을 정한다. <b>즉시 들린다.</b> 화면에는 모델이 자른 뒤의 값이 올라가므로,
        /// 슬라이더 위치와 실제 볼륨이 갈리지 않는다.
        /// </summary>
        public void SetMasterVolume(float value)
        {
            _settings.SetMasterVolume(value);
            ApplyAndShow();
        }

        /// <summary>
        /// 전체화면을 정한다. <b>화면에는 실제로 그렇게 됐는지를 표시한다</b> — 브라우저가
        /// 거부해도 토글만 켜지면 플레이어가 무엇이 참인지 알 수 없다.
        /// </summary>
        public void SetFullscreen(bool value)
        {
            _settings.SetFullscreen(value);
            ApplyAndShow();
        }

        private void ApplyAndShow()
        {
            _applier.Apply(_settings);

            if (IsOpen)
            {
                Show();
            }
        }

        private void Show()
        {
            _view.ShowSettings(_settings.MasterVolume, _applier.IsFullscreen);
        }
    }
}
