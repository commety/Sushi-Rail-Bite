using SushiDefense.Belt;
using SushiDefense.Customers;
using SushiDefense.Data;
using SushiDefense.Run;
using SushiDefense.Stages;
using SushiDefense.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SushiDefense.Audio
{
    /// <summary>
    /// 로직의 사건을 소리로 옮긴다. <b>판정하지 않는다</b> — 겹쳐도 되는지는
    /// <see cref="SoundBudget"/> 이, 아예 소리를 내도 되는지는 <see cref="AudioUnlockGate"/> 가
    /// 정한다.
    ///
    /// <para>
    /// <b>런 수명이다.</b> 스테이지가 넘어가도 배경음이 이어지고 잠금이 다시 걸리지 않아야
    /// 하므로, 스테이지마다 새로 만들지 않고 <see cref="Bind"/> 로 새 판의 객체만 갈아 낀다.
    /// </para>
    /// <para>
    /// 재생 횟수를 노출하는 이유: 헤드리스 배치모드에는 오디오 장치가 없어 테스트가 실제
    /// 소리를 들을 수 없다. 뷰가 <c>RevenueText</c> 를 노출하는 것과 같은 이유다.
    /// </para>
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        // 큐를 구분하는 키. 값 자체는 의미가 없고 서로 다르기만 하면 된다.
        private const int SushiEatenCue = 0;
        private const int CustomerPlacedCue = 1;
        private const int RewardPickedCue = 2;
        private const int StageAdvancedCue = 3;
        private const int StageClearedCue = 4;
        private const int StageFailedCue = 5;
        private const int UiClickCue = 6;

        [SerializeField] private AudioBankSO _bank;

        /// <summary>효과음 전용. 배경음과 나누지 않으면 배경음이 효과음마다 끊긴다.</summary>
        [SerializeField] private AudioSource _sfxSource;

        [SerializeField] private AudioSource _bgmSource;

        /// <summary>
        /// 이 화면이 트는 곡. 씬마다 다른 <b>유일한</b> 오디오 설정이라 여기 있다 —
        /// 나머지 일곱 큐는 뱅크 하나를 그대로 공유한다.
        /// </summary>
        [SerializeField] private BgmTrack _bgmTrack = BgmTrack.Stage;

        private readonly AudioUnlockGate _gate = new();

        private SoundBudget _budget;
        private ClaimCoordinator _coordinator;
        private CustomerPlacementService _placement;
        private StageController _stage;
        private RewardSelectionPresenter _rewards;
        private StageTransitionPresenter _transition;

        /// <summary>배치 수는 변경 이벤트가 없어 값 변화를 지켜본다.</summary>
        private int _shownPlacedCount = -1;

        /// <summary>실제로 재생한 횟수. 검증용이다.</summary>
        public int PlayedCount { get; private set; }

        /// <summary>예산·게이트·설정에 막혀 버려진 요청 수. 검증용이다.</summary>
        public int SuppressedCount { get; private set; }

        /// <summary>배경음을 시작한 횟수. 검증용이다.</summary>
        public int BgmStartCount { get; private set; }

        /// <summary>배경음이 지금 울리고 있는가.</summary>
        public bool IsBgmPlaying => _bgmSource != null && _bgmSource.isPlaying;

        /// <summary>첫 사용자 입력이 들어왔는가.</summary>
        public bool IsUnlocked => _gate.IsUnlocked;

        /// <summary>
        /// 이 화면이 틀 곡. 뱅크가 없으면 <c>null</c> 이며, 부르는 쪽은 아무것도 하지 않는다 —
        /// 소리는 로직의 전제 조건이 아니다.
        /// </summary>
        public AudioCue BgmCue =>
            _bank == null ? null : _bgmTrack == BgmTrack.Main ? _bank.MainBgm : _bank.Bgm;

        /// <summary>
        /// 이 판의 사건 출처를 물린다. 이미 물려 있으면 먼저 끊는다 — <c>Build()</c> 는
        /// 스테이지가 넘어갈 때마다 다시 불리고, 구독이 겹치면 한 번의 먹힘이 두 번 난다.
        /// </summary>
        public void Bind(ClaimCoordinator coordinator, CustomerPlacementService placement,
                         StageController stage, RewardSelectionPresenter rewards,
                         StageTransitionPresenter transition)
        {
            Unbind();

            _coordinator = coordinator;
            _placement = placement;
            _stage = stage;
            _rewards = rewards;
            _transition = transition;

            if (_coordinator != null)
            {
                _coordinator.SushiEaten += OnSushiEaten;
            }

            if (_stage != null)
            {
                _stage.OutcomeDecided += OnOutcomeDecided;
            }

            if (_rewards != null)
            {
                _rewards.RewardChosen += OnRewardChosen;
            }

            if (_transition != null)
            {
                _transition.StageAdvanced += OnStageAdvanced;
            }

            // 잠금은 유지한다 — 스테이지가 넘어갔다고 다시 잠기면 배경음이 죽는다.
            _budget?.Reset();
            _shownPlacedCount = _placement != null ? _placement.PlacedCount : -1;
            SilenceUntilUnlocked();
        }

        /// <summary>
        /// 구독을 끊는다. 조율자·컨트롤러는 <see cref="MonoBehaviour"/> 가 아니라 씬이
        /// 내려가도 살아 있을 수 있으므로, 떼지 않으면 파괴된 이 객체를 계속 부른다
        /// (<c>.claude/rules/scripts.md</c> §6).
        /// </summary>
        public void Unbind()
        {
            if (_coordinator != null)
            {
                _coordinator.SushiEaten -= OnSushiEaten;
                _coordinator = null;
            }

            if (_stage != null)
            {
                _stage.OutcomeDecided -= OnOutcomeDecided;
                _stage = null;
            }

            if (_rewards != null)
            {
                _rewards.RewardChosen -= OnRewardChosen;
                _rewards = null;
            }

            if (_transition != null)
            {
                _transition.StageAdvanced -= OnStageAdvanced;
                _transition = null;
            }

            _placement = null;
        }

        /// <summary>
        /// 첫 사용자 입력을 알린다. 브라우저는 제스처가 있기 전까지 오디오를 잠그고,
        /// 그 상태에서 낸 소리는 <b>밀리지 않고 사라진다</b> — 씬 시작에 배경음을 재생하면
        /// 무음으로 흘러가 영영 들리지 않는다.
        ///
        /// <para>
        /// public 인 이유: 지금은 손님 배치가 첫 제스처지만, 화면이 늘면 다른 입력 지점도
        /// 이걸 부르게 된다 (M6).
        /// </para>
        /// </summary>
        public void NotifyUserInput()
        {
            _gate.Unlock();

            if (_gate.TryConsumeUnlockMoment())
            {
                StartBgm();
            }
        }

        /// <summary>
        /// UI 버튼이 눌렸다. <b>버튼 전용이다</b> — 카드 선택·손님 배치는 자기 큐를 갖는다
        /// (아키텍트 결정). 무엇이 버튼인지는 <c>UiClickSound</c> 가 가른다.
        ///
        /// <para>
        /// <b>여기서도 잠금을 푼다.</b> 메인 화면이 앞에 생기면서 게임의 <b>첫 제스처가
        /// 배치가 아니라 버튼 클릭</b>이 됐다 — 이 줄이 없으면 브라우저 잠금이 스테이지에
        /// 들어갈 때까지 안 풀려 메뉴에서 아무 소리도 나지 않는다.
        /// </para>
        /// </summary>
        public void PlayUiClick()
        {
            NotifyUserInput();
            Play(_bank != null ? _bank.UiClick : null, UiClickCue);
        }

        private void Awake()
        {
            SilenceUntilUnlocked();
        }

        /// <summary>
        /// <b>이미 열린 페이지로 들어왔으면 기다리지 않는다.</b> 잠금은 페이지 단위인데
        /// 진행자는 씬마다 새로 태어나므로, 여기서 한 번 확인하지 않으면 화면을 옮길 때마다
        /// <b>다시 클릭하기 전까지 음악이 없다</b> — 메인 → 스테이지 → 메인 세 구간 모두에서
        /// 그랬다.
        ///
        /// <para>
        /// <c>Awake</c> 가 아니라 <c>Start</c> 인 이유: 씬 진입점이 <see cref="Bind"/> 로
        /// 참조를 물리는 시점이 <c>Awake</c> 와 같은 프레임이라, 더 이른 곳에서 시작하면
        /// 뱅크가 아직 없을 수 있다.
        /// </para>
        /// </summary>
        private void Start()
        {
            if (_gate.TryConsumeUnlockMoment())
            {
                StartBgm();
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// <c>AudioSource</c> 의 자동 재생을 끈다. 기본값이 켜져 있어서 <b>그대로 두면
        /// 게이트가 통째로 무력화된다</b> — 씬이 열리자마자 잠긴 채로 재생이 시작되고,
        /// 브라우저가 그것을 버리므로 배경음이 영영 들리지 않는다.
        ///
        /// <para>
        /// 이미 열린 뒤에는 멈추지 않는다. <see cref="Bind"/> 는 스테이지가 넘어갈 때마다
        /// 불리는데, 거기서 멈추면 판이 바뀔 때마다 배경음이 끊긴다.
        /// </para>
        /// </summary>
        private void SilenceUntilUnlocked()
        {
            if (_sfxSource != null)
            {
                _sfxSource.playOnAwake = false;
            }

            if (_bgmSource == null)
            {
                return;
            }

            _bgmSource.playOnAwake = false;

            if (!_gate.IsUnlocked)
            {
                _bgmSource.Stop();
            }
        }

        /// <summary>
        /// 배치 수만 매 프레임 확인한다. 배치 서비스에 변경 이벤트가 없기 때문이며,
        /// <b>값이 바뀐 프레임에만</b> 반응한다 (<c>StageHudView</c> 와 같은 방식이다).
        /// </summary>
        private void Update()
        {
            UnlockOnAnyInput();
            RefreshBgmVolume();

            if (_placement == null)
            {
                return;
            }

            var placed = _placement.PlacedCount;
            if (placed == _shownPlacedCount)
            {
                return;
            }

            var increased = placed > _shownPlacedCount;
            _shownPlacedCount = placed;

            if (!increased)
            {
                return;
            }

            // 자리에 앉히려면 클릭이 있어야 한다 — 배치가 곧 첫 제스처다.
            NotifyUserInput();
            Play(_bank != null ? _bank.CustomerPlaced : null, CustomerPlacedCue);
        }

        /// <summary>
        /// <b>버튼만이 제스처인 것은 아니다.</b> 잠금을 푸는 길이 버튼 클릭과 손님 배치
        /// 둘뿐이었을 때는, 제목 화면에서 <b>빈 곳을 아무리 눌러도 음악이 시작되지 않았다</b> —
        /// 플레이어에게는 «음악이 안 나오는 게임» 으로 보인다.
        ///
        /// <para>
        /// 브라우저가 요구하는 것은 «제스처» 이지 «버튼» 이 아니므로, 아무 입력이나 받는다.
        /// <b>완전한 자동 재생은 불가능하다</b> — 그것은 우리가 고칠 수 있는 종류의 것이
        /// 아니다.
        /// </para>
        /// <para>
        /// 열린 뒤에는 <b>장치를 읽지도 않는다.</b> 매 프레임 도는 경로라 잠금이 풀린 뒤에도
        /// 계속 확인하면 값을 쓰지도 않을 검사를 평생 돌리게 된다.
        /// </para>
        /// </summary>
        private void UnlockOnAnyInput()
        {
            if (_gate.IsUnlocked || !AnyInputThisFrame())
            {
                return;
            }

            NotifyUserInput();
        }

        /// <summary>
        /// 설정에서 배경음 볼륨을 끄는 동안 <b>지금 울리는 곡</b>에도 반영한다. 시작할 때만
        /// 곱하면 슬라이더를 움직여도 다음 곡부터 적용되어, 플레이어에게는 설정이 고장 난
        /// 것으로 보인다.
        ///
        /// <para>
        /// <b>값이 달라진 프레임에만 쓴다.</b> 매 프레임 대입하면 소스가 계속 갱신되고,
        /// 배포 타깃(WebGL)에서 그만큼 손해다 (<c>CustomerView.LateUpdate</c> 와 같은 방식).
        /// </para>
        /// </summary>
        private void RefreshBgmVolume()
        {
            var cue = BgmCue;
            if (_bgmSource == null || cue == null)
            {
                return;
            }

            var target = cue.Volume * VolumeMix.Bgm;
            if (!Mathf.Approximately(_bgmSource.volume, target))
            {
                _bgmSource.volume = target;
            }
        }

        private static bool AnyInputThisFrame()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.press.wasPressedThisFrame)
            {
                return true;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            var touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.wasPressedThisFrame;
        }

        private void OnSushiEaten(CustomerLogic customer, SushiItem sushi)
        {
            Play(_bank != null ? _bank.SushiEaten : null, SushiEatenCue);
        }

        private void OnRewardChosen(RewardOffer offer)
        {
            Play(_bank != null ? _bank.RewardPicked : null, RewardPickedCue);
        }

        private void OnStageAdvanced(StageConfig next)
        {
            Play(_bank != null ? _bank.StageAdvanced : null, StageAdvancedCue);
        }

        /// <summary>
        /// 결과는 컨트롤러가 이미 정했다. <b>여기서 판정하지 않는다</b> — 뷰가 매출과 시간을
        /// 다시 비교하면 판정이 두 곳에 살게 된다.
        /// </summary>
        private void OnOutcomeDecided(StageOutcome outcome)
        {
            if (outcome == StageOutcome.Cleared)
            {
                Play(_bank != null ? _bank.StageCleared : null, StageClearedCue);
            }
            else if (outcome == StageOutcome.Failed)
            {
                Play(_bank != null ? _bank.StageFailed : null, StageFailedCue);
            }
        }

        /// <summary>
        /// 소리 하나를 낸다. 먹힘마다 불리는 경로라 <b>할당을 만들지 않는다</b>
        /// (<c>scripts.md</c> §4).
        /// </summary>
        private void Play(AudioCue cue, int cueId)
        {
            if (!_gate.IsUnlocked || _sfxSource == null || cue == null || !cue.HasClip
                || !EnsureBudget().TryPlay(cueId, Time.time, cue.CooldownSeconds, cue.Clip.length))
            {
                SuppressedCount++;
                return;
            }

            _sfxSource.PlayOneShot(cue.Clip, cue.Volume * VolumeMix.Sfx);
            PlayedCount++;
        }

        private void StartBgm()
        {
            var cue = BgmCue;
            if (_bgmSource == null || cue == null || !cue.HasClip)
            {
                return;
            }

            _bgmSource.clip = cue.Clip;
            _bgmSource.volume = cue.Volume * VolumeMix.Bgm;
            _bgmSource.loop = true;
            _bgmSource.Play();
            BgmStartCount++;
        }

        /// <summary>
        /// 예산은 상한을 알아야 열 수 있어 뱅크가 물린 뒤에 만든다. 뱅크가 없으면 아무것도
        /// 통과시키지 않는 1칸짜리를 쓴다 — 소리는 로직의 전제 조건이 아니다.
        /// </summary>
        private SoundBudget EnsureBudget()
        {
            return _budget ??= new SoundBudget(_bank != null ? _bank.MaxConcurrentSfx : 1);
        }
    }
}
