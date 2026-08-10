using UnityEngine;

namespace SushiDefense.Audio
{
    /// <summary>
    /// 배경음과 효과음의 <b>상대 볼륨</b>. 큐가 들고 있는 볼륨 위에 한 번 더 곱해진다.
    ///
    /// <para>
    /// <b>왜 전역인가.</b> 설정은 메인 화면에만 살고 소리를 내는 진행자는 씬마다 새로
    /// 태어난다 — 값을 인스턴스로 들고 다니면 스테이지로 넘어가는 순간 잃는다. Unity 의
    /// <c>AudioListener.volume</c> 이 마스터를 전역으로 들고 있는 것과 같은 이유이며,
    /// 우리가 그 옆에 두 칸을 더 두는 것뿐이다.
    /// </para>
    /// <para>
    /// <b>믹서를 쓰지 않았다.</b> <c>AudioMixer</c> 가 정석이지만 그룹과 노출 파라미터를
    /// 스크립트로 만들 길이 없어 애셋을 손으로 짜야 하고, 그러면 이 값들이 코드에서
    /// 사라져 EditMode 로 확인할 수 없게 된다. 소리 갈래가 둘뿐인 동안은 이쪽이 싸다.
    /// </para>
    /// <para>
    /// 자르는 곳은 여기 한 곳이다 — 설정 모델이 이미 자르지만, 저장소가 손상된 값을
    /// 돌려주는 경로가 실제로 있어 마지막 관문을 둔다 (<c>GameSettings</c> 와 같은 판단).
    /// </para>
    /// </summary>
    public static class VolumeMix
    {
        /// <summary>아직 아무도 정하지 않았을 때. <c>0</c> 이면 소리가 안 나는 것을 버그로 신고한다.</summary>
        public const float Default = 1f;

        private static float _bgm = Default;
        private static float _sfx = Default;

        /// <summary>배경음 볼륨. <c>0</c>~<c>1</c>.</summary>
        public static float Bgm
        {
            get => _bgm;
            set => _bgm = Clamp(value, _bgm);
        }

        /// <summary>효과음 볼륨. <c>0</c>~<c>1</c>.</summary>
        public static float Sfx
        {
            get => _sfx;
            set => _sfx = Clamp(value, _sfx);
        }

        /// <summary>
        /// 기본값으로 되돌린다. 재생을 다시 시작하면 <c>static</c> 이 살아남으므로 Unity 가
        /// 불러 준다 (RULE-01). 테스트가 서로에게 새는 것도 이것으로 막는다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetOnLoad()
        {
            _bgm = Default;
            _sfx = Default;
        }

        /// <summary>
        /// 범위 밖은 자르고, 숫자가 아닌 값은 <b>무시한다.</b> <c>NaN</c> 은 비교가 전부
        /// <c>false</c> 라 자르기를 통과해 그대로 들어가고, 그 뒤로는 볼륨이 영영 복구되지
        /// 않는다.
        /// </summary>
        private static float Clamp(float value, float current)
        {
            return float.IsNaN(value) ? current : Mathf.Clamp01(value);
        }
    }
}
