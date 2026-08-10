using UnityEngine;

namespace SushiDefense.Settings
{
    /// <summary>
    /// 설정을 <see cref="PlayerPrefs"/> 에 남긴다. <b>WebGL 에서는 IndexedDB 로 간다</b> —
    /// 브라우저가 사이트 데이터를 지우면 함께 사라지며, 그것은 정상이다.
    ///
    /// <para>
    /// <b>키가 없으면 손대지 않는다.</b> 처음 켠 사람에게는 <see cref="GameSettings"/> 의
    /// 기본값이 그대로 남아야 하고, <c>GetFloat</c> 의 기본값을 쓰면 <i>"저장된 적 없음"</i>
    /// 과 <i>"0 으로 저장했음"</i> 이 구분되지 않는다.
    /// </para>
    /// <para>
    /// <b><c>PlayerPrefs.Save()</c> 를 명시적으로 부른다.</b> 자동 기록은 정상 종료 시점에
    /// 일어나는데, 웹은 탭을 닫는 것이 정상 종료가 아니다 — 부르지 않으면 마지막 변경이
    /// 다음 실행에 없다.
    /// </para>
    /// </summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        /// <summary>
        /// 키에 접두어를 붙인다. <c>PlayerPrefs</c> 는 <b>회사·제품 이름 단위로 공유되는</b>
        /// 전역 공간이라, 짧은 이름은 다른 저장 항목과 부딪힌다.
        /// </summary>
        private const string MasterVolumeKey = "SushiRailBite.Settings.MasterVolume";

        private const string BgmVolumeKey = "SushiRailBite.Settings.BgmVolume";

        private const string SfxVolumeKey = "SushiRailBite.Settings.SfxVolume";

        private const string FullscreenKey = "SushiRailBite.Settings.Fullscreen";

        /// <inheritdoc />
        public void Load(GameSettings into)
        {
            if (into == null)
            {
                return;
            }

            if (PlayerPrefs.HasKey(MasterVolumeKey))
            {
                // 손상된 값이 와도 모델이 자른다 — 저장소가 판정하지 않는다.
                into.SetMasterVolume(PlayerPrefs.GetFloat(MasterVolumeKey));
            }

            if (PlayerPrefs.HasKey(BgmVolumeKey))
            {
                into.SetBgmVolume(PlayerPrefs.GetFloat(BgmVolumeKey));
            }

            if (PlayerPrefs.HasKey(SfxVolumeKey))
            {
                into.SetSfxVolume(PlayerPrefs.GetFloat(SfxVolumeKey));
            }

            if (PlayerPrefs.HasKey(FullscreenKey))
            {
                into.SetFullscreen(PlayerPrefs.GetInt(FullscreenKey) != 0);
            }
        }

        /// <inheritdoc />
        public void Save(GameSettings from)
        {
            if (from == null)
            {
                return;
            }

            PlayerPrefs.SetFloat(MasterVolumeKey, from.MasterVolume);
            PlayerPrefs.SetFloat(BgmVolumeKey, from.BgmVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, from.SfxVolume);
            PlayerPrefs.SetInt(FullscreenKey, from.Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
