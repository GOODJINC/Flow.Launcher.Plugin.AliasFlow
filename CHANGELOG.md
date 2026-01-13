# 📝 CHANGELOG.md

이 프로젝트의 주요 변경사항 정리

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