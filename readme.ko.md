[English](./readme.md) | **한국어**

# Alias Flow 🚀

Alias Flow는 사용자가 직접 정의한 별칭(Alias)과 한글 초성 검색을 지원하는 Flow Launcher 전용 플러그인입니다. 복잡한 프로그램 경로와 긴 웹사이트 주소를 단 몇 글자의 키워드로 실행하여 업무 효율을 극대화합니다.

## ✨ 핵심 기능 (Key Features)

|기능|상세 설명|
|------|---|
|**한글 초성 검색**|`네이버`를 찾기 위해 `ㄴㅇㅂ`만 입력해도 매칭되는 지능형 검색을 지원합니다.|
|**다중 별칭 매핑**|하나의 대상에 여러 키워드를 지정할 수 있습니다. (예: `Firefox` → `파폭`, `ff`, `browser`)|
|**Zero-Dependency**|외부 라이브러리 없이 파이썬 기본 환경에서 즉시 실행되는 가벼운 구조입니다.|
|**통합 런처**|로컬 실행 파일(`.exe`)과 `웹 URL`을 하나의 리스트에서 관리합니다.|
|**간편한 백업**|`keywords.json` 파일 하나로 모든 설정을 내보내기 하거나 설정, 동기화할 수 있습니다.|

## 🛠 설치 방법 (Installation)

### 1. 요구 사항

- **[Flow Launcher](https://www.flowlauncher.com/) v1.8 이상**: 이 버전부터는 Python이 **자동으로 설치 및 관리**됩니다.
- *(하위 버전 사용자만 **Python 3.x** 수동 설치가 필요합니다.)*

### 2. 설치 단계
1. 이 저장소의 ZIP 파일을 다운로드하여 압축을 풉니다.

2. 아래 경로에 `AliasFlow` 폴더를 복사하여 넣습니다. (위치: `%AppData%\FlowLauncher\Plugins\AliasFlow`)

3. Flow Launcher를 재시작합니다.

## 🚀 사용법 (Usage)

기본 액션 키워드는 `af`입니다.

**검색 및 실행**: `af` 뒤에 키워드나 초성을 입력합니다.

**예**: `af ㄴㅇㅂ` → 네이버 브라우저 실행

**예**: `af ff` → 파이어폭스 실행

**설정 관리**: `af 설정` 또는 `af config`를 입력하면 데이터 파일이 위치한 폴더가 열립니다.

## ⚙️ 데이터 구성 (Configuration)

`keywords.json` 파일을 수정하여 나만의 실행 리스트를 커스터마이징할 수 있습니다.

```json
[
  {
    "title": "네이버",
    "description": "Naver 포털 및 메일 확인",
    "path": "https://www.naver.com",
    "keywords": ["네이버", "naver"]
  },
  {
    "title": "파이어폭스",
    "description": "Firefox 브라우저 실행",
    "path": "C:\\Program Files\\Mozilla Firefox\\firefox.exe",
    "keywords": ["파이어폭스", "파폭", "브라우저", "firefox", "ff"]
  },
  {
    "title": "설정 폴더 열기",
    "description": "Alias Flow 설정 및 백업 (keywords.json)",
    "path": "open_config_folder",
    "keywords": ["설정", "백업", "config", "backup"]
  }
]
```

- 주의: 로컬 경로 입력 시 백슬래시(`\`)는 반드시 두 번(`\\`) 입력해야 합니다.


## 📂 파일 구조 (File Structure)

```plain
AliasFlow/
├── presets/             # 국가별 키워드 프리셋
│   ├── ko-KR.json
│   ├── en-US.json
│   ├── ja-JP.json
│   └── zh-CN.json
├── plugin.json          # 플러그인 메타데이터
├── main.py              # 초성 검색 및 실행 로직
├── keywords.json        # 키워드 기본값 (싸용자가 수정하여 사용)
└── icon.png             # 플러그인 아이콘
```

---

📄 라이선스 (License)
이 프로젝트는 **MIT License**를 따릅니다.

👨‍💻 **제작자**: [GOODJINC](https://goodjinc.com)
