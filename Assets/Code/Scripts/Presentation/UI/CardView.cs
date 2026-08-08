using System;
using SushiDefense.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SushiDefense.UI
{
    /// <summary>
    /// 카드 한 장의 화면 표현. <b>판정하지 않는다</b> — 무엇을 보여줄지도, 눌린 것이
    /// 유효한지도 부르는 쪽이 정한다.
    ///
    /// <para>
    /// M6 에서 카드가 뜨는 화면이 넷이다 — 손패 · 덱 보기 · 보상 선택 · 백과사전. 각자
    /// 그리면 같은 초밥이 화면마다 다르게 보이고, 카드 모양을 고칠 때 네 곳을 고치게 된다.
    /// <b>틀은 하나</b>이며 등급으로 갈라지지 않는다.
    /// </para>
    /// <para>
    /// <b>스스로 만들어지지 않는다.</b> 각 화면이 필요한 만큼 미리 놓아 두고 켜고 끈다 —
    /// 프로덕션에서 오브젝트를 새로 만드는 지점은 풀 하나로 유지한다 (<c>CLAUDE.md</c> §3.4).
    /// </para>
    /// </summary>
    public sealed class CardView : MonoBehaviour
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 프리팹과의 약속이다.</summary>
        private const string IconName = "Icon";

        private const string NameLabelName = "NameLabel";
        private const string DetailLabelName = "DetailLabel";

        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _detailLabel;
        [SerializeField] private Button _button;

        /// <summary>
        /// 프리팹이 들고 있던 그림. 데이터에 아이콘이 없거나 카드를 비울 때 여기로 돌아간다 —
        /// 남겨 두면 재사용 첫 프레임에 직전 카드의 그림이 번쩍인다.
        /// </summary>
        private Sprite _fallbackSprite;

        /// <summary>지금 표시 중인 이름. 검증용이다.</summary>
        public string NameText { get; private set; } = string.Empty;

        /// <summary>지금 표시 중인 수치 줄. 검증용이다.</summary>
        public string DetailText { get; private set; } = string.Empty;

        /// <summary>지금 화면에 나가 있는 그림. 검증용이다.</summary>
        public Sprite ShownSprite => _icon != null ? _icon.sprite : null;

        /// <summary>카드에 내용이 들어 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>
        /// 이 카드가 눌렸다. <b>몇 번째인지는 부르는 쪽이 안다</b> — 카드가 자기 위치를
        /// 들면 목록이 바뀔 때마다 카드를 다시 물려야 한다.
        /// </summary>
        public event Action<CardView> Clicked;

        /// <summary>초밥 카드를 그린다.</summary>
        public void Show(SushiData sushi)
        {
            Show(CardCaption.NameOf(sushi), CardCaption.DetailOf(sushi),
                 sushi != null ? sushi.Icon : null);
        }

        /// <summary>
        /// 손님 카드를 그린다.
        ///
        /// <para>
        /// <see cref="Show(SushiData)"/> 와 <c>bool</c> 하나로 합치지 않는다. 합치면 뷰 안에
        /// 분기가 생기고 그 분기가 곧 판정이 된다 — <c>IStageTransitionView</c> 가 클리어와
        /// 런 종료를 나눈 것과 같은 이유다.
        /// </para>
        /// </summary>
        public void Show(CustomerData customer)
        {
            Show(CardCaption.NameOf(customer), CardCaption.DetailOf(customer),
                 customer != null ? customer.Icon : null);
        }

        /// <summary>
        /// 빈 자리로 되돌린다. 내용을 지우고 오브젝트를 끈다 —
        /// <c>TableSlotView.Vacate</c> 와 같은 형태다.
        /// </summary>
        public void Clear()
        {
            IsShowing = false;
            NameText = string.Empty;
            DetailText = string.Empty;

            HudLabel.Write(_nameLabel, NameText);
            HudLabel.Write(_detailLabel, DetailText);
            ShowSprite(null);

            gameObject.SetActive(false);
        }

        private void Awake()
        {
            _nameLabel = HudLabel.Resolve(transform, _nameLabel, NameLabelName);
            _detailLabel = HudLabel.Resolve(transform, _detailLabel, DetailLabelName);
            _icon = ResolveIcon();

            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            // 프리팹의 그림을 여기서 붙잡는다. Show 뒤에 읽으면 데이터의 아이콘이 잡힌다.
            _fallbackSprite = _icon != null ? _icon.sprite : null;

            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }

            Clicked = null;
        }

        /// <summary>
        /// 인스펙터에서 비어 있으면 <b>자기 하위 계층에서만</b> 이름으로 찾는다.
        /// <c>FindObjectOfType</c> 같은 씬 전역 탐색이 아니다 (§4.3 금지 대상) —
        /// <see cref="HudLabel.Resolve"/> 와 같은 방식이며, 타입 하나 때문에 그 조각을
        /// 일반화하지는 않았다.
        /// </summary>
        private Image ResolveIcon()
        {
            if (_icon != null)
            {
                return _icon;
            }

            var child = transform.Find(IconName);
            return child != null && child.TryGetComponent<Image>(out var image) ? image : null;
        }

        /// <summary>
        /// 그리는 유일한 경로. 두 <c>Show</c> 가 각자 라벨을 쓰면 한쪽만 고쳐지는 날이 온다.
        /// </summary>
        private void Show(string name, string detail, Sprite icon)
        {
            IsShowing = true;
            NameText = name;
            DetailText = detail;

            HudLabel.Write(_nameLabel, NameText);
            HudLabel.Write(_detailLabel, DetailText);
            ShowSprite(icon);

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 아이콘이 비면 프리팹의 그림을 그대로 둔다. 빈 화면을 만들지 않는다 —
        /// 아이콘을 아직 안 채운 애셋에서 카드가 사라지면 목록이 고장 난 것처럼 보인다
        /// (<c>SushiItemView</c>·<c>CustomerView</c> 와 같은 처리다).
        /// </summary>
        private void ShowSprite(Sprite icon)
        {
            if (_icon == null)
            {
                return;
            }

            _icon.sprite = icon != null ? icon : _fallbackSprite;
        }

        /// <summary>
        /// 빈 카드는 눌려도 아무 일이 없다. 목록이 자리보다 짧을 때 남는 카드가 눌리면
        /// 부르는 쪽이 없는 항목을 고르게 된다.
        /// </summary>
        private void OnButtonClicked()
        {
            if (!IsShowing)
            {
                return;
            }

            Clicked?.Invoke(this);
        }
    }
}
