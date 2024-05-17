using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using Recliner2GCBM.Loader.Datasource;

namespace Recliner2GCBM.Loader
{
    public class ValueDatasourceLoader : DataLoader
    {
        ValueDatasource datasource;
        IList<ValueDatasourceLoaderMapping> mappings;

        public ValueDatasourceLoader(ValueDatasource datasource,
                                     params ValueDatasourceLoaderMapping[] mappings)
        {
            this.datasource = datasource;
            this.mappings = mappings;
        }

        public int Count() => mappings.Count;

        public IEnumerable<string> Load(IDbConnection outputDb)
        {
            foreach (var mapping in mappings)
            {
                yield return mapping.Name;
                LoadMapping(outputDb, mapping);
            }
        }

        private void LoadMapping(IDbConnection outputDb, ValueDatasourceLoaderMapping mapping)
        {
            using (var tx = outputDb.BeginTransaction())
            {
                using (var cmd = outputDb.CreateCommand())
                {
                    cmd.CommandText = mapping.LoadSQL;
                    foreach (var paramMapping in mapping.ParameterMappings)
                    {
                        cmd.Parameters.Add(cmd.CreateParameter());
                        (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = paramMapping.Item1;
                    }

                    foreach (var row in datasource.Read())
                    {
                        cmd.CommandText = mapping.LoadSQL;
                        for (int i = 0; i < mapping.ParameterMappings.Count; i++)
                        {
                            var paramMapping = mapping.ParameterMappings[i];
                            (cmd.Parameters[i] as DbParameter).Value = row[paramMapping.Item2];
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
        }
    }
}
