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
        [SerializeField] private float _beltPosition;

        /// <summary>스테이지 안에서 이 자리를 가리키는 번호.</summary>
        public int SlotIndex => _slotIndex;

        /// <summary>씬에 자리를 놓을 화면 좌표. 표현 전용이다.</summary>
        public Vector2 Position => _position;

        /// <summary>
        /// 이 자리의 1차원 벨트 좌표. 집기 범위는 여기서 <c>±Reach</c> 로 잡힌다.
        ///
        /// <para>
        /// <see cref="Position"/> 과 합치지 않는 이유: 벨트는 화면에서 곡선이거나 꺾일 수 있고,
        /// 그때 화면 좌표와 벨트 진행 거리는 서로 다른 값이 된다. 판정은 언제나 이 1차원 좌표로 한다.
        /// </para>
        /// <para>
        /// 음수가 허용된다 — 벨트 좌표 원점을 어디로 잡을지는 씬 조립에서 정해지므로,
        /// 시작점보다 앞에 있는 자리가 정상일 수 있다.
        /// </para>
        /// </summary>
        public float BeltPosition => _beltPosition;
    }
}
