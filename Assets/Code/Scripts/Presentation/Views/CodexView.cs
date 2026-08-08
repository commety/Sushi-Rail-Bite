using System.Collections.Generic;
using SushiDefense.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SushiDefense.UI
{
    /// <summary>
    /// 백과사전 화면 — 이 게임에 존재하는 초밥과 손님을 카드로 늘어놓는다.
    ///
    /// <para>
    /// <b>규칙이 하나도 없다.</b> 무엇을 보여줄지는 <see cref="CodexPresenter"/> 가 정한다
    /// (<c>DeckPanelView</c>·<c>RewardSelectionView</c> 와 같은 형태다).
    /// </para>
    /// <para>
    /// <b>카드를 런타임에 만들지 않는다</b> (§3.4). 자리를 미리 놓아 두고 켜고 끈다 —
    /// 자리 수는 카탈로그 전체를 덮어야 하지만, <b>모자라도 예외를 내지 않고 있는 만큼만
    /// 그린다.</b> 씬을 조금씩 조립하는 동안 나머지를 아무것도 확인할 수 없게 되기 때문이다.
    /// </para>
    /// </summary>
    public sealed class CodexView : MonoBehaviour, ICodexView
    {
        /// <summary>인스펙터가 비었을 때 자기 하위에서 찾을 자식 이름. 씬 조립과의 약속이다.</summary>
        private const string PanelRootName = "Panel";

        private const string HeaderLabelName = "CodexHeader";

        /// <summary>패널 위쪽 고정 제목.</summary>
        private const string HeaderText = "백과사전";

        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _headerLabel;
        [SerializeField] private CardView[] _cards;
        [SerializeField] private Button _closeButton;

        private CodexPresenter _presenter;

        /// <summary>화면이 떠 있나. 표시 상태이지 판정이 아니다.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>지금 그린 카드 수. 자리보다 목록이 길면 자리 수에서 멈춘다. 검증용이다.</summary>
        public int ShownCardCount { get; private set; }

        /// <summary>입력을 받을 프레젠터를 물린다. 씬 진입점이 부른다.</summary>
        public void Bind(CodexPresenter presenter)
        {
            _presenter = presenter;
        }

        /// <summary>인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.</summary>
        public void Initialize(GameObject panelRoot, CardView[] cards, Button closeButton)
        {
            Unsubscribe();

            _panelRoot = panelRoot;
            _cards = cards;
            _closeButton = closeButton;

            Subscribe();
        }

        /// <inheritdoc />
        public void ShowEntries(IReadOnlyList<SushiData> sushi,
                                IReadOnlyList<CustomerData> customers)
        {
            IsShowing = true;
            ShownCardCount = 0;

            if (_cards != null)
            {
                var sushiCount = sushi != null ? sushi.Count : 0;
                var customerCount = customers != null ? customers.Count : 0;

                for (var i = 0; i < _cards.Length; i++)
                {
                    var card = _cards[i];
                    if (card == null)
                    {
                        continue;
                    }

                    // 초밥이 먼저, 손님이 뒤. 카탈로그의 순서를 그대로 잇는다 — 흔들리면
                    // 같은 화면이 열 때마다 달라 보인다.
                    if (i < sushiCount)
                    {
                        card.Show(sushi[i]);
                        ShownCardCount++;
                    }
                    else if (i - sushiCount < customerCount)
                    {
                        card.Show(customers[i - sushiCount]);
                        ShownCardCount++;
                    }
                    else
                    {
                        card.Clear();
                    }
                }
            }

            SetPanelActive(true);
        }

        /// <inheritdoc />
        public void Hide()
        {
            IsShowing = false;
            ShownCardCount = 0;

            if (_cards != null)
            {
                for (var i = 0; i < _cards.Length; i++)
                {
                    _cards[i]?.Clear();
                }
            }

            SetPanelActive(false);
        }

        private void Awake()
        {
            if (_panelRoot == null)
            {
                var child = transform.Find(PanelRootName);
                _panelRoot = child != null ? child.gameObject : null;
            }

            _headerLabel = HudLabel.Resolve(_panelRoot != null ? _panelRoot.transform : transform,
                                            _headerLabel, HeaderLabelName);

            if (_cards == null || _cards.Length == 0)
            {
                _cards = GetComponentsInChildren<CardView>(true);
            }

            // 고정 문구라 한 번만 쓴다. 열 때마다 쓰면 문자열이 하나씩 생긴다 (§4.3).
            HudLabel.Write(_headerLabel, HeaderText);

            Subscribe();
            Hide();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void Unsubscribe()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void OnCloseClicked()
        {
            _presenter?.Close();
        }

        private void SetPanelActive(bool active)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(active);
            }
        }
    }
}
