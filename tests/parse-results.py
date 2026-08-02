#!/usr/bin/env python3
"""NUnit3 XML(Unity Test Framework 산출물)을 사람·에이전트가 읽을 요약으로 줄인다.

왜 필요한가: Unity 가 뱉는 results.xml 은 테스트 수십 개만 돼도 수백 줄이다.
에이전트가 커밋마다 이걸 통째로 읽으면 컨텍스트를 그대로 태운다. 여기서
"몇 개 중 몇 개 통과 + 실패한 것만" 으로 줄인다.

또 하나: 배치모드는 **컴파일 실패와 테스트 실패가 겉보기에 비슷하다.**
XML 이 아예 없으면 로그에서 `error CS` 를 찾아 컴파일 실패로 구분해 보고한다.

사용:
    parse-results.py <results.xml> [--log <unity.log>] [--max-failures N]

종료 코드:
    0  전부 통과
    1  테스트 실패 있음
    2  컴파일 실패 — 테스트가 아예 돌지 않음
    3  결과 파일 없음/파손 (원인 불명)
"""

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

# 실패 1건당 출력할 스택 줄 수. 넘기면 컨텍스트만 먹고 진단에 도움이 안 된다.
STACK_LINES = 6
COMPILE_ERROR_RE = re.compile(r"error CS\d+")


def compile_errors(log_path):
    """로그에서 C# 컴파일 에러 줄을 중복 없이 뽑는다."""
    if not log_path or not log_path.exists():
        return []

    seen, out = set(), []
    for line in log_path.read_text(errors="replace").splitlines():
        if COMPILE_ERROR_RE.search(line):
            line = line.strip()
            if line not in seen:
                seen.add(line)
                out.append(line)
    return out


def collect_failures(root):
    """result="Failed" 인 test-case 만 평탄하게 수집한다."""
    failures = []
    for case in root.iter("test-case"):
        if case.get("result") != "Failed":
            continue

        failure = case.find("failure")
        message = stack = ""
        if failure is not None:
            msg_node = failure.find("message")
            stack_node = failure.find("stack-trace")
            message = (msg_node.text or "").strip() if msg_node is not None else ""
            stack = (stack_node.text or "").strip() if stack_node is not None else ""

        failures.append((case.get("fullname") or case.get("name") or "?", message, stack))
    return failures


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("results")
    parser.add_argument("--log")
    parser.add_argument("--max-failures", type=int, default=10)
    args = parser.parse_args()

    results_path = Path(args.results)
    log_path = Path(args.log) if args.log else None

    if not results_path.exists():
        errors = compile_errors(log_path)
        if errors:
            print("컴파일 실패 — 테스트가 실행되지 않았습니다.\n")
            for line in errors[:20]:
                print(f"  {line}")
            if len(errors) > 20:
                print(f"  … 외 {len(errors) - 20}건")
            return 2

        print(f"결과 파일이 없습니다: {results_path}")
        if log_path:
            print(f"로그를 확인하세요: {log_path}")
        return 3

    try:
        root = ET.parse(results_path).getroot()
    except ET.ParseError as exc:
        print(f"결과 파일을 읽을 수 없습니다: {results_path}\n  {exc}")
        return 3

    total = int(root.get("total") or 0)
    passed = int(root.get("passed") or 0)
    failed = int(root.get("failed") or 0)
    skipped = int(root.get("skipped") or 0)
    inconclusive = int(root.get("inconclusive") or 0)
    duration = root.get("duration") or "?"

    # 테스트가 0개인 것은 "통과" 가 아니다. TDD 가 기본값인 프로젝트에서
    # 스위트가 비었는데 Green 으로 보고되면 게이트가 조용히 무력화된다.
    if total == 0:
        print("테스트가 0개입니다 — 스위트가 비었거나 어셈블리가 발견되지 않았습니다.")
        print("  Assets/Tests/{EditMode,PlayMode} 의 .asmdef 설정을 확인하세요.")
        return 3

    status = "통과" if failed == 0 else "실패"
    summary = f"{status}: {passed}/{total}"
    if failed:
        summary += f"  실패 {failed}"
    if skipped:
        summary += f"  건너뜀 {skipped}"
    if inconclusive:
        summary += f"  미결 {inconclusive}"
    print(f"{summary}  ({duration}s)")

    if failed == 0:
        return 0

    failures = collect_failures(root)
    print()
    for name, message, stack in failures[: args.max_failures]:
        print(f"✗ {name}")
        if message:
            for line in message.splitlines():
                print(f"    {line}")
        if stack:
            for line in stack.splitlines()[:STACK_LINES]:
                print(f"    | {line}")
        print()

    if len(failures) > args.max_failures:
        print(f"… 외 {len(failures) - args.max_failures}건 (전체는 {results_path})")

    return 1


if __name__ == "__main__":
    sys.exit(main())
