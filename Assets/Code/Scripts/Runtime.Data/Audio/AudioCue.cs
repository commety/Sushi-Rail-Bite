using System;
using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 소리 하나의 정의 — 무엇을, 얼마나 크게, 얼마나 자주.
    ///
    /// <para>
    /// <b>판정하지 않는다.</b> 간격을 들고만 있고 "지금 내도 되나" 는 <c>Runtime</c> 의 예산이
    /// 정한다 — <c>Runtime.Data</c> 는 계약이지 계산이 아니다
    /// (<c>.claude/rules/scriptable-object.md</c> §4).
    /// </para>
    /// <para>
    /// 값 타입이 아니라 참조 타입인 이유: 값 타입이면 사람이 채우기 전의 볼륨이 0 이라
    /// <b>클립을 물려도 소리가 나지 않는다.</b> 그 증상은 "재생이 안 된다" 로 보이지만 원인은
    /// 볼륨이라 애셋을 열어 보기 전까지 찾지 못한다. 참조 타입은 필드 초기값이 살아 있어
    /// 새 큐가 들리는 상태로 태어난다.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class AudioCue
    {
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [SerializeField, Min(0f)] private float _cooldownSeconds;

        /// <summary>재생할 클립. 비어 있을 수 있다 — 소리는 로직의 전제 조건이 아니다.</summary>
        public AudioClip Clip => _clip;

        /// <summary>재생 볼륨. 0~1 이다.</summary>
        public float Volume => _volume;

        /// <summary>
        /// 같은 큐를 다시 낼 수 있을 때까지의 최소 간격. <c>0</c> 이면 겹침 제어가 없다.
        ///
        /// <para>
        /// 자주 나는 큐만 값을 갖는다 — 손님 넷이 같은 프레임에 먹으면 같은 소리가 넷 겹쳐
        /// 볼륨이 네 배가 되고 찢어진다.
        /// </para>
        /// </summary>
        public float CooldownSeconds => _cooldownSeconds;

        /// <summary>
        /// 재생할 것이 있는가. <c>UnityEngine.Object</c> 의 <c>==</c> 오버로드 때문에
        /// 명시적으로 비교한다 (<c>CLAUDE.md</c> §4.3).
        /// </summary>
        public bool HasClip => _clip != null;

        /// <summary>
        /// 이상값 방어. 어트리뷰트는 인스펙터 입력만 막고 직렬화된 이상값·코드 대입은
        /// 통과시키므로 한 번 더 조인다 (<see cref="SushiData"/> 와 같은 처리다).
        ///
        /// <para>
        /// 중첩된 <c>[Serializable]</c> 타입에는 Unity 의 검증 훅이 오지 않는다. 품고 있는
        /// <see cref="AudioBankSO"/> 가 대신 불러 준다.
        /// </para>
        /// </summary>
        internal void Clamp()
        {
            _volume = Mathf.Clamp01(_volume);
            _cooldownSeconds = Mathf.Max(0f, _cooldownSeconds);
        }
    }
}
