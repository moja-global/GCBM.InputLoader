from __future__ import annotations
import warnings
import logging
from logging import FileHandler
from logging import StreamHandler
from sqlalchemy.exc import SAWarning
from pathlib import Path
from argparse import ArgumentParser
from gcbminputloader.project.projectfactory import ProjectFactory

def cli():
    warnings.filterwarnings("ignore", category=SAWarning)

    parser = ArgumentParser(description="Create GCBM input database.")
    parser.add_argument("config", type=Path, help="Path to GCBM input loader config file")
    parser.add_argument("output_path", help="Path or connection string to database to create")
    parser.add_argument("--log_path", type=Path, help="Path to log file directory")
    parser.add_argument("--log_level", help="Log level", default="INFO")
    args = parser.parse_args()
    
    log_path = (args.log_path or (
        Path(args.output_path) if "//" not in args.output_path
        else args.config
    ).parent).joinpath("gcbminputloader.log")

    log_path.parent.mkdir(parents=True, exist_ok=True)
    
    logging.basicConfig(level=args.log_level.upper(), format="%(asctime)s %(message)s", handlers=[
        FileHandler(log_path, mode="w"),
        StreamHandler()
    ])

    logging.info(f"Creating {args.output_path} from {args.config}")
    logging.info(f"Log: {log_path}")
    
    project = ProjectFactory().load(args.config)
    project.create(args.output_path)
