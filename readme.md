**English** | [한국어](./readme.ko.md)

# Alias Flow 🚀

**Alias Flow** is a Flow Launcher plugin that allows you to quickly launch **websites, local applications, and global hotkeys** using custom keywords.

It supports **Korean initial consonant (Chosung) search** and a full GUI-based configuration.

## ✨ Key Features

### 🔍 Smart Search
- **Korean Chosung search**
  - `네이버` → `ㄴㅇㅂ`
  - No need to register initials manually
- **Title-priority sorting**
  - Exact and initial matches appear first

### 🚀 Execution Types
- Launch **Web URLs**
- Launch **Local applications (.exe)**
- Trigger **Global keyboard shortcuts**
  - e.g. `Ctrl + Shift + Space` (1Password)

### ⚙️ Configuration
- Full **GUI-based add / edit / delete**
- **JSON Import / Export**
  - UTF-8 (no BOM)
  - Fully compatible with Windows Notepad
- Environment variables supported  
  (`%USERNAME%`, `%APPDATA%`, etc.)

## 🛠 Installation

### Requirements
- Latest **Flow Launcher**
- Windows 10 / 11

### Steps
1. Download the latest ZIP from GitHub Releases
2. Extract to: 
```
%AppData%\FlowLauncher\Plugins\AliasFlow
```

3. Restart Flow Launcher

## 🚀 Usage

Default action keyword: **`af`**

### Examples

```
af naver
af ㄴㅇㅂ
af kakao
```


### Execution

|Input|Action|
|---|---|
|`af naver`|Open Naver website|
|`af kakao`|Launch KakaoTalk|
|`af 1password`|Trigger global hotkey|

---

## ⌨️ Hotkey Example

```json
{
  "title": "1Password",
  "description": "Quick access",
  "hotkey": "Ctrl+Shift+Space",
  "keywords": ["1password"]
}
```

- If `hotkey` is set, Alias Flow sends the key combination instead of launching a path

- Admin-level apps may require Flow Launcher to run as administrator

## 📂 keywords.json Structure

| Field       | Description                |
| ----------- | -------------------------- |
| title       | Display name               |
| description | Description                |
| path        | URL or executable path     |
| keywords    | Search keywords            |
| hotkey      | Global shortcut (optional) |

## 📦 Import / Export

- Import / Export settings via GUI
- UTF-8 (no BOM)
- Ideal for backup and sync

## 📦 Preset Guide

Alias Flow provides optional **regional preset JSON files** in addition to the default configuration.  
Presets are intended as **starting points**, not final configurations.

### Available Presets
- **Default (English)** – Base configuration
- **Korea (KR)** – Naver, KakaoTalk, etc.
- **China (CN)** – Baidu, WeChat, etc.
- **Japan (JP)** – Yahoo Japan, LINE, etc.

Preset files are organized as follows:

```
presets/
├─ default.en.json
├─ korea.ko.json
├─ china.zh.json
└─ japan.ja.json
```


### 📥 Importing a Preset

1. Open Flow Launcher
2. Go to **Settings → Plugins → Alias Flow**
3. Click **Import JSON**
4. Select a preset JSON file

Imported presets are **merged into your existing configuration**.  
You can freely remove or edit any entry afterward.

---

### ✏️ Customization Notes

- Presets include only **minimal default values**
- Modify keywords, paths, and hotkeys to fit your environment
- Korean initial (chosung) search works automatically — no need to add initials manually

> Presets are designed to help you get started quickly.



## 📄 라이선스

MIT License

## 👨‍💻 제작자

[GOODJINC](https://goodjinc.com)