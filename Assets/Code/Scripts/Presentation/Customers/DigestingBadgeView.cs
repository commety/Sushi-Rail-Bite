using SushiDefense.UI;
using TMPro;
using UnityEngine;

namespace SushiDefense.Customers
{
    /// <summary>
    /// 손님이 쉬는 중임과 <b>언제 끝나는지</b>를 그 위에 작게 알린다.
    ///
    /// <para>
    /// <b>상태를 모른다.</b> 언제 뜰지도 몇 초 남았는지도 <see cref="CustomerView"/> 가 정하고
    /// 여기는 <see cref="Show"/>/<see cref="Hide"/> 만 받는다 — 뷰가 상태를 되물으면 상태 머신의
    /// 진실이 둘이 된다 (<c>CLAUDE.md</c> §3.2·§3.5).
    /// </para>
    /// <para>
    /// <b>아이콘이 상태를, 숫자가 남은 시간을 진다.</b> 16×16 px 안에 12 px 픽셀 폰트로
    /// «소화중» 세 글자는 들어가지 않지만 <b>한 자리 숫자는 들어간다</b> — 소화 시간이
    /// 3~3.5초라 실제로 한 자리다. 두 자리가 되는 밸런스 변경이 오면 이 표시가 먼저 깨지므로,
    /// 그때는 배지 크기를 다시 정해야 한다.
    /// </para>
    /// </summary>
    public sealed class DigestingBadgeView : MonoBehaviour
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 프리팹과의 약속이다.</summary>
        private const string RemainingLabelName = "RemainingLabel";

        /// <summary>켜고 끌 대상. 비어 있으면 이 오브젝트다.</summary>
        [SerializeField] private GameObject _visual;

        /// <summary>남은 초를 적을 라벨. 없으면 표시만 빠진다.</summary>
        [SerializeField] private TMP_Text _remainingLabel;

        /// <summary>참조를 이미 챙겼나.</summary>
        private bool _resolved;

        /// <summary>지금 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>지금 배지에 적혀 있는 글자. 검증용이다.</summary>
        public string RemainingText { get; private set; } = string.Empty;

        /// <summary>
        /// 배지를 띄우고 남은 초를 적는다.
        /// </summary>
        /// <param name="remainingSeconds">
        /// <b>이미 초 단위로 잘린 값</b>을 받는다. 자르는 것은 부르는 쪽의 일이다 — 여기서
        /// 자르면 «값이 바뀐 프레임에만 쓴다» 를 판단할 곳이 둘로 갈린다.
        /// </param>
        public void Show(int remainingSeconds)
        {
            Resolve();

            RemainingText = remainingSeconds.ToString();
            HudLabel.Write(_remainingLabel, RemainingText);
            Apply(true);
        }

        /// <summary>
        /// 배지를 내린다. <b>글자도 함께 지운다</b> — 남겨 두면 다음 소화가 시작될 때
        /// 직전 숫자가 한 프레임 번쩍인다.
        /// </summary>
        public void Hide()
        {
            Resolve();

            RemainingText = string.Empty;
            HudLabel.Write(_remainingLabel, RemainingText);
            Apply(false);
        }

        private void Awake()
        {
            Resolve();
            Hide();
        }

        /// <summary>
        /// 자기 참조를 <b>한 번만</b> 챙긴다. <see cref="Awake"/> 뿐 아니라 그리는 경로에서도
        /// 부르므로 활성화 순서에 기대지 않는다 — 배지는 프리팹에서 꺼진 채 시작하고, 꺼진
        /// 오브젝트의 <c>Awake</c> 는 켜질 때까지 오지 않는다
        /// (<c>.claude/knowledge/unity-scripting-gotchas.md</c> §5).
        /// </summary>
        private void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            if (_visual == null)
            {
                _visual = gameObject;
            }

            _remainingLabel = HudLabel.Resolve(transform, _remainingLabel, RemainingLabelName);
        }

        /// <summary>
        /// 켜고 끄는 유일한 경로. <see cref="Awake"/> 가 돌기 전에 불릴 수 있어
        /// (프리팹에서 꺼진 채로 시작한다) 대상을 여기서 한 번 더 확인한다.
        /// </summary>
        private void Apply(bool showing)
        {
            IsShowing = showing;

            var target = _visual != null ? _visual : gameObject;
            target.SetActive(showing);
        }
    }
}
