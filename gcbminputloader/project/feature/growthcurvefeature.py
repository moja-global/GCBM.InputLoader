import logging
from collections import defaultdict
from itertools import chain
from sqlalchemy import text
from gcbminputloader.project.feature.feature import Feature
from gcbminputloader.util.db import get_connection

class GrowthCurveFeature(Feature):
    
    def __init__(
        self, path, interval, aidb_species_col, increment_start_col, increment_end_col,
        classifier_mapping, worksheet=None, header=True
    ):
        self._path = path
        self._interval = interval
        self._aidb_species_col = aidb_species_col
        self._increment_start_col = increment_start_col
        self._increment_end_col = increment_end_col
        self._classifier_mapping = classifier_mapping
        self._worksheet = worksheet
        self._header = header

    def create(self, output_connection_string):
        logging.info("Loading growth curves...")
        with get_connection(output_connection_string) as output_db:
            gc_data = self._load_data(self._path, self._header, sheet_name=self._worksheet)
            self._load_classifier_values(output_db, gc_data)
            
            species_id_lookup = {
                row.name.lower(): row.id
                for row in output_db.execute(text("SELECT id, name FROM species"))
            }
            
            invalid_species = set(
                (s.lower() for s in gc_data[gc_data.columns[self._aidb_species_col]].unique())
            ).difference(set(species_id_lookup.keys()))

            if invalid_species:
                raise RuntimeError(
                    f"Invalid species found in growth curves: {', '.join(invalid_species)}"
                )

            gc_id_lookup = self._load_growth_curves(output_db, gc_data)

            logging.info("  growth curve components")
            for _, gc_data_row in gc_data.iterrows():
                gc_id = gc_id_lookup[self._get_gc_name(gc_data_row)]
                species_id = species_id_lookup[gc_data_row.iloc[self._aidb_species_col].lower()]
                self._load_growth_curve_component(output_db, gc_id, species_id, gc_data_row)

    def save(self, config_path):
        raise NotImplementedError()

    def _load_classifier_values(self, conn, gc_data):
        logging.info("  classifier values")
        unique_classifier_values = {
            classifier_name: gc_data[gc_data.columns[classifier_col]].unique()
            for classifier_name, classifier_col in self._classifier_mapping.items()
        }
            
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
            ), [{"classifier_id": classifier_ids[classifier_name], "value": str(value)}
                for value in chain(classifier_values, ["?"])])
        
    def _get_gc_name(self, gc_data_row):
        classifier_values = [
            gc_data_row.iloc[col] for col in self._classifier_mapping.values()
        ]
        
        return ",".join((str(v) for v in classifier_values))

    def _load_growth_curves(self, conn, gc_data):
        logging.info("  growth curves")
        gc_id_lookup = {}
        classifier_value_lookup = self._get_classifier_value_lookup(conn)
        for _, gc_data_row in gc_data.iterrows():
            gc_name = self._get_gc_name(gc_data_row)
            if gc_name in gc_id_lookup:
                continue

            conn.execute(
                text("INSERT INTO growth_curve (description) VALUES (:name)"),
                {"name": gc_name}
            )
            
            gc_id = conn.execute(
                text("SELECT id FROM growth_curve WHERE description = :name"),
                {"name": gc_name}
            ).scalar()
        
            classifier_value_ids = [
                classifier_value_lookup[classifier_name][str(gc_data_row.iloc[classifier_col])]
                for classifier_name, classifier_col in self._classifier_mapping.items()
            ]

            conn.execute(text(
                """
                INSERT INTO growth_curve_classifier_value (growth_curve_id, classifier_value_id)
                VALUES (:gc_id, :cv_id)
                """
            ), [{"gc_id": gc_id, "cv_id": classifier_value_id}
                for classifier_value_id in classifier_value_ids])
            
            gc_id_lookup[gc_name] = gc_id
        
        return gc_id_lookup

    def _get_classifier_value_lookup(self, conn):
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

    def _load_growth_curve_component(self, conn, gc_id, species_id, gc_data_row):
        conn.execute(text(
            """
            INSERT INTO growth_curve_component (growth_curve_id, species_id)
            VALUES (:gc_id, :sp_id)
            """
        ), {"gc_id": gc_id, "sp_id": species_id})
        
        increments = [
            float(gc_data_row.iloc[col])
            for col in range(self._increment_start_col, self._increment_end_col + 1)
        ]
        
        gc_component_id = conn.execute(text(
            "SELECT id FROM growth_curve_component WHERE growth_curve_id = :gc_id AND species_id = :sp_id"
        ), {"gc_id": gc_id, "sp_id": species_id}).scalar()
        
        conn.execute(text(
            """
            INSERT INTO growth_curve_component_value (growth_curve_component_id, age, merchantable_volume)
            VALUES (:component_id, :age, :increment)
            """
        ), [{"component_id": gc_component_id, "age": i * self._interval, "increment": increment}
            for i, increment in enumerate(increments)])
