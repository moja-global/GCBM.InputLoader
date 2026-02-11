from __future__ import annotations
import logging
import gcbminputloader
from pathlib import Path
from collections import defaultdict
from sqlalchemy import text
from gcbminputloader.project.feature.feature import Feature
from gcbminputloader.util.db import get_connection
from gcbminputloader.util.json import InputLoaderJson

class DMAssociationsFeature(Feature):
    
    def __init__(self, aidb_path: [str, Path]):
        self._aidb_path = aidb_path

    def create(self, output_connection_string: str):
        logging.info("Loading disturbance matrix associations...")
        with (
            get_connection(self._aidb_path) as aidb,
            get_connection(output_connection_string, optimize=True) as output_db
        ):
            spu_lookup = self._get_spu_lookup(output_db)
            dist_type_lookup = self._get_dist_type_lookup(output_db)
            dm_lookup = self._get_dm_lookup(output_db)
            aidb_dms = (
                self._get_aidb_dm_associations(aidb)
                if Path(self._aidb_path).suffix in (".accdb", ".mdb")
                else self._get_cbm_defaults_dm_associations(aidb)
            )

            output_db.execute(
                text(
                    """
                    INSERT INTO disturbance_matrix_association (
                        spatial_unit_id, disturbance_type_id, disturbance_matrix_id
                    ) VALUES (:spu_id, :dist_type_id, :dm_id)
                    """
                ), [
                    {
                        "spu_id": spu_lookup[row.admin][row.eco],
                        "dist_type_id": dist_type_lookup[row.dist_type],
                        "dm_id": dm_lookup[row.dm]
                    } for row in aidb_dms
                ]
            )

    def _get_cbm_defaults_dm_associations(self, conn: Connection) -> CursorResult:
        return conn.execute(text(
            """
            SELECT
                a_tr.name AS admin,
                e_tr.name AS eco,
                dt_tr.name AS dist_type,
                dm.id || '_' || dm_tr.name AS dm
            FROM disturbance_matrix_association dma
            INNER JOIN spatial_unit spu
                ON dma.spatial_unit_id = spu.id
            INNER JOIN eco_boundary e
                ON spu.eco_boundary_id = e.id
            INNER JOIN eco_boundary_tr e_tr
                ON e.id = e_tr.eco_boundary_id
            INNER JOIN admin_boundary a
                ON spu.admin_boundary_id = a.id
            INNER JOIN admin_boundary_tr a_tr
                ON a.id = a_tr.admin_boundary_id
            INNER JOIN disturbance_type dt
                ON dma.disturbance_type_id = dt.id
            INNER JOIN disturbance_type_tr dt_tr
                ON dt.id = dt_tr.disturbance_type_id
            INNER JOIN disturbance_matrix dm
                ON dma.disturbance_matrix_id = dm.id
            INNER JOIN disturbance_matrix_tr dm_tr
                ON dm.id = dm_tr.disturbance_matrix_id
            WHERE e_tr.locale_id = 1
                AND a_tr.locale_id = 1
                AND dt_tr.locale_id = 1
                AND dm_tr.locale_id = 1
            """
        ))

    def _get_aidb_dm_associations(self, conn: Connection) -> CursorResult:
        resource_root = Path(gcbminputloader.__file__).parent.joinpath("resources", "Loader")
        resource_path = resource_root.joinpath("Legacy", "disturbance_matrix_associations.json")
        sql = InputLoaderJson(resource_path).load()["SQLLoaderMapping"]["fetch_sql"]

        return conn.execute(text(sql))

    def _get_spu_lookup(self, conn: Connection) -> dict[str, dict[str, int]]:
        spu_lookup = defaultdict(dict)
        for row in conn.execute(text(
            """
            SELECT spu.id AS spuid, a.name AS admin, e.name AS eco
            FROM spatial_unit spu
            INNER JOIN admin_boundary a
                ON spu.admin_boundary_id = a.id
            INNER JOIN eco_boundary e
                ON spu.eco_boundary_id = e.id
            """
        )):
            spu_lookup[row.admin][row.eco] = row.spuid

        return spu_lookup

    def _get_dist_type_lookup(self, conn: Connection) -> dict[str, dict[str, int]]:
        dist_type_lookup = {}
        for row in conn.execute(text(
            """
            SELECT id, name AS dist_type
            FROM disturbance_type
            """
        )):
            dist_type_lookup[row.dist_type] = row.id

        return dist_type_lookup

    def _get_dm_lookup(self, conn: Connection) -> dict[str, dict[str, int]]:
        dm_lookup = {}
        for row in conn.execute(text(
            """
            SELECT id, name AS dm
            FROM disturbance_matrix
            """
        )):
            dm_lookup[row.dm] = row.id

        return dm_lookup
