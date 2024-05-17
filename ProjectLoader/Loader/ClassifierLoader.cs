using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Recliner2GCBM.Loader.Error;

namespace Recliner2GCBM.Loader
{
    public class ClassifierLoader : DataLoader
    {
        private readonly IEnumerable<ClassifierLoaderMapping> loaderMappings;

        public ClassifierLoader(params ClassifierLoaderMapping[] loaderMappings)
        {
            this.loaderMappings = loaderMappings;
        }

        public int Count() => loaderMappings.Count();

        public IEnumerable<string> Load(IDbConnection outputDb)
        {
            foreach (var mapping in loaderMappings)
            {
                yield return mapping.Name;
                LoadMapping(outputDb, mapping);
            }
        }

        private void LoadMapping(IDbConnection outputDb, ClassifierLoaderMapping mapping)
        {
            using (var tx = outputDb.BeginTransaction())
            {
                using (var cmd = outputDb.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO classifier (name) VALUES (@name)";
                    cmd.Parameters.Add(cmd.CreateParameter());
                    (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = "@name";
                    (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).Value = mapping.ClassifierName;
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = outputDb.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO classifier_value (classifier_id, value, description)
                        SELECT id, @value as value, @value as description
                        FROM classifier WHERE name = @name";

                    foreach (var paramName in new string[] { "@value", "@name" })
                    {
                        cmd.Parameters.Add(cmd.CreateParameter());
                        (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = paramName;
                    }

                    var uniqueValues = from row in mapping.Datasource.Read()
                                       where row[mapping.Column] != "?"
                                       group row by row[mapping.Column] into v
                                       select v.Key;

                    foreach (var row in uniqueValues)
                    {
                        (cmd.Parameters[0] as DbParameter).Value = row;
                        (cmd.Parameters[1] as DbParameter).Value = mapping.ClassifierName;
                        if (cmd.ExecuteNonQuery() != 1)
                        {
                            throw new LoaderException(
                                "ClassifierLoader",
                                $"Failed to insert classifier value '{mapping.ClassifierName}: {row}'.");
                        }
                    }

                    // Add the wildcard value for the classifier.
                    (cmd.Parameters[0] as DbParameter).Value = "?";
                    (cmd.Parameters[1] as DbParameter).Value = mapping.ClassifierName;
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
        }
    }
}
