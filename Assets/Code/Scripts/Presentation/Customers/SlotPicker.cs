using System.Collections.Generic;
using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 화면의 한 점이 <b>어느 자리를 가리키는지</b> 정한다.
    ///
    /// <para>
    /// 물리를 쓰지 않는다. 자리는 넷뿐이고 좌표가 이미 알려져 있어 거리 비교로 끝난다 —
    /// 콜라이더를 붙이면 씬에 관리할 것이 늘고, 물리 질의는 호출 시점에 제약이 붙는다
    /// (RULE-04).
    /// </para>
    /// <para>
    /// <c>MonoBehaviour</c> 밖에 있는 이유: "어디를 눌렀나" 는 마우스가 없어도 검증할 수 있는
    /// 순수 계산이다 (<c>CLAUDE.md</c> §3.2).
    /// </para>
    /// </summary>
    public static class SlotPicker
    {
        /// <summary>
        /// <paramref name="worldPoint"/> 에서 <paramref name="maxDistance"/> 안에 있는 자리 중
        /// <b>가장 가까운</b> 것. 없으면 <c>null</c>.
        /// </summary>
        /// <remarks>
        /// 동점은 목록 순서가 가른다. 자리끼리 겹치게 놓는 배치는 없으므로 실제로는 나오지
        /// 않지만, 나와도 결과가 흔들리지 않아야 한다.
        /// </remarks>
        public static TableSlotView Pick(Vector2 worldPoint, IReadOnlyList<TableSlotView> slots,
                                         float maxDistance)
        {
            if (slots == null || maxDistance <= 0f)
            {
                return null;
            }

            TableSlotView best = null;
            var bestSqr = maxDistance * maxDistance;

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                var offset = (Vector2)slot.transform.position - worldPoint;
                var sqr = offset.sqrMagnitude;

                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = slot;
                }
            }

            return best;
        }
    }
}
