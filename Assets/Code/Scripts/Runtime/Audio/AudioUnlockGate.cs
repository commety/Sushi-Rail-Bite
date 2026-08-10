using UnityEngine;

namespace SushiDefense.Audio
{
    /// <summary>
    /// 첫 사용자 입력이 있기 전까지 재생을 막는다.
    ///
    /// <para>
    /// 배포 타깃(WebGL)에서 브라우저는 사용자 제스처가 있기 전까지 오디오를 잠근다.
    /// 그 상태에서 낸 소리는 <b>밀리지 않고 그냥 사라진다</b> — 배경음을 씬 시작에 재생하면
    /// 무음으로 흘러가 영영 들리지 않는다. 증상이 "소리가 안 난다" 라서 볼륨·클립·믹서를
    /// 뒤지게 되는데, 원인은 재생 시점이다.
    /// </para>
    /// <para>
    /// <b>잠금은 페이지 단위다 — 씬 단위가 아니다.</b> 브라우저가 한 번 연 것을 다시 잠그지
    /// 않으므로, 잠금 상태도 씬을 넘어 이어져야 한다. 인스턴스마다 따로 들고 있었을 때는
    /// 씬이 바뀔 때마다 새 <c>AudioDirector</c> 가 <b>잠긴 채로 태어나</b>, 메인 →
    /// 스테이지 → 메인 어느 쪽으로 가도 <b>다시 클릭하기 전까지 음악이 없었다.</b>
    /// </para>
    /// <para>
    /// <see cref="SoundBudget"/> 과 합치지 않는다. 예산은 <i>너무 많다·너무 잦다</i> 를,
    /// 이쪽은 <i>아직 아무것도 안 된다</i> 를 본다 — 합치면 "소리가 안 났다" 의 원인이
    /// 둘 중 어느 쪽인지 테스트에서 구분되지 않는다.
    /// </para>
    /// </summary>
    public sealed class AudioUnlockGate
    {
        /// <summary>
        /// 이 페이지에서 제스처가 있었나. <c>static</c> 이므로 도메인 리로드가 꺼진 환경에서
        /// 재생을 다시 시작해도 남는다 — <see cref="ResetOnLoad"/> 가 지운다 (RULE-01).
        /// </summary>
        private static bool _pageUnlocked;

        /// <summary>
        /// 아직 소비되지 않은 «열린 순간». <b>인스턴스마다 따로다</b> — 새 씬의 진행자도
        /// 자기 배경음을 한 번 시작할 기회를 가져야 하기 때문이며, 페이지가 이미 열려
        /// 있으면 생성 시점에 바로 하나를 들고 태어난다.
        /// </summary>
        private bool _momentPending;

        public AudioUnlockGate()
        {
            _momentPending = _pageUnlocked;
        }

        /// <summary>재생이 허용된 상태인가.</summary>
        public bool IsUnlocked => _pageUnlocked;

        /// <summary>
        /// 첫 입력에서 연다. 두 번째 호출부터는 아무 일도 하지 않는다 — 입력은 계속
        /// 들어오는데 그때마다 <see cref="TryConsumeUnlockMoment"/> 가 되살아나면
        /// 배경음이 처음부터 다시 재생된다.
        /// </summary>
        public void Unlock()
        {
            if (_pageUnlocked)
            {
                return;
            }

            _pageUnlocked = true;
            _momentPending = true;
        }

        /// <summary>
        /// 열린 뒤 <b>이 게이트에서 처음 한 번만</b> <c>true</c>. 배경음을 시작할 시점을
        /// 여기서 잡는다.
        /// </summary>
        public bool TryConsumeUnlockMoment()
        {
            if (!_momentPending)
            {
                return false;
            }

            _momentPending = false;
            return true;
        }

        /// <summary>
        /// 페이지를 새로 연 것과 같은 상태로 되돌린다. 재생 시작마다 Unity 가 부르고
        /// (RULE-01 — 도메인 리로드가 꺼져 있어도 <c>static</c> 이 살아남는다), 테스트가
        /// 서로에게 새는 것도 이것으로 막는다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetOnLoad()
        {
            _pageUnlocked = false;
        }
    }
}
