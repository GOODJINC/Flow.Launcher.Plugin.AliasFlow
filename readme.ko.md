[English](./readme.md) | **한국어**

# Alias Flow 🚀

**Alias Flow**는 Flow Launcher 전용 플러그인으로, 사용자가 정의한 키워드를 통해 **웹사이트, 로컬 프로그램, 전역 단축키**를 빠르게 실행할 수 있도록 도와줍니다.

한글 초성 검색(예: `ㄴㅇㅂ`)과 GUI 기반 설정을 지원하여 복잡한 경로와 단축키를 기억하지 않아도 됩니다.

## ✨ 주요 기능

### 🔍 지능형 검색
- **한글 초성 검색 지원**
  - `네이버` → `ㄴㅇㅂ`
  - 키워드에 초성을 직접 등록하지 않아도 자동 인식
- **Title 우선 정렬**
  - 초성/완전 일치 항목이 항상 상단에 표시됨

### 🚀 실행 방식
- **웹 URL 실행**
- **로컬 프로그램 실행 (`.exe`)**
- **전역 키보드 단축키 실행**
  - 예: `Ctrl + Shift + Space` (1Password 등)

### ⚙️ 설정 및 관리
- Flow Launcher **설정 화면에서 GUI로 추가 / 수정 / 삭제**
- **JSON Import / Export 지원**
  - UTF-8 (BOM 없음)
  - Windows 메모장 완전 호환
- `%USERNAME%`, `%APPDATA%` 등 환경 변수 자동 인식

## 🛠 설치 방법

### 요구 사항
- **Flow Launcher 최신 버전**
- Windows 10 / 11

### 설치
1. GitHub Releases에서 최신 버전 ZIP 다운로드

2. 아래 경로에 압축 해제
```
%AppData%\FlowLauncher\Plugins\AliasFlow
```

3. Flow Launcher 재시작

## 🚀 사용 방법

기본 액션 키워드: **`af`**

### 🔎 검색

```
af 네이버
af ㄴㅇㅂ
af 카카오톡
```

### ▶ 실행 예시

|입력|동작|
|---|---|
|`af 네이버`|네이버 웹 실행|
|`af 카카오톡`|카카오톡 프로그램 실행|
|`af 1password`|등록된 글로벌 단축키 실행|

---

## ⌨️ Hotkey 사용 방법

### keywords.json 예시
```json
{
  "title": "1Password",
  "description": "1Password Quick Access",
  "path": "",
  "hotkey": "Ctrl+Shift+Space",
  "keywords": ["1password", "비밀번호"]
}
```

- hotkey가 설정되면 프로그램 실행 대신 단축키를 전송

- 관리자 권한 앱의 경우 Flow Launcher도 관리자 권한 필요할 수 있음

## 📂 데이터 구조 (keywords.json)

```json
{
  "title": "카카오톡",
  "description": "카카오톡 메신저",
  "path": "C:\\Program Files\\Kakao\\KakaoTalk\\KakaoTalk.exe",
  "keywords": ["카카오톡", "카톡"],
  "hotkey": ""
}
```

|필드|설명|
|---|---|
|title|표시 이름|
|description|설명|
|path|웹 URL 또는 실행 경로|
|keywords|검색 키워드|
|hotkey|전역 단축키 (선택)|

## 📦 Import / Export

- 설정 화면에서 JSON 파일 내보내기 / 가져오기
- UTF-8 (BOM 없음)
- Git, 클라우드 백업에 최적화

## 📦 프리셋(Presets) 가이드

Alias Flow는 기본 설정 외에도 **국가/언어별 프리셋 JSON**을 제공합니다.  
프리셋은 기본 예시일 뿐이며, **사용자가 자유롭게 수정하여 사용**하는 것을 전제로 합니다.

### 제공 프리셋
- **Default (English)** – 기본 설치용
- **Korea (KR)** – 네이버, 카카오톡 등 한국 서비스
- **China (CN)** – Baidu, WeChat 등 중국 서비스
- **Japan (JP)** – Yahoo Japan, LINE 등 일본 서비스

프리셋 파일은 다음과 같은 구조로 제공됩니다.

```
presets/
├─ default.en.json
├─ korea.ko.json
├─ china.zh.json
└─ japan.ja.json
```

### 📥 프리셋 가져오기 (Import)

1. Flow Launcher 실행
2. **Settings → Plugins → Alias Flow**
3. **Import JSON** 버튼 클릭
4. 원하는 프리셋 JSON 파일 선택

가져온 프리셋은 기존 설정에 **추가**되며, 필요 없는 항목은 언제든지 삭제할 수 있습니다.

---

### ✏️ 커스터마이징

- 프리셋은 **최소한의 기본값**만 포함합니다.
- 키워드, 경로, 단축키는 사용자 환경에 맞게 수정하세요.
- 한글 초성 검색은 자동 지원되므로 초성을 직접 등록할 필요가 없습니다.

> 프리셋은 “완성본”이 아니라 **빠른 시작을 위한 샘플**입니다.

## 📄 라이선스

MIT License

## 👨‍💻 제작자

[GOODJINC](https://goodjinc.com)