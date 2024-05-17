import logging
from pathlib import Path
from gcbminputloader.util.json import InputLoaderJson

class Configuration(dict):

    def __init__(self, d, config_path, working_path=None):
        super().__init__(d)
        self.config_path = Path(config_path).absolute()
        self.working_path = Path(working_path or config_path).absolute()

    def resolve(self, path=None):
        return self.config_path.joinpath(path).resolve()

    def resolve_working(self, path=None):
        return self.working_path.joinpath(path).resolve()
    
    def get(self, key, default=None):
        v = super().get(key, default)
        if isinstance(v, dict):
            return self.__class__(v, self.config_path, self.working_path)
        
        return v

    @classmethod
    def load(cls, config_path, working_path=None):
        config_path = Path(config_path).absolute()
        logging.debug(f"Loading configuration: {config_path}")

        return cls(
            InputLoaderJson(config_path).load(),
            config_path.parent,
            Path(working_path or config_path.parent)
        )
