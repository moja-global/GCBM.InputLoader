import warnings
from sqlalchemy.exc import SAWarning
from pathlib import Path
from argparse import ArgumentParser
from gcbminputloader.project.projectfactory import ProjectFactory

def cli():
    warnings.filterwarnings("ignore", category=SAWarning)

    parser = ArgumentParser(description="Create GCBM input database.")
    parser.add_argument("config", type=Path, help="Path to GCBM input loader config file")
    parser.add_argument("output_path", help="Path or connection string to database to create")
    args = parser.parse_args()
    
    project = ProjectFactory().load(args.config)
    project.create(args.output_path)
