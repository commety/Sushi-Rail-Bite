using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님 몸통의 동작 재생. <b>판정하지 않는다</b> — 무엇을 틀지는
    /// <see cref="CustomerMotionSelector"/> 가, 얼마나 빠르게는 <see cref="CustomerMotionSpeed"/>
    /// 가 이미 정해서 넘어온다.
    ///
    /// <para>
    /// <b>대기 클립은 대기 동작이 아니다.</b> 이 프로젝트에서 <c>-idle</c> 시트는 «가만히 있는
    /// 모습» 이 아니라 <b>먹기와 소화 둘 다</b>의 재료이며, 갈리는 것은 재생 속도뿐이다.
    /// 아무 동작도 없을 때 나가는 그림은 클립이 아니라 <c>CustomerData.Icon</c> 낱장이다 —
    /// 그래서 <see cref="CustomerMotion.None"/> 에서는 <c>Animator</c> 를 <b>끈다.</b>
    /// </para>
    /// <para>
    /// 끄는 이유가 성능이 아니라 <b>소유권</b>이다: 켜져 있는 <c>Animator</c> 는 매 프레임
    /// 스프라이트를 자기가 덮어써서, 낱장 그림을 대입해도 다음 프레임에 사라진다.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CustomerMotionView : MonoBehaviour
    {
        /// <summary>말풍선 자식의 이름. 인스펙터가 비었을 때 찾을 경로이며 씬 조립과의 약속이다.</summary>
        public const string ThinkingChildName = "ThinkingInterface";

        /// <summary>
        /// 배속을 유도할 클립을 이름으로 가른다. <c>CustomerAnimationBuilder</c> 가 시트 이름을
        /// 그대로 클립 이름으로 쓰므로 접미가 계약이 된다 — 런타임에는 상태 기계 안을 들여다볼
        /// 수 없어 (<c>AnimatorController</c> 는 에디터 전용) 이름 말고 가를 방법이 없다.
        /// </summary>
        private const string IdleClipSuffix = "-idle";

        /// <summary>«지금 집는 중인가». <c>CustomerAnimationBuilder.PickingParameter</c> 와 같은 이름이다.</summary>
        private static readonly int PickingParameter = Animator.StringToHash("Picking");

        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _body;

        /// <summary>대역 밖 초밥을 두고 고민할 때 머리 위에 뜨는 말풍선.</summary>
        [SerializeField] private GameObject _thinking;

        /// <summary>초밥 하나를 먹는 동안 클립이 도는 횟수. 클수록 잘게 씹는다.</summary>
        [SerializeField, Min(1)] private int _chewCycles = 3;

        /// <summary>쉬는 동안 클립이 도는 횟수. 1 이면 소화 시간에 걸쳐 한 번 크게 숨쉰다.</summary>
        [SerializeField, Min(1)] private int _restCycles = 1;

        /// <summary>
        /// 집는 동작 한 번의 길이. 네 칸 시트를 초당 8프레임으로 돌린 길이가 기본값이다.
        ///
        /// <para>
        /// <b>먹기·소화와 달리 클립에서 읽지 않는다.</b> 집기는 밸런스와 무관하게 늘 같은
        /// 속도로 일어나는 <b>연출 수치</b>라 사람이 보며 정하는 값이고
        /// (<c>.claude/domain/presentation-and-audio.md</c> §3), 클립 이름으로 길이를 찾게
        /// 하면 <c>-picking</c> 이라는 이름 규칙이 계약으로 하나 더 늘어난다.
        /// </para>
        /// </summary>
        [SerializeField, Min(0f)] private float _pickingSeconds = 0.5f;

        private CustomerData _data;
        private Sprite _resting;
        private float _idleSeconds;
        private bool _resolved;

        /// <summary>
        /// 지금 화면에 반영돼 있는 동작. <b>변화한 프레임에만</b> 쓰기 위한 값이자 검증용이다.
        /// </summary>
        public CustomerMotion ShownMotion { get; private set; } = CustomerMotion.None;

        /// <summary>지금 말풍선이 떠 있는가. 검증용이다.</summary>
        public bool ShownThinking { get; private set; }

        /// <summary>집기 동작 한 번의 길이. <c>0</c> 이면 집기를 건너뛰고 곧장 먹는 모습이 된다.</summary>
        public float PickingSeconds => _pickingSeconds;

        /// <summary>
        /// 이 유형의 클립 묶음을 문다. <paramref name="resting"/> 은 아무 동작도 없을 때
        /// 나갈 낱장 그림이다.
        ///
        /// <para>
        /// <b>동작을 <see cref="CustomerMotion.None"/> 으로 되돌린다.</b> 자리를 갈아탈 때
        /// 앞 손님이 먹던 배속과 집기 상태가 남으면, 방금 앉은 손님이 아무것도 없는데
        /// 씹고 있는 것처럼 보인다.
        /// </para>
        /// </summary>
        public void Bind(CustomerData data, Sprite resting)
        {
            Resolve();

            _data = data;
            _resting = resting;
            _idleSeconds = 0f;

            if (_animator != null)
            {
                _animator.runtimeAnimatorController = data != null ? data.Motions : null;
                ReadIdleLength();
            }

            // 되돌리기 전에 «지금과 다르다» 를 만들어 둔다. 변화 감지가 걸려 있어서
            // 앞 손님도 None 이었다면 그대로 넘어가고, 그러면 새 손님의 낱장 그림이
            // 대입되지 않아 앞 손님의 마지막 프레임이 남는다.
            ShownMotion = CustomerMotion.Picking;
            Show(CustomerMotion.None);

            ShownThinking = true;
            ShowThinking(false);
        }

        /// <summary>
        /// 동작을 화면에 반영한다. <b>바뀐 프레임에만</b> 쓴다 — 매 프레임
        /// <c>SetBool</c>·<c>speed</c> 를 대입하면 배포 타깃(WebGL)에서 그대로 손해다.
        /// </summary>
        public void Show(CustomerMotion motion)
        {
            if (motion == ShownMotion)
            {
                return;
            }

            Resolve();
            ShownMotion = motion;

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                RestShowing();
                return;
            }

            if (motion == CustomerMotion.None)
            {
                _animator.enabled = false;
                RestShowing();
                return;
            }

            _animator.enabled = true;
            _animator.SetBool(PickingParameter, motion == CustomerMotion.Picking);
            _animator.speed = CustomerMotionSpeed.For(motion, _idleSeconds,
                                                      _data != null ? _data.EatSeconds : 0f,
                                                      _data != null ? _data.DigestSeconds : 0f,
                                                      _chewCycles, _restCycles);
        }

        /// <summary>
        /// 말풍선을 켜고 끈다. 몸통 색은 건드리지 않는다 — 기다림은 <b>말풍선 하나로만</b>
        /// 알린다. 한때 몸통을 파랗게 칠했는데, 같은 사실을 두 채널로 내보내면 상태 색이
        /// 통째로 대기에 묶여 먹기·소화가 묻힌다.
        /// </summary>
        public void ShowThinking(bool thinking)
        {
            if (thinking == ShownThinking)
            {
                return;
            }

            Resolve();
            ShownThinking = thinking;

            if (_thinking != null)
            {
                _thinking.SetActive(thinking);
            }
        }

        private void Awake()
        {
            Resolve();
        }

        /// <summary>
        /// 자기 참조를 <b>한 번만</b> 챙긴다. <see cref="Awake"/> 뿐 아니라 <see cref="Bind"/>
        /// 에서도 부르므로 실행 순서에 기대지 않는다 — 자리는 손님이 앉을 때까지 꺼져 있고,
        /// 꺼진 오브젝트의 <see cref="Awake"/> 는 켜질 때까지 돌지 않는다
        /// (<c>CustomerView.Resolve</c> 와 같은 방어다).
        /// </summary>
        private void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_body == null)
            {
                _body = GetComponent<SpriteRenderer>();
            }

            if (_thinking == null)
            {
                var child = transform.Find(ThinkingChildName);
                _thinking = child != null ? child.gameObject : null;
            }
        }

        /// <summary>
        /// 배속의 기준이 되는 대기 클립의 길이를 컨트롤러에서 읽는다. 시트의 칸 수나 프레임
        /// 속도를 코드에 박지 않기 위해서다 — 박아 두면 시트를 한 칸 늘렸을 때
        /// <b>배속만 조용히 어긋난다.</b>
        /// </summary>
        private void ReadIdleLength()
        {
            var clips = _animator.runtimeAnimatorController != null
                ? _animator.runtimeAnimatorController.animationClips
                : null;

            if (clips == null)
            {
                return;
            }

            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip != null && clip.name.EndsWith(IdleClipSuffix, System.StringComparison.Ordinal))
                {
                    _idleSeconds = clip.length;
                    return;
                }
            }
        }

        /// <summary>동작이 없을 때의 그림으로 되돌린다. 비어 있으면 그대로 둔다.</summary>
        private void RestShowing()
        {
            if (_body != null && _resting != null)
            {
                _body.sprite = _resting;
            }
        }
    }
}
