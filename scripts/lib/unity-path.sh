#!/usr/bin/env bash
# unity-path.sh — Unity 에디터 실행 파일 위치를 해석한다.
#
# 이 파일은 실행하지 않고 source 한다:
#     source "$(dirname "$0")/lib/unity-path.sh"
#
# 설정되는 변수:
#   PROJECT_ROOT      Unity 프로젝트 루트 절대경로
#   UNITY_VERSION     ProjectSettings/ProjectVersion.txt 의 m_EditorVersion
#   UNITY_BIN         Unity 실행 파일 절대경로 (검증 완료 — 실행 가능)
#   PLAYBACK_ENGINES  플랫폼 모듈 디렉토리 (빌드 스크립트만 사용)
#
# 실패 시 종료 코드:
#   2  Unity 프로젝트 루트가 아님
#   3  해당 버전의 Unity 에디터가 설치돼 있지 않음
#
# 왜 분리했나: 빌드(scripts/run.sh)와 테스트(tests/run-tests.sh)가 같은
# 경로 해석을 필요로 한다. 복제하면 Unity 버전을 올릴 때 한쪽만 고쳐진다.

if [ ! -f ProjectSettings/ProjectVersion.txt ]; then
    echo "ERROR: Unity 프로젝트 루트에서 실행해야 합니다 (ProjectSettings/ProjectVersion.txt 없음)." >&2
    exit 2
fi

PROJECT_ROOT="$(pwd)"
UNITY_VERSION=$(awk '/m_EditorVersion:/ {print $2; exit}' ProjectSettings/ProjectVersion.txt)

case "$(uname -s)" in
    Darwin)
        UNITY_APP="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app"
        UNITY_BIN="$UNITY_APP/Contents/MacOS/Unity"
        # Unity 6 은 PlaybackEngines 를 .app 번들 밖, 에디터 루트에 둔다.
        PLAYBACK_ENGINES="/Applications/Unity/Hub/Editor/$UNITY_VERSION/PlaybackEngines" ;;
    Linux)
        UNITY_BIN="$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity"
        PLAYBACK_ENGINES="$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/PlaybackEngines" ;;
    *)
        UNITY_BIN="/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe"
        PLAYBACK_ENGINES="/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/PlaybackEngines" ;;
esac

if [ ! -x "$UNITY_BIN" ]; then
    cat >&2 <<EOF
ERROR: Unity 에디터를 찾을 수 없습니다:
  $UNITY_BIN

Unity Hub 로 $UNITY_VERSION 을 설치한 뒤 다시 시도하세요.
EOF
    exit 3
fi
