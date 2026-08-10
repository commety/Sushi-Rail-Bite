using System.Runtime.CompilerServices;

// 런타임 상태 컨테이너의 세터는 같은 어셈블리의 로직(M2 CustomerLogic 등)만 만지게 internal 로
// 좁혀 두었다. 테스트가 상태를 꾸며 놓고 검증하려면 그 internal 이 보여야 한다 —
// 프로덕션에 public 세터를 여는 대신 테스트 어셈블리에만 노출한다 (.claude/rules/tests.md §7).
[assembly: InternalsVisibleTo("Tests.EditMode")]

// PlayMode 도 같은 이유로 필요해졌다 — 포화도 바는 MonoBehaviour 라 이 프로젝트의 관례상
// PlayMode 에 있고, "세 칸 찬 손님" 을 그리려면 그 상태를 꾸며 놓을 수 있어야 한다.
// 실제 로직으로 초밥을 먹여 만들 수도 있지만, 그러면 표시 테스트가 집기 규칙까지 끌고 온다.
[assembly: InternalsVisibleTo("Tests.PlayMode")]
