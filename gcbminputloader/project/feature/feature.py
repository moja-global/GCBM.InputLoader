from __future__ import annotations
import pandas as pd
from pathlib import Path

class Feature:

    def create(self, output_connection_string: str):
        raise NotImplementedError()

    def save(self, config: Configuration):
        raise NotImplementedError()

    def _load_data(
        self, path: [str, Path], header:bool = True, allow_nulls=True,
        **kwargs: Any
    ) -> DataFrame:
        path = Path(path)
        if path.suffix.startswith(".xls"):
            data = pd.read_excel(
                path,
                header=0 if self._header else None,
                sheet_name=kwargs.get("sheet_name")
            )
        
        data = pd.read_csv(path, header=0 if header else None)
        if not allow_nulls and pd.isna(data).any().agg("max"):
            raise RuntimeError(f"Error: found null values in {path}")

        return data
