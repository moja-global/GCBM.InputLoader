using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using Recliner2GCBM.Loader.Util;

namespace Recliner2GCBM.Loader
{
    public class SQLiteDataLoader : DataLoader
    {
        private readonly IEnumerable<SQLLoaderMapping> loaderMappings;
        private string inputDbPath;

        public SQLiteDataLoader(string inputDbPath,
                                params SQLLoaderMapping[] loaderMappings)
        {
            this.inputDbPath = inputDbPath;
            this.loaderMappings = loaderMappings;
        }

        public int Count()
        {
            return loaderMappings.Count();
        }

        public IEnumerable<string> Load(IDbConnection outputDb)
        {
            using (var inputDb = GetConnection(inputDbPath))
            {
                foreach (var mapping in loaderMappings)
                {
                    yield return mapping.Name;
                    LoadMapping(inputDb, outputDb, mapping);
                }
            }
        }

        private IDbConnection GetConnection(string path)
        {
            var inputDb = new SQLiteConnection($"Data Source={path};Version=3;");
            inputDb.Open();

            return inputDb;
        }

        private void LoadMapping(IDbConnection inputDb,
                                 IDbConnection outputDb,
                                 SQLLoaderMapping mapping)
        {
            using (var command = inputDb.CreateCommand())
            {
                command.CommandText = mapping.FetchSQL;
                var results = command.ExecuteReader();
                using (var tx = outputDb.BeginTransaction())
                {
                    using (var cmd = outputDb.CreateCommand())
                    {
                        cmd.CommandText = mapping.LoadSQL;

                        var parameters = QueryHelper.ExtractParameters(mapping.LoadSQL);
                        foreach (var parameter in parameters)
                        {
                            cmd.Parameters.Add(cmd.CreateParameter());
                            (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = $"@{parameter}";
                        }

                        while (results.Read())
                        {
                            for (int i = 0; i < parameters.Count(); i++)
                            {
                                var parameter = parameters[i];
                                (cmd.Parameters[i] as DbParameter).Value = results[parameter];
                            }

                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
        }
    }
}
