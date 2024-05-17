import json
from pathlib import Path

class InputLoaderJson:
    
    def __init__(self, path):
        self._path = Path(path)
       
    def resolve(self, path):
        return self._path.parent.joinpath(path).resolve()

    def load(self):
        try:
            return json.load(open(self._path))
        except:
            return self._load_borked()

    def _load_borked(self):
        return json.loads(
            open(self._path, "rb")
                .read()
                .decode("utf8")[1:]
                .replace("\r\n", "")
                .replace("\t", " ")
        )
