using UnityEngine;
using UnityEngine.EventSystems;

namespace SushiDefense.UI
{
    /// <summary>
    /// 화면이 뜬 직후 잠깐 <see cref="EventSystem"/> 을 꺼 둔다. 판단은
    /// <see cref="InputArmGate"/> 가 하고 여기는 그 결과를 컴포넌트에 반영만 한다
    /// (<c>CLAUDE.md</c> §3.2).
    ///
    /// <para>
    /// <b>왜 <see cref="EventSystem"/> 인가.</b> 버튼마다 «아직 누르지 마세요» 를 붙이면
    /// 버튼이 늘 때마다 빠뜨릴 자리가 는다. 배달 경로가 하나뿐이므로 거기서 한 번 막는다.
    /// </para>
    /// <para>
    /// <b>같은 오브젝트에 붙인다.</b> 남의 컴포넌트를 껐다 켜면 그 오브젝트가 씬에서 사라질
    /// 때 여기가 <c>null</c> 을 붙들게 된다 — 자기 것을 다루면 수명이 같다.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public sealed class UiInputArmer : MonoBehaviour
    {
        /// <summary>
        /// 입력을 받기까지 기다리는 시간. 연출 수치라 여기 있다 (M5 D7).
        /// 사람이 의도적으로 누르는 클릭은 이보다 뒤에 온다.
        /// </summary>
        [SerializeField, Min(0f)] private float _delaySeconds = 0.25f;

        /// <summary>
        /// 함께 기다리는 최소 프레임 수. 로딩 직후 첫 프레임의 <c>deltaTime</c> 이
        /// 부풀어 있어 시간만으로는 <b>한 프레임 만에 열릴 수 있다</b>.
        /// </summary>
        [SerializeField, Min(0)] private int _delayFrames = 2;

        private EventSystem _eventSystem;
        private InputArmGate _gate;

        /// <summary>지금 입력을 받고 있나. 검증용이다.</summary>
        public bool IsArmed => _gate == null || _gate.IsArmed;

        private void Awake()
        {
            _eventSystem = GetComponent<EventSystem>();
            _gate = new InputArmGate(_delaySeconds, _delayFrames);
            Apply();
        }

        /// <summary>
        /// <c>Update</c> 가 아니라 <c>LateUpdate</c> 다. 같은 프레임에 입력 모듈이 이미
        /// 돌았을 수 있어, 켜는 것은 그 프레임의 처리가 끝난 뒤여야 한다.
        /// </summary>
        private void LateUpdate()
        {
            if (_gate.IsArmed)
            {
                return;
            }

            // 실제 시간으로 잰다. 이 게이트는 판의 시간이 아니라 브라우저가 화면을
            // 내주는 시간을 기다리는 것이라, 멈춤이나 배율에 묶이면 안 된다.
            _gate.Tick(Time.unscaledDeltaTime);
            Apply();
        }

        private void Apply()
        {
            if (_eventSystem != null)
            {
                _eventSystem.enabled = _gate.IsArmed;
            }
        }
    }
}
