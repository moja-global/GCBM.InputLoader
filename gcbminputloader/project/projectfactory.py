from __future__ import annotations
import logging
import gcbminputloader
from gcbminputloader.util.configuration import Configuration
from gcbminputloader.project.project import Project
from gcbminputloader.project.project import ProjectType
from gcbminputloader.project.feature.growthcurvefeature import GrowthCurveFeature
from gcbminputloader.project.feature.transitionrulefeature import TransitionRuleFeature
from gcbminputloader.project.feature.disturbancecategoryfeature import DisturbanceCategoryFeature
from enum import Enum

class ConfigurationType(Enum):
   
    GcbmInputLoader = 0
    Recliner2Gcbm = 1

class ProjectFactory:
    
    def load(self, config_path: [str, Path]) -> Project:
        config = Configuration.load(config_path)
        config_type = self._get_config_type(config)
        project_type = self._get_project_type(config)

        logging.debug(f"Configuration type: {config_type.name}")
        logging.debug(f"Project type: {project_type.name}")

        if config_type == ConfigurationType.Recliner2Gcbm:
            return self._load_recliner2gcbm_project(project_type, config)
        
        return self._load_project(project_type, config)
        
    def _load_project(self, project_type: ProjectType, config: Configuration) -> Project:
        aidb_path = config.resolve(config["aidb"])
        project = Project(project_type, aidb_path, config["classifiers"])
        for feature_name, feature_config in config.get("features", {}).items():
            project.add_feature(self._build_feature(project, feature_name, feature_config))

        return project
    
    def _load_recliner2gcbm_project(self, project_type: ProjectType, config: Configuration) -> Project:
        aidb_path = config.resolve(config["AIDBPath"])
        classifiers = [c["Name"] for c in config["ClassifierSet"]]
        project = Project(project_type, aidb_path, classifiers)
        
        growth_curve_config = config.get("GrowthCurves", {})
        if growth_curve_config.get("Path"):
            project.add_feature(self._build_feature(project, "growth_curves", Configuration({
                "path": growth_curve_config.resolve(growth_curve_config["Path"]),
                "interval": growth_curve_config["Interval"],
                "aidb_species_col": growth_curve_config["SpeciesCol"],
                "increment_start_col": growth_curve_config["IncrementStartCol"],
                "increment_end_col": growth_curve_config["IncrementEndCol"],
                "classifier_cols": {c["Name"]: c["Column"] for c in growth_curve_config["Classifiers"]},
                "worksheet": growth_curve_config.get("Page"),
                "header": growth_curve_config.get("Header")
            }, config.config_path, config.working_path)))
            
        transition_config = config.get("TransitionRules", {})
        if transition_config.get("Path"):
            project.add_feature(self._build_feature(project, "transition_rules", Configuration({
                "path": transition_config.resolve(transition_config["Path"]),
                "id_col": transition_config["NameCol"],
                "reset_age_col": transition_config["AgeCol"],
                "regen_delay_col": transition_config["DelayCol"],
                "classifier_transition_cols": {c["Name"]: c["Column"] for c in transition_config["Classifiers"]},
                "worksheet": transition_config.get("Page"),
                "header": transition_config.get("Header"),
                "reset_age_type_col": transition_config.get("TypeCol"),
                "disturbance_type_col": transition_config.get("RuleDisturbanceTypeCol"),
                "classifier_matching_cols": (
                    {c["Name"]: c["Column"] for c in transition_config["Classifiers"]}
                    if transition_config["RuleClassifiers"][0].get("Column")
                    else None
                )
            }, config.config_path, config.working_path)))
            
        disturbance_category_config = config.get("DisturbanceTypeCategories")
        if disturbance_category_config:
            project.add_feature(self._build_feature(
                project, "disturbance_type_categories", disturbance_category_config
            ))

        return project
    
    def _get_config_type(self, config: Configuration) -> ConfigurationType:
        if "Project" in config:
            return ConfigurationType.Recliner2Gcbm
        
        return ConfigurationType.GcbmInputLoader

    def _get_project_type(self, config: Configuration) -> ProjectType:
        recliner2gcbm_project_type = config.get("Project", {}).get("Configuration")
        if recliner2gcbm_project_type is not None:
            return (
                ProjectType.LegacyGcbmClassicSpatialNoGrowthCurves if recliner2gcbm_project_type == 1
                else ProjectType.LegacyGcbmClassicSpatial
            )
            
        return ProjectType[config["project_type"]]

    def _build_feature(self, project: Project, feature_name: str, feature_config: Configuration) -> Feature:
        if feature_name == "growth_curves":
            logging.debug("Building growth curve feature")
            classifier_mapping = project.create_classifier_mapping(feature_config["classifier_cols"])
            return GrowthCurveFeature(
                feature_config.resolve(feature_config["path"]), feature_config["interval"],
                feature_config["aidb_species_col"], feature_config["increment_start_col"],
                feature_config["increment_end_col"], classifier_mapping, feature_config.get("worksheet"),
                feature_config.get("header")
            )
        
        if feature_name == "transition_rules":
            logging.debug("Building transition rules feature")
            transition_classifier_mapping = project.create_classifier_mapping(
                feature_config["classifier_transition_cols"])
            
            match_classifier_mapping = (
                project.create_classifier_mapping(feature_config["classifier_matching_cols"])
                if "classifier_matching_cols" in feature_config else None
            )

            return TransitionRuleFeature(
                feature_config.resolve(feature_config["path"]), feature_config["id_col"],
                feature_config["regen_delay_col"], feature_config["reset_age_col"],
                transition_classifier_mapping, feature_config.get("worksheet"),
                feature_config.get("header"), feature_config.get("reset_age_type_col"),
                feature_config.get("disturbance_type_col"), match_classifier_mapping
            )
        
        if feature_name == "disturbance_type_categories":
            logging.debug("Building disturbance type categories feature")
            return DisturbanceCategoryFeature(feature_config)
        
        raise RuntimeError(f"Unknown feature: {feature_name}")
