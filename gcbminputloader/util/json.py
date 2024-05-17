import json
import logging
from pathlib import Path

class InputLoaderJson:
    
    def __init__(self, path):
        self._path = Path(path)
       
    def resolve(self, path):
        return self._path.parent.joinpath(path).resolve()

    def load(self):
        try:
            logging.debug(f"Loading JSON file: {self._path.absolute()}")
            return json.load(open(self._path))
        except:
            logging.debug("  retrying with corrections")
            return self._load_borked()

    def _load_borked(self):
        return json.loads(
            open(self._path, "rb")
                .read()
                .decode("utf8")[1:]
                .replace("\r\n", "")
                .replace("\t", " ")
        )
