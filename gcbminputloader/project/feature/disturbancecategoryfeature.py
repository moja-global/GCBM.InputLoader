from __future__ import annotations
import logging
from sqlalchemy import text
from gcbminputloader.project.feature.feature import Feature
from gcbminputloader.util.db import get_connection

class DisturbanceCategoryFeature(Feature):
   
    def __init__(self, category_mappings: dict[str, int]):
        self._category_mappings = category_mappings

    def create(self, output_connection_string: str):
        logging.info("Loading disturbance categories...")
        with get_connection(output_connection_string, optimize=True) as output_db:
            valid_disturbance_types = {
                row.name.lower()
                for row in output_db.execute(text("SELECT name FROM disturbance_type"))
            }
            
            invalid_disturbance_types = set(
                (c.lower() for c in self._category_mappings.keys())
            ).difference(valid_disturbance_types)

            if invalid_disturbance_types:
                raise RuntimeError(
                    "Invalid disturbance types found in disturbance category configuration: "
                    + ", ".join(invalid_disturbance_types)
                )

            category_ids = {
                row.code.lower(): row.id
                for row in output_db.execute(text("SELECT code, id FROM disturbance_category"))
            }
            
            for disturbance_type, category in self._category_mappings.items():
                output_db.execute(text(
                    """
                    UPDATE disturbance_type
                    SET disturbance_category_id = :category_id
                    WHERE LOWER(name) = LOWER(:disturbance_type)
                    """
                ), {"category_id": category_ids[category.lower()],
                    "disturbance_type": disturbance_type})
                
    def save(self, config: Configuration):
        logging.info("  disturbance categories")
        config["disturbance_type_categories"] = self._category_mappings
