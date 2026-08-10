using System;
using UnityEngine;

namespace SushiDefense.Effects
{
    /// <summary>
    /// 짧게 커지며 사라지는 이펙트 하나. 수명이 다하면 <b>스스로 풀에 돌아간다.</b>
    ///
    /// <para>
    /// 연출 수치를 이 프리팹의 직렬화 필드에 둔 이유: 이 이펙트 하나에만 의미가 있고 다른
    /// 곳에서 참조되지 않는다. SO 로 빼면 애셋만 늘고 찾기 어려워진다 — 사람이 플레이하며
    /// 조정하는 밸런스 수치와는 다르다.
    /// </para>
    /// <para>
    /// 수명 관리에 코루틴을 쓰지 않는다. 재생마다 <c>IEnumerator</c> 가 할당되는데, 이건
    /// 초밥이 먹힐 때마다 도는 경로다 (<c>.claude/rules/scripts.md</c> §4).
    /// </para>
    /// </summary>
    public sealed class PopEffectView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [SerializeField, Min(0.01f)] private float _lifetimeSeconds = 0.35f;
        [SerializeField, Min(0f)] private float _startScale = 0.4f;
        [SerializeField, Min(0f)] private float _endScale = 1.3f;

        private Action<GameObject> _returnToPool;
        private float _elapsed;

        /// <summary>지금 떠 있는가. 검증용이다.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// 지정한 자리에서 재생을 시작한다.
        /// </summary>
        /// <param name="returnToPool">
        /// 수명이 끝났을 때 부를 반납 경로. <b>미리 만들어 둔 델리게이트를 넘긴다</b> —
        /// 호출부에서 메서드 그룹을 매번 넘기면 재생마다 델리게이트가 할당된다.
        /// </param>
        public void Play(Vector3 worldPosition, Color tint, Action<GameObject> returnToPool)
        {
            transform.position = worldPosition;
            _returnToPool = returnToPool;
            _elapsed = 0f;
            IsPlaying = true;

            if (_body != null)
            {
                _body.color = tint;
            }

            ApplyScale(0f);
        }

        private void Awake()
        {
            if (_body == null)
            {
                _body = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            _elapsed += Time.deltaTime;

            if (_elapsed < _lifetimeSeconds)
            {
                ApplyScale(_elapsed / _lifetimeSeconds);
                return;
            }

            IsPlaying = false;

            // 반납 경로가 없으면 그냥 멈춘다. 여기서 Destroy 를 부르면 풀이 깨진다
            // (프로덕션의 Instantiate/Destroy 는 풀 안에만 있다 — CLAUDE.md §3.4).
            _returnToPool?.Invoke(gameObject);
        }

        /// <summary>
        /// 커지면서 옅어진다. <paramref name="progress"/> 는 0~1.
        /// </summary>
        private void ApplyScale(float progress)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, progress);

            if (_body != null)
            {
                var color = _body.color;
                color.a = 1f - progress;
                _body.color = color;
            }
        }
    }
}
