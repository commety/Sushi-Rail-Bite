using SushiDefense.UI;

namespace SushiDefense.Tests.EditMode.UI
{
    /// <summary>
    /// 정보 창의 손으로 쓴 스텁. 프레젠터가 <b>무엇을 · 몇 번</b> 넘겼는지를 기록한다.
    ///
    /// <para>
    /// 호출 횟수를 따로 세는 이유: 이 프레젠터의 계약 절반이 «값이 안 바뀌면 뷰를 부르지
    /// 않는다» 라, 마지막 값만 봐서는 <b>매 프레임 다시 그리는 구현</b>과 구분되지 않는다.
    /// </para>
    /// </summary>
    internal sealed class FakeCustomerInspectorView : ICustomerInspectorView
    {
        public int ShowCount { get; private set; }

        public int RefreshCount { get; private set; }

        public int HideCount { get; private set; }

        public string LastName { get; private set; }

        public string LastStats { get; private set; }

        public string LastState { get; private set; }

        public string LastSaturation { get; private set; }

        public string LastRemaining { get; private set; }

        public void ShowCustomer(string name, string stats)
        {
            ShowCount++;
            LastName = name;
            LastStats = stats;
        }

        public void RefreshLive(string state, string saturation, string remaining)
        {
            RefreshCount++;
            LastState = state;
            LastSaturation = saturation;
            LastRemaining = remaining;
        }

        public void Hide()
        {
            HideCount++;
        }
    }
}
