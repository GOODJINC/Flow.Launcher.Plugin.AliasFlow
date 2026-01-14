# 📝 CHANGELOG.md (한국어로 제공)

이 프로젝트의 주요 변경사항 정리

## [2.0.1] - 2026-01-14

### Added
- 초성 검색 지원: 키워드에 초성을 별도로 등록하지 않아도 `ㄴㅇㅂ` 입력으로 `네이버` 등 한글 항목을 검색 가능.
- 초성 검색 정렬 강화: 초성 검색 시 Title 초성 일치 항목을 우선 노출(prefix/contains 우선순위 기반).

### Changed
- 실행 로직 개선: URL뿐 아니라 로컬 프로그램/파일/폴더 실행 안정화를 위해 Windows ShellExecute(`Process.Start` + `UseShellExecute=true`) 기반으로 실행 경로를 개선.
- 공백 포함 경로 처리 강화: `C:\Program Files\...`처럼 공백이 있는 경로도 따옴표 없이 정상 실행되도록 실제 파일 존재 여부 기반으로 exe/args 파싱 로직으로 변경.
- 환경변수 확장 강화: `"%USERNAME%"`, `"%LOCALAPPDATA%"` 등 환경변수가 포함된 경로를 실행 전에 자동 확장하도록 처리 로직 점검 및 적용.

### Fixed
- `af 카카오톡`, `af VS Code` 등 로컬 프로그램 실행이 동작하지 않던 문제 수정(경로 파싱/공백 처리 원인).
- Import/Export JSON 관련 가독성/호환성 개선(이미 반영된 항목 유지):
- Export 시 한글 유니코드 이스케이프 방지
- UTF-8 (BOM 없음) 저장 유지

## [2.0.0] - 2026-01-14

파이썬(Python)으로 구현하는 것의 한계가 있어서 C#으로 대규모 전환 실시
파이썬으로 구현했던 대부분의 편의 기능이 거의 초기 상태로 돌아감 (예: 컨텍스트 메뉴, 한글 초성 검색 등)

### Added
- Flow Launcher Settings UI에서 키워드 항목을 GUI로 추가/수정/삭제할 수 있도록 구현.
- Settings UI에서 keywords.json을 Import / Export로 빠르게 백업 및 복원 가능.
- 배포 자동화를 위한 GitHub Actions 릴리즈 파이프라인 추가:
- src/plugin.json의 Version 변경 시 자동 dotnet publish → 패키징 → Release 업로드.
- JSON 출력 가독성 개선:
- Export 시 한글이 \uXXXX로 이스케이프되지 않도록 처리.
- UTF-8 (BOM 없음) 으로 저장하여 편집기 호환성 개선.

### Changed
- 플러그인 구현 언어를 Python → C#(.NET) 으로 전환.
- 키워드 저장 형식을 keywords.json 스키마에 맞춰 정합성 개선:
- AliasItem(Keyword/Target/Arguments) 기반에서
- KeywordEntry(title/path/keywords/description) 기반으로 통일.
- Settings UI의 DataGrid 표시/바인딩을 KeywordEntry 기준으로 수정하여
- 빈 칸으로 보이던 Keyword/Target 표시 문제 해결.
- 플러그인 실행/검색 로직을 KeywordEntry 스키마 기준으로 검색 대상 확대:
- title/description/path/keywords 배열 기반 검색.

### Fixed
- Settings UI에서 description만 보이고 다른 컬럼이 비는 문제 수정(모델-JSON 스키마 불일치 원인).
- SettingsPanel/ViewModel/Repository 간 타입 불일치로 인한 컴파일 에러 해결.
- GitHub Actions 패키징 단계에서 필수 리소스(예: icon.png) 누락 시 발생하던 릴리즈 실패 원인 정리(아이콘 경로 정리 포함).

### Build/CI
- dotnet publish 기반 배포 패키지 생성 흐름 확립:
- .dll, .deps.json 등 런타임 산출물 + plugin.json, icon.png, keywords.json 동봉.
- 산출물 폴더(bin/, obj/, publish/, dist/)는 소스 관리에서 제외하도록 .gitignore 정리.

## [1.1.0] - 2026-01-13

### **Added**

- 4단계 아이콘 해결 로직 (4-Tier Icon Resolution): 사용자 지정, 로컬 추출, 웹 파비콘, 기본 아이콘 순으로 최적의 아이콘을 탐색하는 로직 구현

- 컨텍스트 메뉴 (Context Menu): Shift+Enter 또는 우클릭을 통해 URL 복사, 파일 경로 복사, 해당 폴더 열기 기능을 수행할 수 있는 메뉴 추가

- 가중치 기반 검색 정렬 (Scoring System): 입력값과 제목/키워드 간의 일치도를 8단계로 세분화하여 가장 정확한 결과를 상단에 배치

- 환경 변수 자동 치환: %USERNAME%, %APPDATA% 등 윈도우 환경 변수가 포함된 경로를 실제 시스템 경로로 자동 변환하여 실행

- 클립보드 및 탐색기 연동: 외부 라이브러리 없이 PowerShell을 이용한 클립보드 복사와 explorer /select를 이용한 파일 강조 기능 추가

### **Changed**

- 아이콘 경로 통일: plugin.json 및 main.py 내의 모든 기본 아이콘 참조를 icon.png로 단일화하여 일관성 확보

- 메서드 호출 구조 개선: getattr을 활용하여 JSON-RPC 요청을 동적으로 처리하도록 코드 리팩토링

## [1.0.0] - 2026-01-12

### **Added**

- Alias Flow 초기 릴리스: 별칭(Alias) 기능을 통한 프로그램 및 URL 실행 플러그인 기본 기능 구현

- 한글 초성 검색 지원: 유니코드 수식으로 한글 초성 검색 기능 구현 (외부 라이브러리(jamo) 의존성 X)

- GitHub Actions 자동 배포: 자동으로 Flow.Launcher.Plugin.AliasFlow.zip 파일을 생성하여 Releases에 업로드

- 다국어 문서 지원: 영어(readme.md) 및 한국어(readme.ko.md) 매뉴얼 작성

---

참고 : https://keepachangelog.com/en/1.0.0/