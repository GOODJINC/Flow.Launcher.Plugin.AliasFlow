**English** | [한국어](./readme.ko.md)

# Alias Flow 🚀

Alias Flow is a dedicated plugin for Flow Launcher that supports user-defined aliases and Korean Initial Consonant (Chosung) search. It maximizes workflow efficiency by allowing you to execute complex program paths and long website URLs with just a few keystrokes.

## ✨Key Features

|Feature|Description|
|------|---|
|**Korean Chosung Search**|Supports intelligent search where you can find `네이버` by simply typing its initials `ㄴㅇㅂ`.|
|**Multi-Alias Mapping**|Map multiple keywords to a single target. (e.g., `Firefox` → `파폭`, `ff`, `browser`)|
|**Zero-Dependency**|Lightweight architecture that runs instantly in a default Python environment without external libraries.|
|**Integrated Launcher**|Manage both local `executable files (.exe)` and `Web URLs` in a single unified list.|
|**Easy Backup**|Export, configure, or sync all settings using a single `keywords.json` file.|

## 🛠 Installation

### 1. Requirements

- **Flow Launcher v1.8 or higher**: Python environment is **automatically handled** by [Flow Launcher](https://www.flowlauncher.com/).
- *(For older versions: Manual installation of **Python 3.x** is required.)*

### 2. Steps

1. Download the ZIP file from this repository and extract it.

2. Copy the `AliasFlow` folder into the following directory: `%AppData%\FlowLauncher\Plugins\AliasFlow`

3. Restart Flow Launcher.


## 🚀 Usage

The default action keyword is `af`.

**Search & Execute**: Type `af` followed by a keyword or Korean initials.

**Example**: `af ㄴㅇㅂ` → Launches Naver in your browser.

**Example**: `af ff` → Launches Firefox.

**Settings Management**: Type `af 설정` or `af config` to open the folder containing your data files.

## ⚙️ Configuration

You can customize your execution list by editing the `keywords.json` file.

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

- Note: When entering local paths, you must use double backslashes (**\\**).


## 📂 File Structure

```plain
AliasFlow/
├── presets/             # Keyword Presets by Country
│   ├── ko-KR.json
│   ├── en-US.json
│   ├── ja-JP.json
│   └── zh-CN.json
├── plugin.json          # Plugin metadata
├── main.py              # Chosung search and execution logic
├── keywords.json        # Default Keywords (User Editable)
└── icon.png             # Plugin icon
```

---

📄 This project is licensed under the **MIT License**.

👨‍💻 **Author**: [GOODJINC](https://goodjinc.com)
