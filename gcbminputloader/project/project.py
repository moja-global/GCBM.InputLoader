from __future__ import annotations
import json
import logging
import gcbminputloader
from pathlib import Path
from enum import Enum
from sqlalchemy import MetaData
from sqlalchemy import text
from sqlalchemy import insert
from gcbminputloader.util.json import InputLoaderJson
from gcbminputloader.util.db import get_connection
from gcbminputloader.util.configuration import Configuration

class ProjectType(Enum):

    GcbmClassicSpatial = "cbm_classic_spatial"
    LegacyGcbmClassicSpatial = "legacy_cbm_classic_spatial"
    LegacyGcbmClassicSpatialNoGrowthCurves = "legacy_cbm_classic_spatial_no_gc_tables"
    
class ClassifierMapping(dict):
    
    def __init__(self, classifiers: list[str]):
        super().__init__({c: None for c in classifiers})
    
    def map_classifier(self, classifier: str, col: int):
        if classifier not in self:
            raise RuntimeError(f"Unknown classifier: {classifier}")
        
        self[classifier] = col

class Project:
    
    def __init__(self, project_type: ProjectType, aidb_path: [str, Path], classifiers: list[str]):
        self._project_type = project_type
        self._aidb_path = aidb_path
        self._classifiers = classifiers
        self._features = []

    def create(self, output_connection_string: str):
        logging.debug(f"Loading {output_connection_string} using {self._aidb_path}")
        Path(output_connection_string).unlink(True)

        with get_connection(output_connection_string, optimize=True) as output_db:
            logging.info("Loading default parameters...")
            project_loader_config = self._read_loader_config(f"{self._project_type.value}.json")
            for loader_config_path in project_loader_config:
                if not loader_config_path.endswith(".json"):
                    continue
                
                self._process_loader(loader_config_path, output_db)
                
        for feature in self._features:
            feature.create(output_connection_string)
    
    def save(self, config_path: [str, Path]):
        config_path = Path(config_path)

        logging.info(f"Writing configuration to {config_path}...")
        logging.info("  project configuration")
        config = Configuration({
            "project_type": self._project_type.name,
            "classifiers": self._classifiers
        }, config_path.parent)
        
        config["aidb"] = config.resolve_working_relative(self._aidb_path)
        
        feature_config = config.get("features", {})
        for feature in self._features:
            feature.save(feature_config)
        
        config["features"] = feature_config
        config.save(config_path)

    def create_classifier_mapping(self, mappings: dict[str, int] = None) -> ClassifierMapping:
        classifier_mapping = ClassifierMapping(self._classifiers)
        for classifier, col in (mappings or {}).items():
            classifier_mapping.map_classifier(classifier, col)
        
        return classifier_mapping

    def add_feature(self, feature: Feature):
        self._features.append(feature)

    def _process_loader(self, loader_config_path: [str, Path], output_db: Connection):
        loader_config_path = Path(loader_config_path)
        loader_type, loader_config = next(iter(self._read_loader_config(loader_config_path).items()))
        loader_name = loader_config.get("name", loader_config_path.name)
        logging.info(f"  {loader_name}")
        if loader_type == "InternalLoaderMapping":
            queries = self._parse_sql(
                loader_config.get("sql")
                or open(self._get_resource_path(
                    loader_config["sql_file"],
                    output_db.engine.dialect.name
                )).read()
            )
            
            for (query, _) in queries:
                output_db.execute(query)
        elif loader_type == "SQLLoaderMapping":
            fetch_query, _ = self._parse_sql(loader_config["fetch_sql"])[0]
            load_query, load_params = self._parse_sql(loader_config["load_sql"])[0]
            with get_connection(str(self._aidb_path)) as aidb:
                aidb_data = aidb.execute(fetch_query)
                cols = aidb_data.keys()
                for row in aidb_data:
                    row_data = dict(zip(cols, row))
                    output_db.execute(load_query.bindparams(**{
                        k: v for k, v in row_data.items() if k in load_params
                    }))
        elif loader_type == "StaticLoaderMapping":
            table_name = loader_config["table"]
            md = MetaData()
            md.reflect(bind=output_db, only=[table_name])
            table = next(iter(md.tables.values()))
            data_cols = loader_config["fields"]
            output_db.execute(
                insert(table),
                [dict(zip(data_cols, row)) for row in loader_config["data"]]
            )
                
    def _read_loader_config(self, rel_config_path: Path) -> InputLoaderJson:
        loader_config_path = self._get_resource_path(rel_config_path)
        loader_config = InputLoaderJson(loader_config_path).load()

        return loader_config

    def _parse_sql(self, raw_sql: str) -> list[tuple[str, Iterable[str]]]:
        queries = []
        for sql in raw_sql.replace("@", ":").split(";"):
            if not sql or sql.isspace():
                continue
            
            query = text(sql)
            query_params = set(query.compile().bind_names.values())
            queries.append((query, query_params))

        return queries
    
    def _get_resource_path(self, filename: [str, Path], dialect: str = None) -> Path:
        resource_root = Path(gcbminputloader.__file__).parent.joinpath("resources", "Loader")
        resource_path = resource_root.joinpath(filename)
        if dialect:
            dialect_suffix = f"{resource_path.suffix}.{dialect}"
            dialect_resource_path = resource_path.with_suffix(dialect_suffix)
            if dialect_resource_path.exists():
                return dialect_resource_path

        return resource_path
