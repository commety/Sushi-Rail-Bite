using System;
using UnityEngine;

namespace SushiDefense.Data
{
    /// <summary>
    /// 손님이 앉을 수 있는 고정 자리 하나의 정의. 실제 배치·점유 상태는 런타임이 갖는다.
    /// M0 은 자리 식별과 위치까지만 정의한다 — 자리별 제약(유형 제한 등)이 필요해지면 M3 에서 넓힌다.
    /// </summary>
    [Serializable]
    public sealed class TableSlotDefinition
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private Vector2 _position;

        /// <summary>스테이지 안에서 이 자리를 가리키는 번호.</summary>
        public int SlotIndex => _slotIndex;

        /// <summary>벨트 기준 자리 위치. 집기 범위 계산의 기준점이 된다.</summary>
        public Vector2 Position => _position;
    }
}
