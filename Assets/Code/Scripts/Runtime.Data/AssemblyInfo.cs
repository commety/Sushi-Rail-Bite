using System.Runtime.CompilerServices;

// 데이터 검증(OnValidate)은 인스펙터가 아니라 EditMode 테스트로 고정한다.
// 테스트를 위해 public 세터를 여는 대신 internal 만 노출한다 (.claude/rules/tests.md §7).
[assembly: InternalsVisibleTo("Tests.EditMode")]
