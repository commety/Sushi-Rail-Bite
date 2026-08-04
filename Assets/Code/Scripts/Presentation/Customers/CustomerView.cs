using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님 하나의 화면 표현. <see cref="CustomerLogic"/> 에 위임만 한다 (<c>CLAUDE.md</c> §3.2).
    /// 자격·배정 판단을 여기에 두지 않는다.
    /// </summary>
    public sealed class CustomerView : MonoBehaviour
    {
        /// <summary>이 뷰가 그리고 있는 손님 로직.</summary>
        public CustomerLogic Logic { get; private set; }

        /// <summary>배치 시점에 로직을 물린다. 뷰가 로직을 스스로 만들지 않는다.</summary>
        public void Bind(CustomerLogic logic)
        {
            Logic = logic;
        }
    }
}
