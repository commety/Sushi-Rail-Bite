using System;
using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// "초밥이 먹혔다"를 발행하는 채널. 벨트·손님(발행)과 점수·UI(구독) 사이의 직접 참조를 끊는다
    /// (<c>CLAUDE.md</c> §3.3).
    ///
    /// <para>
    /// <b>구독자는 <c>OnDestroy</c>/<c>OnDisable</c> 에서 반드시 해제해야 한다.</b>
    /// 이 채널은 <see cref="ScriptableObject"/> 라 플레이 종료 후에도 살아 있고, 남은 구독은
    /// 다음 플레이에서 파괴된 객체를 호출한다 (<c>.claude/rules/scripts.md</c> §6).
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "SushiRailBite/Events/Sushi Eaten Channel",
                     fileName = "SushiEatenEventChannel")]
    public sealed class SushiEatenEventChannelSO : ScriptableObject
    {
        /// <summary>초밥이 소비될 때마다 발생한다.</summary>
        public event Action<SushiEatenPayload> OnRaised;

        /// <summary>구독자 전원에게 소비 사실을 알린다. 구독자가 없어도 안전하다.</summary>
        public void Raise(in SushiEatenPayload payload)
        {
            OnRaised?.Invoke(payload);
        }

        /// <summary>
        /// 남은 구독을 전부 끊는다. 스테이지 종료·플레이 재시작처럼 구독자 수명이 한꺼번에
        /// 끝나는 시점에서 누수를 막는 안전장치다.
        /// </summary>
        public void ClearSubscribers()
        {
            OnRaised = null;
        }
    }
}
