using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Recliner2GCBM.Configuration;
using Recliner2GCBM.Loader.Datasource;
using Recliner2GCBM.Loader.Error;

namespace Recliner2GCBM.Loader
{
    public class GrowthCurveLoader : DataLoader
    {
        private ValueDatasource datasource;
        private IEnumerable<ClassifierReference> classifiers;
        private int speciesColumn;
        private Tuple<int, int> incrementColumns;
        private int interval;

        public GrowthCurveLoader(ValueDatasource datasource,
                                 IEnumerable<ClassifierReference> classifiers,
                                 int speciesColumn,
                                 Tuple<int, int> incrementColumns,
                                 int interval)
        {
            this.datasource = datasource;
            this.classifiers = classifiers;
            this.speciesColumn = speciesColumn;
            this.incrementColumns = incrementColumns;
            this.interval = interval;
        }

        public int Count() => 1;

        public IEnumerable<string> Load(IDbConnection outputDb)
        {
            yield return "Growth curves";

            foreach (var classifier in classifiers)
            {
                if (!classifier.Column.HasValue)
                {
                    throw new LoaderException($"Missing column mapping for classifier {classifier.Name}");
                }
            }

            using (var tx = outputDb.BeginTransaction())
            {
                var usedGCNames = new HashSet<string>();
                foreach (var row in datasource.Read())
                {
                    string gcName = BuildGrowthCurveName(row);
                    if (!usedGCNames.Contains(gcName))
                    {
                        InsertGrowthCurve(outputDb, gcName);
                        InsertGrowthCurveClassifierValues(outputDb, gcName, row);
                        usedGCNames.Add(gcName);
                    }

                    string speciesName = row[speciesColumn];
                    ValidateSpecies(outputDb, speciesName);
                    InsertGrowthCurveComponent(outputDb, gcName, speciesName);

                    var increments = ExtractIncrements(row);
                    InsertGrowthCurveComponentValues(outputDb, gcName, speciesName, increments);
                }

                tx.Commit();
            }
        }

        private string BuildGrowthCurveName(IList<string> data)
        {
            var components = new List<string>();
            foreach (var classifier in classifiers)
            {
                components.Add(data[classifier.Column.Value]);
            }

            return String.Join(",", components);
        }

        private IEnumerable<double> ExtractIncrements(IList<string> row)
        {
            for (int i = incrementColumns.Item1; i <= incrementColumns.Item2; i++)
            {
                yield return Double.Parse(row[i]);
            }
        }

        private void ValidateSpecies(IDbConnection db, string speciesName)
        {
            using (var cmd = db.CreateCommand())
            {
                try
                {
                    cmd.CommandText = "SELECT id FROM species WHERE LOWER(name) = LOWER(@speciesName)";
                    cmd.Parameters.Add(cmd.CreateParameter());
                    (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = "@speciesName";
                    (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).Value = speciesName;
                    if (cmd.ExecuteScalar() == null)
                    {
                        throw new LoaderException(
                            "GrowthCurveLoader",
                            $"Error finding species: {speciesName}.");
                    }
                }
                catch (LoaderException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new LoaderException(
                        "GrowthCurveLoader",
                        $"Error finding species: {speciesName}. Exception: {e.Message}");
                }
            }
        }

        private void InsertGrowthCurve(IDbConnection db, string name)
        {
            using (var cmd = db.CreateCommand())
            {
                try
                {
                    cmd.CommandText = @"
                        INSERT INTO growth_curve (description)
                        SELECT @name AS description";

                    cmd.Parameters.Add(cmd.CreateParameter());
                    (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = "@name";
                    (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).Value = name;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    throw new LoaderException(
                        "GrowthCurveLoader",
                        $"Failed to insert growth curve: {name}. Exception: {e.Message}");
                }
            }
        }

        private void InsertGrowthCurveComponent(IDbConnection db,
                                                string gcName,
                                                string speciesName)
        {
            using (var cmd = db.CreateCommand())
            {
                try
                {
                    cmd.CommandText = @"
                        INSERT INTO growth_curve_component (growth_curve_id, species_id)
                        SELECT gc.id, s.id
                        FROM growth_curve gc, species s
                        WHERE gc.description = @gcName
                            AND LOWER(s.name) = LOWER(@speciesName)";

                    foreach (var param in new Tuple<string, string>[]
                    {
                        new Tuple<string, string>("gcName", gcName),
                        new Tuple<string, string>("speciesName", speciesName)
                    })
                    {
                        cmd.Parameters.Add(cmd.CreateParameter());
                        (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = param.Item1;
                        (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).Value = param.Item2;
                    }

                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    throw new LoaderException(
                        "GrowthCurveLoader",
                        String.Format(
                            "Failed to insert species component '{0}' for growth curve '{1}'. Exception: {2}",
                            speciesName, gcName, e.Message));
                }
            }
        }

        private void InsertGrowthCurveComponentValues(IDbConnection db,
                                                      string gcName,
                                                      string speciesName,
                                                      IEnumerable<double> increments)
        {
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO growth_curve_component_value (growth_curve_component_id, age, merchantable_volume)
                    SELECT gcc.id, @age AS age, @volume AS volume
                    FROM growth_curve_component gcc
                    INNER JOIN growth_curve gc
                        ON gcc.growth_curve_id = gc.id
                    INNER JOIN species s
                        ON gcc.species_id = s.id
                    WHERE gc.description = @gcName
                        AND LOWER(s.name) = LOWER(@speciesName)";

                foreach (var paramName in new string[] { "@gcName", "@speciesName", "@age", "@volume" })
                {
                    cmd.Parameters.Add(cmd.CreateParameter());
                    (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = paramName;
                }

                int age = 0;
                foreach (var increment in increments)
                {
                    try
                    {
                        (cmd.Parameters[0] as DbParameter).Value = gcName;
                        (cmd.Parameters[1] as DbParameter).Value = speciesName;
                        (cmd.Parameters[2] as DbParameter).Value = age;
                        (cmd.Parameters[3] as DbParameter).Value = increment;
                        cmd.ExecuteNonQuery();
                        age += interval;
                    }
                    catch (Exception e)
                    {
                        throw new LoaderException(
                            "GrowthCurveLoader",
                            String.Format(
                                "Failed to insert values ({0}, {1}) for species '{2}' in growth curve '{3}'. Exception: {4}",
                                age, increment, speciesName, gcName, e.Message));
                    }
                }
            }
        }

        private void InsertGrowthCurveClassifierValues(IDbConnection db,
                                                       string gcName,
                                                       IList<string> row)
        {
            using (var cmd = db.CreateCommand())
            {
                try
                {
                    cmd.CommandText = @"
                        INSERT INTO growth_curve_classifier_value (growth_curve_id, classifier_value_id)
                        SELECT gc.id, cv.id
                        FROM growth_curve gc, classifier_value cv
                        INNER JOIN classifier c
                            ON cv.classifier_id = c.id
                        WHERE gc.description = @gcName
                            AND c.name = @classifierName
                            AND cv.value = @classifierValue";

                    foreach (var paramName in new string[] { "@gcName", "@classifierName", "@classifierValue" })
                    {
                        cmd.Parameters.Add(cmd.CreateParameter());
                        (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = paramName;
                    }

                    foreach (var classifier in classifiers)
                    {
                        (cmd.Parameters[0] as DbParameter).Value = gcName;
                        (cmd.Parameters[1] as DbParameter).Value = classifier.Name;
                        (cmd.Parameters[2] as DbParameter).Value = row[classifier.Column.Value];
                        if (cmd.ExecuteNonQuery() != 1)
                        {
                            throw new LoaderException(
                                "GrowthCurveLoader",
                                String.Format(
                                    "Failed to insert classifier value '{0}: {1}' for growth curve '{2}'.",
                                    classifier.Name, row[classifier.Column.Value], gcName));
                        }
                    }
                }
                catch (LoaderException) { throw; }
                catch (Exception e)
                {
                    throw new LoaderException(
                        "GrowthCurveLoader",
                        $"Failed to insert classifier value for growth curve '{gcName}'. Exception: {e.Message}");
                }
            }
        }
    }
}
