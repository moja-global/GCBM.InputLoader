using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Recliner2GCBM.Loader;
using Recliner2GCBM.Loader.Datasource;
using Recliner2GCBM.Loader.Error;
using System.Reflection;

namespace Recliner2GCBM.Configuration
{
    public class DataLoaderFactory
    {
        private Dictionary<ProjectType, string> loaderConfigs = new Dictionary<ProjectType, string>()
        {
            {
                new ProjectType(ProjectMode.SpatiallyExplicit, ModuleConfiguration.CBMClassic),
                "cbm_classic_spatial"
            },
            {
                new ProjectType(ProjectMode.SpatiallyExplicit, ModuleConfiguration.CBMNoGrowthCurves),
                "cbm_spatial_no_gc_tables"
            }
        };

        private ProviderTypeFactory providerFactory;

        public DataLoaderFactory(ProviderTypeFactory providerFactory)
        {
            this.providerFactory = providerFactory;
        }

        public ProjectLoader GetLoader(ProjectConfiguration config)
        {
            return new ProjectLoader(providerFactory, GetLoaders(config));
        }

        private IEnumerable<DataLoader> GetLoaders(ProjectConfiguration config)
        {
            var loaderConfigPath = GetLoaderConfigPath(config);
            var loaderTaskConfigFiles = JsonConvert.DeserializeObject<IList<string>>(
                ReadLoaderResource(loaderConfigPath));

            string currentTaskType = null;
            var currentTaskConfigs = new List<JToken>();
            JObject taskData = null;
            foreach (var taskConfigFile in loaderTaskConfigFiles)
            {
                DataLoader internalTask = null;
                string taskType = null;

                // Check if it's a reference to a special internally-defined loader.
                switch (taskConfigFile)
                {
                    case "ClassifierLoader":
                        taskType = "ClassifierLoader";
                        internalTask = CreateClassifierLoader(config);
                        break;
                    case "GrowthCurveLoader":
                        taskType = "GrowthCurveLoader";
                        internalTask = CreateGrowthCurveLoader(config);
                        break;
                    case "TransitionLoader":
                        if (!String.IsNullOrWhiteSpace(config.TransitionRules.Path))
                        {
                            taskType = "TransitionLoader";
                            internalTask = CreateTransitionLoader(config);
                        }
                        break;
                    case "DisturbanceCategoryLoader":
                        taskType = "DisturbanceCategoryLoader";
                        internalTask = CreateDisturbanceCategoryLoader(config);
                        break;
                    default:
                        taskData = JObject.Parse(ReadLoaderResource(taskConfigFile));
                        taskType = taskData.First.Path;
                        break;
                }

                if (taskType == null)
                {
                    continue;
                }

                // Process the loader and its children if the loader type has changed.
                if (taskType != currentTaskType)
                {
                    if (currentTaskConfigs.Count > 0)
                    {
                        yield return BuildLoader(config, currentTaskType, currentTaskConfigs);
                    }

                    currentTaskType = taskType;
                    currentTaskConfigs.Clear();
                }

                // Return the internal loader or accumulate the externally-defined
                // loader child.
                if (internalTask != null)
                {
                    yield return internalTask;
                }
                else
                {
                    var taskConfig = taskData.First.First;
                    currentTaskConfigs.Add(taskConfig);
                }
            }

            if (currentTaskConfigs.Count > 0)
            {
                yield return BuildLoader(config, currentTaskType, currentTaskConfigs);
            }
        }

        private DataLoader BuildLoader(ProjectConfiguration projectConfig,
                                       string mappingType,
                                       IEnumerable<JToken> mappingConfigs)
        {
            switch (mappingType)
            {
                case "SQLLoaderMapping":
                    return CreateSQLLoader(projectConfig, mappingConfigs);
                case "StaticLoaderMapping":
                    return CreateStaticLoader(projectConfig, mappingConfigs);
                case "InternalLoaderMapping":
                    return CreateInternalLoader(projectConfig, mappingConfigs);
                default:
                    throw new LoaderException($"Unknown loader type: {mappingType}");
            }
        }

        private string GetLoaderConfigPath(ProjectConfiguration config)
        {
            var loaderConfigPath = $"{loaderConfigs[config.Project]}.json";
            if (config.AIDBPath.EndsWith(".mdb"))
            {
                loaderConfigPath = $"legacy_{loaderConfigPath}";
            }

            return loaderConfigPath;
        }

        private Uri ResourceUri(params string[] segments)
        {
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return new Uri($"{exePath}/Resources/Loader/{String.Join("/", segments)}", UriKind.Relative);
        }

        private string ReadLoaderResource(string relativePath)
        {
            var uri = ResourceUri(relativePath);
            return File.OpenText(uri.ToString()).ReadToEnd();
        }

        private bool ResourceExists(string relativePath)
        {
            return File.Exists(ResourceUri(relativePath).ToString());
        }

        private DataLoader CreateInternalLoader(ProjectConfiguration config,
                                                IEnumerable<JToken> mappingConfigs)
        {
            var mappings = new List<InternalLoaderMapping>();
            foreach (var mappingConfig in mappingConfigs) {
                if (mappingConfig["sql_file"] != null)
                {
                    var genericSqlPath = mappingConfig["sql_file"];
                    var providerSpecificSqlPath = $"{genericSqlPath}.{config.OutputConfiguration.Name.ToLower()}";
                    var sqlPath = ResourceExists(providerSpecificSqlPath)
                        ? providerSpecificSqlPath
                        : genericSqlPath;

                    mappings.Add(new InternalLoaderMapping(
                        mappingConfig["name"].ToString(),
                        ReadLoaderResource(sqlPath.ToString())));
                }
                else
                {
                    mappings.Add(new InternalLoaderMapping(
                        mappingConfig["name"].ToString(),
                        mappingConfig["sql"].ToString()));
                }
            }

            return new InternalDataLoader(mappings.ToArray());
        }

        private DataLoader CreateStaticLoader(ProjectConfiguration config,
                                              IEnumerable<JToken> mappingConfigs)
        {
            var mappings = new List<StaticLoaderMapping>();
            foreach (var mappingConfig in mappingConfigs)
            {
                var name = mappingConfig["name"].ToString();
                var table = mappingConfig["table"].ToString();
                var fields = from field in mappingConfig["fields"] select field.ToString();
                var datasets = new List<IEnumerable<object>>();
                foreach (var dataset in mappingConfig["data"].ToArray())
                {
                    var values = new List<object>();
                    foreach (var value in dataset)
                    {
                        values.Add(value.ToObject<object>());
                    }

                    datasets.Add(values);
                }

                mappings.Add(new StaticLoaderMapping(name, table, fields, datasets.ToArray()));
            }

            return new StaticDataLoader(mappings.ToArray());
        }
        
        private DataLoader CreateSQLLoader(ProjectConfiguration config,
                                           IEnumerable<JToken> mappingConfigs)
        {
            if (config.AIDBPath.EndsWith(".mdb"))
            {
                return new AccessDataLoader(
                    config.AIDBPath,
                    (
                        from mappingConfig in mappingConfigs
                        select new SQLLoaderMapping(
                            mappingConfig["name"].ToString(),
                            mappingConfig["fetch_sql"].ToString(),
                            mappingConfig["load_sql"].ToString())
                    ).ToArray());
            }
            else if (config.AIDBPath.EndsWith(".db"))
            {
                return new SQLiteDataLoader(
                    config.AIDBPath,
                    (
                        from mappingConfig in mappingConfigs
                        select new SQLLoaderMapping(
                            mappingConfig["name"].ToString(),
                            mappingConfig["fetch_sql"].ToString(),
                            mappingConfig["load_sql"].ToString())
                    ).ToArray());
            }

            throw new LoaderException("Unrecognized ArchiveIndex database type");
        }

        private ValueDatasource CreateValueDatasource(string path, int page, bool header)
        {
            var ext = Path.GetExtension(path);
            switch (ext)
            {
                case ".xls" :
                case ".xlsx": return new XLSXDatasource(path, page, header);
                case ".csv" : return new CSVDatasource(path, header);
            }
    
            return null;
        }

        private DataLoader CreateDisturbanceCategoryLoader(ProjectConfiguration config)
        {
            return new ValueDatasourceLoader(
                new ListDatasource(
                    from item in config.DisturbanceTypeCategories
                    select new List<string> { item.Item1, item.Item2 }),
                new ValueDatasourceLoaderMapping(
                "Disturbance type categories",
                @"UPDATE disturbance_type
                  SET disturbance_category_id = (
                      SELECT id
                      FROM disturbance_category
                      WHERE code = @Category)
                  WHERE name = @DisturbanceType",
                new Tuple<string, int>("DisturbanceType", 0),
                new Tuple<string, int>("Category", 1)));
        }

        private DataLoader CreateClassifierLoader(ProjectConfiguration config)
        {
            var mappings = new List<ClassifierLoaderMapping>();
            foreach (var classifier in config.ClassifierSet)
            {
                var datasource = CreateValueDatasource(classifier.Path,
                                                       classifier.Page,
                                                       classifier.Header);

                mappings.Add(new ClassifierLoaderMapping(
                    $"Classifier: {classifier.Name}",
                    classifier.Name,
                    classifier.Column,
                    datasource));
            }

            return new ClassifierLoader(mappings.ToArray());
        }

        private DataLoader CreateGrowthCurveLoader(ProjectConfiguration config)
        {
            var datasource = CreateValueDatasource(config.GrowthCurves.Path,
                                                   config.GrowthCurves.Page,
                                                   config.GrowthCurves.Header);

            return new GrowthCurveLoader(
                datasource,
                config.GrowthCurves.Classifiers,
                config.GrowthCurves.SpeciesCol,
                new Tuple<int, int>(config.GrowthCurves.IncrementStartCol,
                                    config.GrowthCurves.IncrementEndCol),
                config.GrowthCurves.Interval);
        }

        private DataLoader CreateTransitionLoader(ProjectConfiguration config)
        {
            var datasource = CreateValueDatasource(config.TransitionRules.Path,
                                                   config.TransitionRules.Page,
                                                   config.TransitionRules.Header);

            // Mapping for loading the transitions themselves: type, reset age, regen delay.
            var transitionParams = new List<Tuple<string, int>>()
            {
                new Tuple<string, int>("@Description", config.TransitionRules.NameCol),
                new Tuple<string, int>("@Age", config.TransitionRules.AgeCol),
                new Tuple<string, int>("@RegenDelay", config.TransitionRules.DelayCol)
            };

            if (config.TransitionRules.TypeCol.HasValue)
            {
                transitionParams.Add(new Tuple<string, int>(
                    "@TransitionType", config.TransitionRules.TypeCol.Value));
            }

            var transitions = new ValueDatasourceLoaderMapping(
                "Transitions",
                $@"INSERT INTO transition (transition_type_id, description, age, regen_delay)
                  SELECT t.id, @Description, CAST(@Age AS INTEGER), CAST(@RegenDelay AS INTEGER)
                  FROM transition_type t
                  WHERE LOWER(t.name) LIKE LOWER(
                      {(config.TransitionRules.TypeCol.HasValue ? "@TransitionType": "'absolute'")})",
                transitionParams.ToArray());

            // Mapping for loading the transition classifier values - the new classifier values
            // for a disturbed pixel.
            var classifierSQLParams = new List<Tuple<string, int>>()
            {
                new Tuple<string, int>("@Description", config.TransitionRules.NameCol)
            };

            var whitespace = new Regex(@"\s+");
            var classifierMatchSQL = new List<string>();
            foreach (var classifier in config.TransitionRules.Classifiers)
            {
                if (!classifier.Column.HasValue)
                {
                    throw new LoaderException($"Missing column mapping for classifier {classifier.Name}");
                }

                string valueParamName = $"@{whitespace.Replace(classifier.Name, "")}";
                classifierSQLParams.Add(new Tuple<string, int>(
                    valueParamName, classifier.Column.Value));

                classifierMatchSQL.Add($@"(
                    c.name = '{classifier.Name}'
                    AND cv.value = {valueParamName})");
            }

            var transitionClassifierValues = new ValueDatasourceLoaderMapping(
                "Transition classifier values",
                $@"INSERT INTO transition_classifier_value (transition_id, classifier_value_id)
                   SELECT t.id, cv.id
                   FROM transition t, classifier_value cv
                   INNER JOIN classifier c
                       ON c.id = cv.classifier_id
                   WHERE t.description = @Description
                       AND ({String.Join(" OR ", classifierMatchSQL)})",
                classifierSQLParams.ToArray());

            // Mapping for loading the transition rules which associate a transition
            // with a particular disturbance type.
            var transitionRuleParams = new List<Tuple<string, int>>()
            {
                new Tuple<string, int>("@Description", config.TransitionRules.NameCol)
            };

            if (config.TransitionRules.RuleDisturbanceTypeCol.HasValue)
            {
                transitionRuleParams.Add(new Tuple<string, int>(
                    "@DisturbanceType", config.TransitionRules.RuleDisturbanceTypeCol.Value));
            }

            var transitionRules = new ValueDatasourceLoaderMapping(
                "Transition rules",
                $@"INSERT INTO transition_rule (transition_id, disturbance_type_id)
                  SELECT t.id, dt.id
                  FROM transition t, disturbance_type dt
                  WHERE LOWER(t.description) = LOWER(@Description)
                      AND {(config.TransitionRules.RuleDisturbanceTypeCol.HasValue
                            ? "LOWER(dt.name) = LOWER(@DisturbanceType)"
                            : "0 = 1")}",
                transitionRuleParams.ToArray());

            // Mapping for loading the transition rule classifier values, for matching
            // a disturbance type and classifier set (with wildcards) to a transition.
            var trClassifierSQLParams = new List<Tuple<string, int>>()
            {
                new Tuple<string, int>("@Description", config.TransitionRules.NameCol)
            };

            var trClassifierMatchSQL = new List<string>();
            foreach (var classifier in config.TransitionRules.RuleClassifiers)
            {
                if (!classifier.Column.HasValue)
                {
                    continue;
                }

                string valueParamName = $"@{whitespace.Replace(classifier.Name, "")}";
                trClassifierSQLParams.Add(new Tuple<string, int>(
                    valueParamName, classifier.Column.Value));

                trClassifierMatchSQL.Add($@"(
                    c.name = '{classifier.Name}'
                    AND cv.value = {valueParamName})");
            }

            var transitionRuleClassifierValues = new ValueDatasourceLoaderMapping(
                "Transition rule classifier values",
                $@"INSERT INTO transition_rule_classifier_value (transition_rule_id, classifier_value_id)
                   SELECT tr.id, cv.id
                   FROM classifier_value cv, transition t
                   INNER JOIN transition_rule tr
                       ON tr.transition_id = t.id
                   INNER JOIN classifier c
                       ON c.id = cv.classifier_id
                   WHERE t.description = @Description AND " + (
                       trClassifierMatchSQL.Count > 0 ? $@"({String.Join(" OR ", trClassifierMatchSQL)})"
                                                      : "0 = 1"),
                trClassifierSQLParams.ToArray());

            return new ValueDatasourceLoader(
                datasource,
                transitions,
                transitionClassifierValues,
                transitionRules,
                transitionRuleClassifierValues);
        }
    }
}
