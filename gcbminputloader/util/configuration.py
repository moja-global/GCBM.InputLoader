from __future__ import annotations
import json
import logging
from os.path import relpath
from pathlib import Path
from gcbminputloader.util.json import InputLoaderJson

class Configuration(dict):

    def __init__(self, d: dict, config_path: [str, Path], working_path: [str, Path] = None):
        super().__init__(d)
        self.config_path = Path(config_path).absolute()
        self.working_path = Path(working_path or config_path).absolute()

    def resolve(self, path: [str, Path] = None) -> Path:
        return self.config_path.joinpath(path).resolve()
    
    def resolve_relative(self, path: [str, Path]) -> Path:
        return relpath(self.resolve(path), self.config_path)

    def resolve_working(self, path: [str, Path] = None) -> Path:
        return self.working_path.joinpath(path).resolve()
    
    def resolve_working_relative(self, path: [str, Path]) -> Path:
        return relpath(self.resolve_working(path), self.working_path)
    
    def get(self, key: str, default: Any = None) -> Any:
        v = super().get(key, default)
        if isinstance(v, dict):
            return self.__class__(v, self.config_path, self.working_path)
        
        return v
    
    def items(self) -> Iterable[tuple[str, Any]]:
        for k, v in super().items():
            if isinstance(v, dict):
                yield k, self.__class__(v, self.config_path, self.working_path)
            else:
                yield k, v

    def save(self, filename: [str, Path]):
        output_path = self.working_path.joinpath(filename)
        json_content = json.dumps(self, indent=4, ensure_ascii=False)
        with open(output_path, "w", encoding="utf8", errors="surrogateescape") as out_file:
            out_file.write(json_content)

    @classmethod
    def load(cls, config_path: [str, Path], working_path: [str, Path] = None) -> Configuration:
        config_path = Path(config_path).absolute()
        logging.debug(f"Loading configuration: {config_path}")

        return cls(
            InputLoaderJson(config_path).load(),
            config_path.parent,
            Path(working_path or config_path.parent)
        )
