from __future__ import annotations
import json
import logging
from pathlib import Path

class InputLoaderJson:
    
    def __init__(self, path: [str, Path]):
        self._path = Path(path)
       
    def resolve(self, path: [str, Path]) -> Path:
        return self._path.parent.joinpath(path).resolve()

    def load(self) -> dict:
        try:
            logging.debug(f"Loading JSON file: {self._path.absolute()}")
            return json.load(open(self._path))
        except:
            logging.debug("  retrying with corrections")
            return self._load_borked()

    def _load_borked(self):
        return json.loads(
            open(self._path, encoding="utf-8-sig")
                .read()
                .replace("\n", "")
                .replace("\t", " ")
        )
