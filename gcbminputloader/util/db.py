import urllib
import psutil
from pathlib import Path
from contextlib import contextmanager
from sqlalchemy import create_engine
from sqlalchemy import text

@contextmanager
def get_connection(connection_string):
    connection_url = "sqlite://"
    schema = None
    if connection_string.startswith("postgresql"):
        # Raw SQLAlchemy string with an extra /schema on the end.
        connection_url, schema = connection_string.rsplit("/", 1)
        schema = schema.lower()
    elif connection_string.endswith(".db"):
        connection_url = f"sqlite:///{connection_string}"
    elif connection_string.endswith(".accdb") or connection_string.endswith(".mdb"):
        if not Path(connection_string).exists():
            raise IOError(f"File not found: {connection_string}")
        
        connection_string = (
            r"DRIVER={Microsoft Access Driver (*.mdb, *.accdb)};"
            f"DBQ={str(Path(connection_string).absolute())};"
            r"ExtendedAnsiSQL=1;"
        )

        connection_url = \
            "access+pyodbc:///?odbc_connect={}" \
            .format(urllib.parse.quote_plus(connection_string))

    engine = create_engine(connection_url, future=True)
    with engine.connect() as conn:
        try:
            with conn.begin():
                if "sqlite" in connection_url:
                    for sql in (
                        "PRAGMA journal_mode=off",
                        "PRAGMA synchronous=off",
                        "PRAGMA page_size=4096",
                        "PRAGMA temp_store=2",
                        f"PRAGMA cache_size={int(psutil.virtual_memory().available / 4096 * 0.75)}"
                    ):
                        conn.execute(text(sql))
                elif schema:
                    conn.execute(text(f"SET SEARCH_PATH={schema}"))
                
                yield conn
                
            if "sqlite" in connection_url:
                conn.execute(text("PRAGMA analysis_limit=1000"))
                conn.execute(text("PRAGMA optimize"))
                
            conn.close()
        finally:
            engine.dispose()
