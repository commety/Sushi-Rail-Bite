using SushiDefense.Data;
using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님을 놓는 고정 자리. 점유 여부와 벨트 좌표를 들고 있고, 배치 규칙은 갖지 않는다 —
    /// "놓을 수 있나" 는 <c>CustomerPlacementService</c> 가 답한다 (step-08).
    /// </summary>
    public sealed class TableSlotView : MonoBehaviour
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private float _beltPosition;

        /// <summary>스테이지 안에서 이 자리를 가리키는 번호.</summary>
        public int SlotIndex => _slotIndex;

        /// <summary>집기 범위 판정의 기준이 되는 1차원 벨트 좌표.</summary>
        public float BeltPosition => _beltPosition;

        /// <summary>지금 손님이 앉아 있는가.</summary>
        public bool IsOccupied => Occupant != null;

        /// <summary>앉아 있는 손님의 뷰. 비어 있으면 <c>null</c>.</summary>
        public CustomerView Occupant { get; private set; }

        /// <summary>스테이지 정의에서 자리 정보를 받는다. 화면 위치도 함께 맞춘다.</summary>
        public void Bind(TableSlotDefinition definition)
        {
            _slotIndex = definition.SlotIndex;
            _beltPosition = definition.BeltPosition;
            transform.localPosition = definition.Position;
        }

        /// <summary>손님을 앉힌다.</summary>
        public void Occupy(CustomerView customer)
        {
            Occupant = customer;
        }

        /// <summary>자리를 비운다.</summary>
        public void Vacate()
        {
            Occupant = null;
        }
    }
}
