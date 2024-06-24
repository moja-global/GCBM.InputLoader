from __future__ import annotations
import pandas as pd
from pathlib import Path

class Feature:

    def create(self, output_connection_string: str):
        raise NotImplementedError()

    def save(self, config: Configuration):
        raise NotImplementedError()

    def _load_data(self, path: [str, Path], header:bool = True, **kwargs: Any) -> DataFrame:
        path = Path(path)
        if path.suffix.startswith(".xls"):
            return pd.read_excel(
                path,
                header=0 if self._header else None,
                sheet_name=kwargs.get("sheet_name")
            )
        
        return pd.read_csv(path, header=0 if header else None)
