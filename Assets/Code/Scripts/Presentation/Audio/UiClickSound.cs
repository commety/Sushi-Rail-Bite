using SushiDefense.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SushiDefense.Audio
{
    /// <summary>
    /// 자기 하위의 <see cref="Button"/> 이 눌리면 클릭음을 낸다.
    ///
    /// <para>
    /// <b>버튼만이다.</b> 카드 선택(보상·백과사전)과 손님 배치는 이 소리를 쓰지 않는다
    /// (아키텍트 결정) — 클릭음은 «메뉴를 조작했다» 를 위한 것이고, 카드를 고르거나 손님을
    /// 앉히는 것은 게임 안의 사건이라 자기 큐가 따로 있다. 같은 소리를 쓰면 메뉴 조작과
    /// 플레이가 청각적으로 구분되지 않는다.
    /// </para>
    /// <para>
    /// 가르는 방법은 <b><see cref="CardView"/> 를 가진 오브젝트를 건너뛰는 것</b> 하나다.
    /// 카드는 자기 오브젝트에 <c>Button</c> 을 달고 있어서, 이 규칙 없이 하위를 훑으면
    /// 카드까지 딸려 온다. 목록을 인스펙터로 손수 물리지 않는 이유는 버튼이 늘 때마다
    /// 빠뜨리기 때문이고, 빠뜨려도 <b>화면은 멀쩡해서</b> 눈에 띄지 않는다.
    /// </para>
    /// <para>
    /// <c>FindObjectOfType</c> 을 쓰지 않는다 (§4.3). 소리를 낼 주체는 인스펙터로 받고,
    /// 대상 버튼은 <b>자기 하위</b>에서만 찾는다.
    /// </para>
    /// </summary>
    public sealed class UiClickSound : MonoBehaviour
    {
        [SerializeField] private AudioDirector _director;

        private Button[] _buttons;

        /// <summary>구독한 버튼 수. 검증용이다.</summary>
        public int HookedCount { get; private set; }

        /// <summary>인스펙터 없이 참조를 물린다. 테스트·부트스트랩용 진입점이다.</summary>
        public void Initialize(AudioDirector director)
        {
            Unsubscribe();
            _director = director;
            Subscribe();
        }

        private void Awake()
        {
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            // 꺼져 있는 패널의 버튼도 잡는다 — 설정·사전은 처음에 꺼진 채로 시작한다.
            _buttons = GetComponentsInChildren<Button>(true);
            HookedCount = 0;

            for (var i = 0; i < _buttons.Length; i++)
            {
                var button = _buttons[i];
                if (button == null || IsCard(button))
                {
                    continue;
                }

                button.onClick.AddListener(OnClicked);
                HookedCount++;
            }
        }

        private void Unsubscribe()
        {
            if (_buttons == null)
            {
                return;
            }

            for (var i = 0; i < _buttons.Length; i++)
            {
                var button = _buttons[i];
                if (button != null && !IsCard(button))
                {
                    button.onClick.RemoveListener(OnClicked);
                }
            }

            _buttons = null;
            HookedCount = 0;
        }

        private static bool IsCard(Button button)
        {
            return button.TryGetComponent<CardView>(out _);
        }

        private void OnClicked()
        {
            if (_director != null)
            {
                _director.PlayUiClick();
            }
        }
    }
}
