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
        """라이브러리 없이 한글 유니코드 수식으로 초성을 추출합니다."""
        CHOSUNG_LIST = ['ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ']
        result = []
        for char in text:
            code = ord(char)
            # 한글 음절 범위(가 ~ 힣)인지 확인
            if 0xAC00 <= code <= 0xD7A3:
                chosung_index = (code - 0xAC00) // 588
                result.append(CHOSUNG_LIST[chosung_index])
            else:
                result.append(char.lower())
        return "".join(result)

    def query(self, query_str):
        results = []
        query_str = query_str.lower().strip()

        for item in self.keywords_data:
            title = item.get("title", "")
            description = item.get("description", "")
            path = item.get("path", "")
            keywords = item.get("keywords", [])
            
            # 1. 일반 검색 (제목 또는 키워드에 포함)
            is_match = any(query_str in k.lower() for k in keywords) or query_str in title.lower()
            
            # 2. 초성 검색 (제목의 초성과 입력값 비교)
            title_chosung = self.get_chosung(title)
            if query_str in title_chosung:
                is_match = True

            if is_match or not query_str:
                results.append({
                    "Title": title,
                    "SubTitle": f"{description} (Keywords: {', '.join(keywords)})",
                    "IcoPath": "Images\\app.jpg", # plugin.json의 IcoPath와 일치
                    "JsonRPCAction": {
                        "method": "execute_action",
                        "parameters": [path],
                        "dontHideAfterAction": False
                    }
                })
        
        if not results and query_str:
            results.append({
                "Title": "검색 결과가 없습니다.",
                "SubTitle": "설정 폴더를 열어 keywords.json을 확인하세요.",
                "IcoPath": "Images\\app.jpg",
                "JsonRPCAction": {"method": "open_config_folder", "parameters": []}
            })
        return results

    def execute_action(self, path):
        if path == "open_config_folder":
            os.startfile(os.path.dirname(self.config_path))
        elif path.startswith("http"):
            webbrowser.open(path)
        else:
            if os.path.exists(path):
                os.startfile(path)
            else:
                subprocess.run(['msg', '*', f"Path not found: {path}"], shell=True)

if __name__ == "__main__":
    # Flow Launcher JSON-RPC 통신
    try:
        launcher = AliasFlow()
        if len(sys.argv) > 1:
            request = json.loads(sys.argv[1])
            method = request.get("method")
            params = request.get("parameters", [])

            if method == "query":
                print(json.dumps({"result": launcher.query(params[0])}))
            elif method in ["execute_action", "open_config_folder"]:
                launcher.execute_action(params[0] if params else "open_config_folder")
    except Exception as e:
        with open("error.log", "w", encoding="utf-8") as f:
            f.write(str(e))