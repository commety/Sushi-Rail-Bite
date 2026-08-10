using System;
using System.Collections.Generic;

namespace SushiDefense.Audio
{
    /// <summary>
    /// 효과음이 겹칠 때의 규칙을 판정한다. <b>재생하지 않는다.</b>
    ///
    /// <para>
    /// 손님 넷이 같은 프레임에 초밥을 먹으면 같은 소리가 넷 겹쳐 볼륨이 네 배가 되고
    /// 찢어진다. 그것을 막는 규칙이 둘이다 — <b>같은 큐를 너무 자주 내지 않는다</b>(간격),
    /// <b>한꺼번에 너무 많이 내지 않는다</b>(상한).
    /// </para>
    /// <para>
    /// <b>시계를 갖지 않는다.</b> 시각을 인자로 받으므로 "0.05초 안에 셋" 을 프레임 없이
    /// 재현할 수 있다 (<c>.claude/rules/tests.md</c> §1). 재생 장치도 모른다 — 무엇을 낼지는
    /// 부르는 쪽이 안다.
    /// </para>
    /// </summary>
    public sealed class SoundBudget
    {
        /// <summary>
        /// 아직 아무것도 울린 적 없는 슬롯의 만료 시각. <c>0</c> 으로 두면 시각이 음수인
        /// 경우에 점유 중으로 읽히므로 비교로 결코 이길 수 없는 값을 쓴다.
        /// </summary>
        private const float NeverOccupied = float.NegativeInfinity;

        /// <summary>슬롯별 만료 시각. 길이가 곧 동시 재생 상한이다.</summary>
        private readonly float[] _slotExpiry;

        /// <summary>큐별로 다음 재생이 허용되는 시각.</summary>
        private readonly Dictionary<int, float> _cueAllowedAt = new();

        /// <summary>
        /// 동시에 울릴 수 있는 효과음 수를 정해 예산을 연다.
        /// </summary>
        /// <param name="maxConcurrent">
        /// 1 이상이어야 한다. <c>0</c> 이면 소리가 하나도 나지 않아, 무음의 원인을 재생 쪽에서
        /// 찾게 된다 — 경계에서 한 번 막는다.
        /// </param>
        public SoundBudget(int maxConcurrent)
        {
            if (maxConcurrent < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxConcurrent), maxConcurrent, "동시 재생 상한은 1 이상이어야 합니다.");
            }

            _slotExpiry = new float[maxConcurrent];
            ClearSlots();
        }

        /// <summary>지금 울리고 있는 효과음 수. 만료된 것은 세지 않는다.</summary>
        public int ActiveCount(float now)
        {
            var active = 0;
            for (var i = 0; i < _slotExpiry.Length; i++)
            {
                if (_slotExpiry[i] > now)
                {
                    active++;
                }
            }

            return active;
        }

        /// <summary>
        /// 재생을 요청한다. 허용되면 예산을 소모하고 <c>true</c> 를 돌려준다.
        ///
        /// <para>
        /// 먹힘마다 불리는 경로다 — <b>할당을 만들지 않는다</b> (<c>.claude/rules/scripts.md</c> §4).
        /// 슬롯은 생성자에서 한 번 잡고, 큐 키는 개수가 고정이라 첫 재생 이후로는 사전에
        /// 새 항목이 들어가지 않는다.
        /// </para>
        /// </summary>
        /// <param name="cueId">간격을 구분하는 키. 큐마다 고유하면 값 자체는 무관하다.</param>
        /// <param name="now">호출 시각(초). 단조 증가여야 한다.</param>
        /// <param name="cooldownSeconds">같은 큐를 다시 낼 수 있을 때까지의 최소 간격.</param>
        /// <param name="durationSeconds">이 소리가 슬롯을 붙들고 있을 시간.</param>
        public bool TryPlay(int cueId, float now, float cooldownSeconds, float durationSeconds)
        {
            if (_cueAllowedAt.TryGetValue(cueId, out var allowedAt) && now < allowedAt)
            {
                return false;
            }

            var slot = FindFreeSlot(now);
            if (slot < 0)
            {
                return false;
            }

            _slotExpiry[slot] = now + durationSeconds;
            _cueAllowedAt[cueId] = now + cooldownSeconds;
            return true;
        }

        /// <summary>
        /// 슬롯과 간격을 전부 비운다. 스테이지 재시작처럼 <b>시각이 되감기는</b> 지점에서
        /// 부른다 — 그러지 않으면 직전 판의 만료 시각이 미래로 남아 소리가 한동안 죽는다.
        /// </summary>
        public void Reset()
        {
            ClearSlots();
            _cueAllowedAt.Clear();
        }

        private int FindFreeSlot(float now)
        {
            for (var i = 0; i < _slotExpiry.Length; i++)
            {
                if (_slotExpiry[i] <= now)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ClearSlots()
        {
            for (var i = 0; i < _slotExpiry.Length; i++)
            {
                _slotExpiry[i] = NeverOccupied;
            }
        }
    }
}
