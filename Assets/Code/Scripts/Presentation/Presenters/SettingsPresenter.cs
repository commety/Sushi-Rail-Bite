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

        /// <summary>
        /// 엔진 쪽 전체화면 값을 <b>마지막으로 봤을 때의 상태</b>.
        ///
        /// <para>
        /// 이것을 두지 않고 매번 엔진 값을 그대로 화면에 쓰면 <b>토글을 두 번 눌러야
        /// 한 번 바뀐다.</b> 브라우저의 전체화면 전환은 비동기라, 누른 그 프레임에는
        /// <c>Screen.fullScreen</c> 이 아직 옛 값이다 — 그 값을 되읽어 그리면 방금 켠
        /// 토글이 곧바로 꺼진 모습으로 되돌아간다.
        /// </para>
        /// <para>
        /// 그래서 <b>엔진 값이 달라진 순간에만</b> 반영한다. 우리가 시킨 변화는 이미
        /// 모델에 있으므로 아무 일도 일어나지 않고, <c>Esc</c> 로 빠져나오는 것처럼
        /// <b>우리가 시키지 않은</b> 변화만 화면으로 올라온다.
        /// </para>
        /// </summary>
        private bool _observedFullscreen;

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
            _observedFullscreen = _applier.IsFullscreen;
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
        /// 전체화면을 정한다. <b>화면에는 고른 값을 그대로 표시한다</b> — 엔진 값을 되읽으면
        /// 비동기 전환 때문에 토글이 제자리로 튄다 (<see cref="_observedFullscreen"/>).
        /// </summary>
        public void SetFullscreen(bool value)
        {
            _settings.SetFullscreen(value);
            ApplyAndShow();
        }

        /// <summary>
        /// 엔진 쪽에서 <b>우리가 시키지 않은</b> 전체화면 변화가 있었는지 확인한다.
        /// 브라우저에서 <c>Esc</c> 로 빠져나오는 경로가 그것이며, 이걸 보지 않으면
        /// 창으로 돌아온 뒤에도 토글이 켜진 채로 남는다.
        ///
        /// <para>
        /// <b>매 프레임 불러도 된다.</b> <c>bool</c> 하나를 읽어 비교할 뿐이고, 값이 실제로
        /// 달라진 프레임에만 화면을 다시 그린다 (§4.3).
        /// </para>
        /// </summary>
        /// <returns>화면에 반영할 변화가 있었으면 <c>true</c>.</returns>
        public bool SyncFullscreen()
        {
            var actual = _applier.IsFullscreen;
            if (actual == _observedFullscreen)
            {
                return false;
            }

            _observedFullscreen = actual;

            if (actual == _settings.Fullscreen)
            {
                return false;
            }

            // 모델만 맞춘다. 여기서 Apply 를 부르면 방금 브라우저가 바꾼 것을 되돌리게 된다.
            _settings.SetFullscreen(actual);

            if (IsOpen)
            {
                Show();
            }

            return true;
        }

        private void ApplyAndShow()
        {
            _applier.Apply(_settings);

            // 방금 우리가 시킨 결과를 «본 것» 으로 기록한다. 이걸 빠뜨리면 다음
            // SyncFullscreen 이 우리 자신의 변화를 «바깥에서 일어난 일» 로 착각한다.
            _observedFullscreen = _applier.IsFullscreen;

            if (IsOpen)
            {
                Show();
            }
        }

        private void Show()
        {
            _view.ShowSettings(_settings.MasterVolume, _settings.Fullscreen);
        }
    }
}
