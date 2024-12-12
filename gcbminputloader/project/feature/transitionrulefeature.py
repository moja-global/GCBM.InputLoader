from __future__ import annotations
import logging
import numpy as np
from numbers import Number
from collections import defaultdict
from itertools import chain
from sqlalchemy import text
from gcbminputloader.project.feature.feature import Feature
from gcbminputloader.util.db import get_connection

class TransitionRuleFeature(Feature):
    
    def __init__(
        self, path: [str, Path], id_col: int, regen_delay_col: int, reset_age_col: int,
        transition_classifier_mapping: dict[str, int], worksheet: int = None, header: bool = True,
        reset_age_type_col: int = None, disturbance_type_col: int = None,
        match_classifier_mapping: dict[str, int] = None
    ):
        self._path = path
        self._id_col = id_col
        self._regen_delay_col = regen_delay_col
        self._reset_age_col = reset_age_col
        self._transition_classifier_mapping = transition_classifier_mapping
        self._worksheet = worksheet
        self._header = header if header is not None else True
        self._reset_age_type_col = reset_age_type_col
        self._disturbance_type_col = disturbance_type_col
        self._match_classifier_mapping = match_classifier_mapping

    def create(self, output_connection_string: str):
        logging.info("Loading transition rules...")
        with get_connection(output_connection_string, optimize=True) as output_db:
            transition_rule_data = self._load_data(
                self._path, self._header, sheet_name=self._worksheet
            )
            
            self._load_classifier_values(output_db, transition_rule_data)
            
            transition_id_lookup = self._load_transitions(output_db, transition_rule_data)
            classifier_value_lookup = self._get_classifier_value_lookup(output_db)
            
            self._load_transition_classifier_values(
                output_db, transition_id_lookup, classifier_value_lookup, transition_rule_data
            )
            
            self._load_soft_transitions(
                output_db, transition_id_lookup, classifier_value_lookup, transition_rule_data
            )

    def save(self, config: Configuration):
        logging.info("  transition rules")
        config["transition_rules"] = {
            "path": config.resolve_working_relative(self._path),
            "id_col": self._id_col,
            "reset_age_col": self._reset_age_col,
            "regen_delay_col": self._regen_delay_col,
            "classifier_transition_cols": self._transition_classifier_mapping
        }
        
        if self._worksheet is not None:
            config["transition_rules"]["worksheet"] = self._worksheet
            
        if not self._header:
            config["transition_rules"]["header"] = False
            
        if self._reset_age_type_col is not None:
            config["transition_rules"]["reset_age_type_col"] = self._reset_age_type_col
            
        if self._disturbance_type_col is not None:
            config["transition_rules"]["disturbance_type_col"] = self._disturbance_type_col
            
        if self._match_classifier_mapping and any(self._match_classifier_mapping.values()):
            config["transition_rules"]["classifier_matching_cols"] = self._match_classifier_mapping

    def _load_classifier_values(self, conn: Connection, transition_rule_data: DataFrame):
        logging.info("  classifier values")
        unique_classifier_values = defaultdict(list)
        for classifier_name, classifier_col in chain(
            self._transition_classifier_mapping.items(),
            (self._match_classifier_mapping or {}).items()
        ):
            if classifier_col is None:
                continue

            unique_classifier_values[classifier_name].extend(
                transition_rule_data[transition_rule_data.columns[classifier_col]].unique()
            )
        
        conn.execute(
            text("INSERT INTO classifier (name) VALUES (:name) ON CONFLICT DO NOTHING"),
            [{"name": classifier} for classifier in unique_classifier_values.keys()]
        )
            
        classifier_ids = {
            row.name: row.id
            for row in conn.execute(text("SELECT id, name FROM classifier"))
        }
            
        for classifier_name, classifier_values in unique_classifier_values.items():
            conn.execute(text(
                """
                INSERT INTO classifier_value (classifier_id, value, description)
                VALUES (:classifier_id, :value, :value)
                ON CONFLICT DO NOTHING
                """
            ), [
                {"classifier_id": classifier_ids[classifier_name], "value": str(value)}
                for value in chain(classifier_values, ["?"])
                if not (isinstance(value, Number) and np.isnan(value))
            ])
        
    def _load_transitions(self, conn: Connection, transition_rule_data: DataFrame) -> dict[str, int]:
        logging.info("  base transitions")
        transition_id_lookup = {}
        transition_type_lookup = self._get_transition_type_lookup(conn)
        for _, transition_data_row in transition_rule_data.iterrows():
            transition_name = transition_data_row.iloc[self._id_col]
            transition_type_name = (
                transition_data_row.iloc[self._reset_age_type_col] if self._reset_age_type_col
                else "absolute"
            )

            conn.execute(text(
                """
                INSERT INTO transition (description, transition_type_id, age, regen_delay)
                VALUES (:name, :transition_type_id, :reset_age, :regen_delay)
                """
            ), {"name": transition_name,
                "transition_type_id": transition_type_lookup[transition_type_name],
                "reset_age": transition_data_row.iloc[self._reset_age_col],
                "regen_delay": transition_data_row.iloc[self._regen_delay_col]})
            
            transition_id = conn.execute(
                text("SELECT id FROM transition WHERE description = :name"),
                {"name": transition_name}
            ).scalar()

            transition_id_lookup[transition_name] = transition_id
            
        return transition_id_lookup

    def _load_transition_classifier_values(
        self, conn: Connection, transition_id_lookup: dict[str, int],
        classifier_value_lookup: dict[str, dict[str, int]], transition_rule_data: DataFrame
    ):
        logging.info("  transition classifier values")
        for _, transition_data_row in transition_rule_data.iterrows():
            transition_id = transition_id_lookup[transition_data_row.iloc[self._id_col]]
            conn.execute(text(
                """
                INSERT INTO transition_classifier_value (transition_id, classifier_value_id)
                VALUES (:transition_id, :classifier_value_id)
                """
            ), [{
                "transition_id": transition_id,
                "classifier_value_id":
                    classifier_value_lookup[classifier][transition_data_row.iloc[classifier_col]]
            } for classifier, classifier_col in self._transition_classifier_mapping.items()])

    def _load_soft_transitions(
        self, conn: Connection, transition_id_lookup: dict[str, int],
        classifier_value_lookup: dict[str, dict[str, int]], transition_rule_data: DataFrame
    ):
        logging.info("  soft transition rules")
        if self._disturbance_type_col is None:
            logging.info("    not configured")
            return
        
        disturbance_type_lookup = self._get_disturbance_type_lookup(conn)
        classifier_value_lookup = self._get_classifier_value_lookup(conn)
        for _, transition_data_row in transition_rule_data.iterrows():
            disturbance_type = transition_data_row.iloc[self._disturbance_type_col]
            if (not disturbance_type
                or (not isinstance(disturbance_type, str) and np.isnan(disturbance_type))
                or (isinstance(disturbance_type, str) and disturbance_type.isspace())
            ):
                continue
            
            transition_id = transition_id_lookup[transition_data_row.iloc[self._id_col]]
            disturbance_type_id = disturbance_type_lookup.get(disturbance_type.lower())
            if disturbance_type_id is None:
                logging.info(f"    warning: added new disturbance type '{disturbance_type}'")
                conn.execute(text(
                    """
                    INSERT INTO disturbance_type (disturbance_category_id, name, code)
                    SELECT 1, :name, MAX(code) + 1
                    FROM disturbance_type
                    LIMIT 1
                    """
                ), {"name": disturbance_type})

                disturbance_type_id = conn.execute(text("SELECT last_insert_rowid()")).fetchone()[0]

            conn.execute(text(
                """
                INSERT INTO transition_rule (transition_id, disturbance_type_id)
                VALUES (:transition_id, :disturbance_type_id)
                """
            ), {"transition_id": transition_id, "disturbance_type_id": disturbance_type_id})

            transition_rule_id = conn.execute(text(
                """
                SELECT id
                FROM transition_rule
                WHERE transition_id = :transition_id
                    AND disturbance_type_id = :disturbance_type_id
                """
            ), {"transition_id": transition_id, "disturbance_type_id": disturbance_type_id}).scalar()
            
            for classifier_name, classifier_col in self._match_classifier_mapping.items():
                classifier_value = transition_data_row.iloc[classifier_col]
                if not isinstance(classifier_value, str) and np.isnan(classifier_value):
                    classifier_value = "?"
                
                conn.execute(text(
                    """
                    INSERT INTO transition_rule_classifier_value (transition_rule_id, classifier_value_id)
                    VALUES (:transition_rule_id, :classifier_value_id)
                    """
                ), {
                    "transition_rule_id": transition_rule_id,
                    "classifier_value_id": classifier_value_lookup[classifier_name][classifier_value]
                })

    def _get_transition_type_lookup(self, conn: Connection) -> dict[str, int]:
        return {
            row.name: row.id
            for row in conn.execute(text("SELECT name, id FROM transition_type"))
        }

    def _get_classifier_value_lookup(self, conn: Connection) -> dict[str, dict[str, int]]:
        classifier_value_lookup = defaultdict(dict)
        for row in conn.execute(text(
            """
            SELECT cv.id, c.name AS classifier, cv.value AS classifier_value
            FROM classifier_value cv
            INNER JOIN classifier c
                ON cv.classifier_id = c.id
            """
        )):
            classifier_value_lookup[row.classifier][row.classifier_value] = row.id

        return classifier_value_lookup

    def _get_disturbance_type_lookup(self, conn: Connection) -> dict[str, int]:
        return {
            row.name.lower(): row.id
            for row in conn.execute(text("SELECT name, id FROM disturbance_type"))
        }
