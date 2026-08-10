# `gh-pages` — 배포 산출물 전용 브랜치

이 브랜치에는 **소스가 없다.** GitHub Pages 가 그대로 서빙하는 Unity WebGL 산출물만 있다.
소스는 `release/*` · `dev` · `main` 에 있다.

공개 주소: <https://commety.github.io/Sushi-Rail-Bite/>

## 다시 배포하는 법

Unity WebGL 은 에디터 라이선스가 필요해 CI 에서 굽지 못한다. **로컬에서 구워 올린다.**

```bash
# 1) 소스 브랜치에서 굽는다 (트리가 깨끗한 상태여야 산출물이 커밋과 대응한다)
git checkout release/v-1-0-0
./scripts/run.sh webgl
```

```bash
# 2) 산출물을 이 브랜치로 옮긴다. FP 는 빌드 폴더 이름.
FP=main-release-v-1-0-0-XXXXXXX-WebGL-XXXXXXXX-XXXXXX
B=../builds/$FP
cp -R "$B/Build" "$B/TemplateData" "$B/index.html" "$B/BUILD_INFO.txt" .
for e in data wasm framework.js loader.js; do mv "Build/$FP.$e" "Build/SushiRailBite.$e"; done
sed -i '' "s/$FP/SushiRailBite/g" index.html
```

## 손으로 유지해야 하는 것

빌드가 새로 나올 때마다 사라지므로 매번 다시 넣는다.

| | 왜 |
|---|---|
| `Build/*` 를 `SushiRailBite.*` 로 개명 + `index.html` 치환 | 기본 파일명에 브랜치·커밋·타임스탬프가 박혀 URL 이 지저분해진다 |
| `index.html` 의 `<title>` | 기본값이 `Unity Web Player \| Sushi-Rail-Bite` 다. 바꾸려면 `ProjectSettings` 의 `productName` 을 고쳐야 하는데 그건 별도 승인 사항이라 산출물에서 고친다 |
| `.nojekyll` | 없으면 Jekyll 이 끼어든다 |
| `.gitattributes` | **가장 중요.** LFS 가 켜지면 Pages 가 `.wasm`·`.data` 를 포인터 텍스트로 서빙해 사이트가 죽는다 |

## 압축을 켜지 않는 이유

`webGLCompressionFormat` 이 **Disabled** 라 산출물이 약 53 MB 로 크다.
Brotli/Gzip 을 켜면 1/4 로 줄지만 서버가 `Content-Encoding` 헤더를 붙여야 하는데
**GitHub Pages 는 응답 헤더를 설정할 수 없다.**

줄이고 싶다면 Unity 의 **Decompression Fallback** 을 함께 켜야 한다 (로더가 JS 로 직접 푼다).
`ProjectSettings` 변경이므로 사람의 승인이 필요하다.
