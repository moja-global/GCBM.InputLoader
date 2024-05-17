using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Recliner2GCBM.Loader
{
    public class StaticDataLoader : DataLoader
    {
        private readonly IEnumerable<StaticLoaderMapping> loaderMappings;

        public StaticDataLoader(params StaticLoaderMapping[] loaderMappings)
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

        private void LoadMapping(IDbConnection outputDb, StaticLoaderMapping mapping)
        {
            using (var tx = outputDb.BeginTransaction())
            {
                using (var cmd = outputDb.CreateCommand())
                {
                    cmd.CommandText = String.Format(
                        "INSERT INTO {0} ({1}) VALUES ({2})",
                        mapping.Table,
                        String.Join(",", mapping.Fields),
                        String.Join(",", from field in mapping.Fields select $"@{field}"));

                    foreach (var field in mapping.Fields)
                    {
                        cmd.Parameters.Add(cmd.CreateParameter());
                        (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = $"@{field}";
                    }

                    foreach (var row in mapping.Data)
                    {
                        int paramIndex = 0;
                        foreach (var value in row)
                        {
                            (cmd.Parameters[paramIndex++] as DbParameter).Value = value;
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
        }
    }
}
