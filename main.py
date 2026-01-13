import sys
import json
import os
import webbrowser
import subprocess

class AliasFlow:
    def __init__(self):
        # 데이터 파일 로드
        self.config_path = os.path.join(os.path.dirname(__file__), "keywords.json")
        try:
            with open(self.config_path, "r", encoding="utf-8") as f:
                self.keywords_data = json.load(f)
        except Exception:
            self.keywords_data = []

    def get_chosung(self, text):
        """한글 유니코드 수식으로 초성을 추출합니다."""
        CHOSUNG_LIST = ['ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ']
        result = []
        for char in text:
            code = ord(char)
            if 0xAC00 <= code <= 0xD7A3:
                chosung_index = (code - 0xAC00) // 588
                result.append(CHOSUNG_LIST[chosung_index])
            else:
                result.append(char.lower())
        return "".join(result)

    def resolve_icon(self, item, expanded_path):
        """제안된 4단계 아이콘 해결 로직을 수행합니다."""
        # Tier 4: 기본 보루 (Fallback)
        default_icon = "icon.png" 

        # Tier 1: 사용자가 keywords.json에 직접 지정한 아이콘
        manual_icon = item.get("icon")
        if manual_icon:
            # 상대 경로일 경우 절대 경로로 변환
            manual_icon_path = os.path.join(os.path.dirname(__file__), manual_icon) if not os.path.isabs(manual_icon) else manual_icon
            if os.path.exists(manual_icon_path):
                return manual_icon

        # Tier 2: 로컬 파일(.exe 등)인 경우 시스템 추출 아이콘 사용
        if not expanded_path.startswith("http") and os.path.exists(expanded_path):
            return expanded_path

        # Tier 3: 웹사이트인 경우 구글 파비콘 API (인터넷 연결 필요)
        if expanded_path.startswith("http"):
            try:
                domain = expanded_path.split("//")[-1].split("/")[0]
                return f"https://www.google.com/s2/favicons?domain={domain}&sz=64"
            except Exception:
                return default_icon

        return default_icon

    def query(self, query_str):
        scored_results = []
        query_str = query_str.lower().strip()

        for item in self.keywords_data:
            title = item.get("title", "")
            description = item.get("description", "")
            path = item.get("path", "")
            expanded_path = os.path.expandvars(path) # 환경 변수 처리
            keywords = [k.lower() for k in item.get("keywords", [])]
            
            score = 0
            title_lower = title.lower()
            title_chosung = self.get_chosung(title)

            # 가중치 점수 계산 로직
            if not query_str: score = 1
            elif query_str == title_lower: score = 100
            elif query_str in keywords: score = 90
            elif title_lower.startswith(query_str): score = 80
            elif query_str == title_chosung: score = 70
            elif any(k.startswith(query_str) for k in keywords): score = 60
            elif query_str in title_lower: score = 50
            elif any(query_str in k for k in keywords): score = 40
            elif query_str in title_chosung: score = 30

            if score > 0:
                # 4단계 아이콘 로직 호출
                ico_path = self.resolve_icon(item, expanded_path)

                scored_results.append(({
                    "Title": title,
                    "SubTitle": f"{description}",
                    "IcoPath": ico_path,
                    "ContextData": path,
                    "JsonRPCAction": {
                        "method": "execute_action",
                        "parameters": [path],
                        "dontHideAfterAction": False
                    }
                }, score))
        
        scored_results.sort(key=lambda x: x[1], reverse=True)
        return [x[0] for x in scored_results]

    def context_menu(self, path):
        """우클릭/Shift+Enter 동작 정의"""
        results = []
        expanded_path = os.path.expandvars(path)

        if path.startswith("http"):
            results.append({
                "Title": "Copy URL",
                "SubTitle": f"복사: {path}",
                "IcoPath": "icon.png",
                "JsonRPCAction": {"method": "copy_to_clipboard", "parameters": [path]}
            })
        else:
            results.append({
                "Title": "Open Containing Folder",
                "SubTitle": "해당 폴더를 열고 파일을 선택합니다.",
                "IcoPath": "icon.png",
                "JsonRPCAction": {"method": "open_folder", "parameters": [expanded_path]}
            })
            results.append({
                "Title": "Copy Full Path",
                "SubTitle": f"복사: {expanded_path}",
                "IcoPath": "icon.png",
                "JsonRPCAction": {"method": "copy_to_clipboard", "parameters": [expanded_path]}
            })
        return results

    def execute_action(self, path):
        if path == "open_config_folder":
            os.startfile(os.path.dirname(self.config_path))
            return

        expanded_path = os.path.expandvars(path)
        if path.startswith("http"):
            webbrowser.open(expanded_path)
        else:
            if os.path.exists(expanded_path):
                os.startfile(expanded_path)
            else:
                subprocess.run(['msg', '*', f"Path not found: {expanded_path}"], shell=True)

    def copy_to_clipboard(self, text):
        """PowerShell을 이용한 클립보드 복사"""
        command = f'[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms"); [System.Windows.Forms.Clipboard]::SetText("{text}")'
        subprocess.run(['powershell', '-command', command], check=True)

    def open_folder(self, path):
        """폴더 열기 및 파일 선택"""
        if os.path.exists(path):
            subprocess.run(['explorer', '/select,', os.path.normpath(path)])

if __name__ == "__main__":
    try:
        launcher = AliasFlow()
        if len(sys.argv) > 1:
            request = json.loads(sys.argv[1])
            method = request.get("method")
            params = request.get("parameters", [])

            if method == "query":
                print(json.dumps({"result": launcher.query(params[0])}))
            elif method == "context_menu":
                print(json.dumps({"result": launcher.context_menu(params[0])}))
            else:
                func = getattr(launcher, method, None)
                if func: func(*params)
    except Exception as e:
        with open("error.log", "w", encoding="utf-8") as f:
            f.write(str(e))