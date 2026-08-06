using UnityEngine;

namespace SushiDefense.UI
{
    /// <summary>
    /// 스테이지 전환 화면 — 몇 판을 깼고 다음이 무엇인지 한 줄로 알린다.
    ///
    /// <para>
    /// <b>규칙이 하나도 없다.</b> 다음 스테이지가 있는지도, 넘어가도 되는지도
    /// <see cref="StageTransitionPresenter"/> 가 답한다. 여기서 되물으면 두 판정이 어긋날 수
    /// 있고 EditMode 로 검증할 수도 없다 (<c>CLAUDE.md</c> §3.2·§3.6).
    /// </para>
    /// <para>
    /// 렌더링에 <see cref="TextMesh"/> 를 쓴다. 씬에 Canvas 가 없고 전부 월드 스페이스라
    /// <c>StageHudView</c>·<c>RewardSelectionView</c> 와 같은 방식이다.
    /// <b>M6(메인화면·덱빌딩)에서 제대로 된 UI 로 교체될 placeholder 다.</b>
    /// </para>
    /// </summary>
    public sealed class StageTransitionView : MonoBehaviour, IStageTransitionView
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string MessageLabelName = "StageTransitionLabel";

        [SerializeField] private TextMesh _messageLabel;

        private StageTransitionPresenter _presenter;

        /// <summary>지금 표시 중인 문구. 검증용이다.</summary>
        public string MessageText { get; private set; } = string.Empty;

        /// <summary>화면이 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>입력을 받을 프레젠터를 물린다. 씬 진입점이 부른다.</summary>
        public void Bind(StageTransitionPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <inheritdoc />
        public void ShowStageCleared(int clearedStageNumber, int nextStageNumber)
        {
            Show($"스테이지 {clearedStageNumber} 클리어 — 다음: 스테이지 {nextStageNumber} (Enter)");
        }

        /// <inheritdoc />
        public void ShowRunComplete(int clearedStageNumber)
        {
            Show($"스테이지 {clearedStageNumber} 클리어 — 런 완료! (Enter)");
        }

        /// <inheritdoc />
        public void Hide()
        {
            IsShowing = false;
            MessageText = string.Empty;
            PlaceholderLabel.Write(_messageLabel, MessageText);
        }

        private void Awake()
        {
            _messageLabel = PlaceholderLabel.Resolve(transform, _messageLabel, MessageLabelName);
        }

        /// <summary>
        /// 문구를 만드는 <b>유일한 지점</b>. 화면이 뜰 때 한 번만 돌므로 문자열 결합이
        /// 프레임 예산에 닿지 않는다 — <see cref="Update"/> 에서 만들면 WebGL 에서 GC
        /// 스파이크가 그대로 히칭이 된다 (§4.3).
        /// </summary>
        private void Show(string message)
        {
            IsShowing = true;
            MessageText = message;
            PlaceholderLabel.Write(_messageLabel, MessageText);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Enter 로 다음 판에 들어간다. 조작 키를 문구에 넣어 두는 것이 안내의 전부다 —
        /// 버튼 히트박스는 M6 에서 만들 UI 의 몫이다.
        /// </summary>
        private void Update()
        {
            if (_presenter == null || !_presenter.IsOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _presenter.Proceed();
            }
        }
    }
}
