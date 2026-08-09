using TMPro;
using UnityEngine;

namespace SushiDefense.UI
{
    /// <summary>
    /// 손님 정보 창의 화면. <b>판정하지 않는다</b> — 프레젠터가 만든 글자를 라벨로 옮기기만
    /// 한다 (<c>CLAUDE.md</c> §3.2·§3.6).
    ///
    /// <para>
    /// <b>정적 줄과 실시간 줄이 다른 메서드로 들어온다.</b> 매 프레임 도는 것은 뒤쪽뿐이라,
    /// 한 메서드가 여섯 라벨을 전부 쓰면 안 바뀐 스탯 문자열까지 매 프레임 대입하게 된다.
    /// </para>
    /// </summary>
    public sealed class CustomerInspectorView : MonoBehaviour, ICustomerInspectorView
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string NameLabelName = "NameLabel";

        private const string KindLabelName = "KindLabel";
        private const string StatsLabelName = "StatsLabel";
        private const string StateLabelName = "StateLabel";
        private const string SaturationLabelName = "SaturationLabel";
        private const string RemainingLabelName = "RemainingLabel";

        /// <summary>켜고 끌 대상. 비어 있으면 이 오브젝트다.</summary>
        [SerializeField] private GameObject _panel;

        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _kindLabel;
        [SerializeField] private TMP_Text _statsLabel;
        [SerializeField] private TMP_Text _stateLabel;
        [SerializeField] private TMP_Text _saturationLabel;
        [SerializeField] private TMP_Text _remainingLabel;

        /// <summary>참조를 이미 챙겼나.</summary>
        private bool _resolved;

        /// <summary>지금 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <inheritdoc />
        public void ShowCustomer(string name, string kind, string stats)
        {
            Resolve();

            HudLabel.Write(_nameLabel, name);
            HudLabel.Write(_kindLabel, kind);
            HudLabel.Write(_statsLabel, stats);

            IsShowing = true;
            Apply(true);
        }

        /// <inheritdoc />
        public void RefreshLive(string state, string saturation, string remaining)
        {
            Resolve();

            HudLabel.Write(_stateLabel, state);
            HudLabel.Write(_saturationLabel, saturation);
            HudLabel.Write(_remainingLabel, remaining);
        }

        /// <inheritdoc />
        public void Hide()
        {
            Resolve();

            IsShowing = false;
            Apply(false);
        }

        private void Awake()
        {
            Resolve();
            Hide();
        }

        /// <summary>
        /// 자기 참조를 <b>한 번만</b> 챙긴다. <see cref="Awake"/> 뿐 아니라 그리는 경로에서도
        /// 부르므로 <b>활성화 순서에 기대지 않는다.</b>
        ///
        /// <para>
        /// 이 창은 씬에서 <b>꺼진 채로</b> 시작한다. 꺼진 오브젝트의 <c>Awake</c> 는 켜질
        /// 때까지 오지 않으므로, 챙기는 일을 거기에만 두면 <b>첫 번째 열기에서 라벨이 전부
        /// <c>null</c></b> 이라 빈 창이 뜬다 — 예외도 경고도 없고 두 번째부터 정상으로 보이는
        /// 것이 그 서명이다 (<c>CardView</c>·<c>CustomerView</c> 와 같은 사고다).
        /// </para>
        /// </summary>
        private void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            if (_panel == null)
            {
                _panel = gameObject;
            }

            _nameLabel = HudLabel.Resolve(transform, _nameLabel, NameLabelName);
            _kindLabel = HudLabel.Resolve(transform, _kindLabel, KindLabelName);
            _statsLabel = HudLabel.Resolve(transform, _statsLabel, StatsLabelName);
            _stateLabel = HudLabel.Resolve(transform, _stateLabel, StateLabelName);
            _saturationLabel = HudLabel.Resolve(transform, _saturationLabel, SaturationLabelName);
            _remainingLabel = HudLabel.Resolve(transform, _remainingLabel, RemainingLabelName);
        }

        /// <summary>
        /// 켜고 끄는 유일한 경로. <see cref="Awake"/> 가 돌기 전에 불릴 수 있어 대상을
        /// 여기서 한 번 더 확인한다.
        /// </summary>
        private void Apply(bool showing)
        {
            var target = _panel != null ? _panel : gameObject;
            target.SetActive(showing);
        }
    }
}
