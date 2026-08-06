using System.Collections.Generic;
using SushiDefense.Data;

namespace SushiDefense.Run
{
    /// <summary>
    /// 이 런이 배치할 수 있는 손님 명부. <see cref="SushiDeck"/> 과 같은 규칙으로 자란다 —
    /// 클리어 보상이 손님을 영입한다.
    ///
    /// <para>
    /// <b>배치된 손님이 아니다.</b> 지금 자리에 앉아 있는 손님은
    /// <c>CustomerPlacementService</c> 가 알고, 스테이지가 끝나면 사라진다. 여기 있는 것은
    /// "이 런에서 앉힐 수 있는 종류" 다.
    /// </para>
    /// </summary>
    public sealed class CustomerDeck
    {
        private readonly List<CustomerData> _members = new();

        /// <summary>명부에 든 손님. 추가 순서를 유지한다.</summary>
        public IReadOnlyList<CustomerData> Members => _members;

        /// <summary>명부에 든 손님 수.</summary>
        public int Count => _members.Count;

        /// <summary>빈 명부로 시작한다.</summary>
        public CustomerDeck()
        {
        }

        /// <summary>
        /// 주어진 손님으로 명부를 연다. <c>null</c> 과 중복은 빠지므로
        /// <see cref="TryAdd"/> 를 반복한 것과 같은 결과가 나온다.
        /// </summary>
        public CustomerDeck(IEnumerable<CustomerData> members)
        {
            if (members == null)
            {
                return;
            }

            foreach (var member in members)
            {
                TryAdd(member);
            }
        }

        /// <summary>이 손님이 명부에 있나.</summary>
        public bool Contains(CustomerData member)
        {
            return member != null && _members.Contains(member);
        }

        /// <summary>
        /// 손님을 더한다. 이미 있거나 <c>null</c> 이면 <c>false</c> 를 돌려주고
        /// <b>명부를 그대로 둔다</b>.
        /// </summary>
        public bool TryAdd(CustomerData member)
        {
            if (member == null || _members.Contains(member))
            {
                return false;
            }

            _members.Add(member);
            return true;
        }
    }
}
