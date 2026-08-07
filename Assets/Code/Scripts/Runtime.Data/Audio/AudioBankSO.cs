using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 이 게임이 내는 소리 전부와, 겹칠 때의 규칙.
    ///
    /// <para>
    /// 간격과 동시 재생 상한이 코드가 아니라 여기 있는 이유: 겹침이 거슬리는지 아닌지는
    /// <b>플레이하며 조정하는 값</b>이다 (<c>CLAUDE.md</c> §3.1). 코드에 박으면 한 번 만질
    /// 때마다 컴파일해야 한다.
    /// </para>
    /// <para>
    /// <b>재생 장치를 모른다.</b> 무엇을 낼지만 들고 있다 — 지금 내도 되는지는 <c>Runtime</c> 의
    /// 예산이, 실제 재생은 <c>Presentation</c> 이 한다.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "SushiRailBite/Audio Bank", fileName = "AudioBank")]
    public sealed class AudioBankSO : ScriptableObject
    {
        /// <summary>
        /// 동시 재생 수의 구조적 하한. 밸런스 값이 아니라 "이 아래는 뱅크가 아니다"는
        /// 불변조건이라 코드에 둔다 — <c>0</c> 이면 소리가 하나도 나지 않는다.
        /// 실제 상한은 밸런스라 애셋에서 정한다.
        /// </summary>
        private const int MinimumConcurrentSfx = 1;

        [SerializeField] private AudioCue _sushiEaten = new();
        [SerializeField] private AudioCue _customerPlaced = new();
        [SerializeField] private AudioCue _rewardPicked = new();
        [SerializeField] private AudioCue _stageAdvanced = new();
        [SerializeField] private AudioCue _stageCleared = new();
        [SerializeField] private AudioCue _stageFailed = new();
        [SerializeField] private AudioCue _bgm = new();

        [SerializeField, Min(MinimumConcurrentSfx)]
        private int _maxConcurrentSfx = MinimumConcurrentSfx;

        /// <summary>
        /// 초밥이 소비될 때. <b>가장 자주 나는 큐</b>라 간격과 볼륨이 여기서 제일 중요하다.
        /// </summary>
        public AudioCue SushiEaten => _sushiEaten;

        /// <summary>손님을 자리에 앉혔을 때.</summary>
        public AudioCue CustomerPlaced => _customerPlaced;

        /// <summary>보상 카드를 골랐을 때.</summary>
        public AudioCue RewardPicked => _rewardPicked;

        /// <summary>다음 스테이지로 넘어갈 때.</summary>
        public AudioCue StageAdvanced => _stageAdvanced;

        /// <summary>스테이지를 클리어했을 때. 스테이지당 한 번뿐이다.</summary>
        public AudioCue StageCleared => _stageCleared;

        /// <summary>스테이지에 실패했을 때. 스테이지당 한 번뿐이다.</summary>
        public AudioCue StageFailed => _stageFailed;

        /// <summary>
        /// 스테이지 배경음.
        ///
        /// <para>
        /// 효과음과 같은 타입을 쓰되 간격은 의미가 없다 — 루프로 한 번 시작하고 끝이다.
        /// 배포 타깃(WebGL)에서는 <b>첫 사용자 입력 전에 시작하면 무음으로 흘러가</b> 영영
        /// 들리지 않으므로, 시작 시점은 <c>Presentation</c> 이 게이트로 잡는다.
        /// </para>
        /// </summary>
        public AudioCue Bgm => _bgm;

        /// <summary>
        /// 동시에 울릴 수 있는 효과음 수. 이 수를 넘는 요청은 버려진다.
        ///
        /// <para>
        /// 배경음은 세지 않는다 — 효과음과 채널이 다르고, 상한에 걸려 배경음이 끊기면
        /// 상한을 올리는 것 말고는 고칠 방법이 없다.
        /// </para>
        /// </summary>
        public int MaxConcurrentSfx => _maxConcurrentSfx;

        /// <summary>
        /// 이상값 방어. 어트리뷰트는 인스펙터 입력만 막으므로 여기서 한 번 더 조인다.
        /// 중첩된 큐에는 Unity 의 검증 훅이 직접 오지 않아 대신 불러 준다.
        ///
        /// <para>
        /// <b>구조 불변식만 본다.</b> 볼륨이 작다·간격이 길다는 오류가 아니라 기획이다
        /// (<c>.claude/rules/scriptable-object.md</c> §6).
        /// </para>
        /// </summary>
        internal void OnValidate()
        {
            _maxConcurrentSfx = Mathf.Max(MinimumConcurrentSfx, _maxConcurrentSfx);

            _sushiEaten.Clamp();
            _customerPlaced.Clamp();
            _rewardPicked.Clamp();
            _stageAdvanced.Clamp();
            _stageCleared.Clamp();
            _stageFailed.Clamp();
            _bgm.Clamp();
        }
    }
}
