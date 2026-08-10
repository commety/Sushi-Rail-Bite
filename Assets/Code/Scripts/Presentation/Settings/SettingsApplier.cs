using SushiDefense.Audio;
using SushiDefense.UI;
using UnityEngine;

namespace SushiDefense.Settings
{
    /// <summary>
    /// 고른 설정을 엔진에 먹인다 — <c>AudioListener.volume</c> 과 <c>Screen.fullScreen</c>.
    ///
    /// <para>
    /// <b>WebGL 의 전체화면 제약 셋.</b> 여기 적어 두지 않으면 다음 사람이
    /// <i>"전체화면이 안 되는데요"</i> 에서 멈춘다.
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <c>Screen.fullScreen = true</c> 는 <b>사용자 제스처 안에서만</b> 먹는다. 토글을 누르는
    /// 것이 그 제스처이므로 정상 경로에서는 동작하지만, <b>불러온 직후의 자동 적용은
    /// 무시될 수 있다</b> — 저장된 값이 전체화면이어도 페이지를 새로 열면 창 모드로 뜬다.
    /// 그것은 버그가 아니다.
    /// </description></item>
    /// <item><description>
    /// <c>Screen.fullScreenMode</c> 의 데스크톱 전용 모드는 WebGL 에서 의미가 없다.
    /// <c>fullScreen</c> 불리언만 쓴다.
    /// </description></item>
    /// <item><description>
    /// 그래서 <b>화면에 상태를 되읽어 표시한다</b> (<see cref="IsFullscreen"/>). 적용이
    /// 거부됐는데 토글만 켜져 있으면 플레이어가 무엇이 참인지 알 수 없다.
    /// </description></item>
    /// </list>
    /// <para>
    /// 볼륨은 세 층이다: 큐별 상대 볼륨(<c>AudioBankSO</c>) × 갈래별
    /// (<see cref="VolumeMix"/>) × 마스터(<c>AudioListener.volume</c>). 마스터만 엔진에
    /// 둘 곳이 있고 갈래별은 없어서 <b>가운데 층만</b> 우리가 든다 — 그래서 이 클래스가
    /// 오디오 코드를 여전히 건드리지 않는다.
    /// </para>
    /// </summary>
    public sealed class SettingsApplier : ISettingsApplier
    {
        /// <inheritdoc />
        public bool IsFullscreen => Screen.fullScreen;

        /// <inheritdoc />
        public void Apply(GameSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            AudioListener.volume = settings.MasterVolume;

            // 갈래별 볼륨은 엔진에 둘 곳이 없어 우리가 든다. 소리를 내는 진행자는 씬마다
            // 새로 태어나므로, 값이 씬을 넘어 살아 있어야 스테이지로 넘어가도 유지된다.
            VolumeMix.Bgm = settings.BgmVolume;
            VolumeMix.Sfx = settings.SfxVolume;

            // 같은 값을 다시 쓰지 않는다 — 전체화면 전환은 브라우저에서 실제 창 조작이라
            // 값이 그대로인데도 깜빡임이 난다.
            if (Screen.fullScreen != settings.Fullscreen)
            {
                Screen.fullScreen = settings.Fullscreen;
            }
        }
    }
}
