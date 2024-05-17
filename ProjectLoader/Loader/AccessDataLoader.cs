using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SQLite;
using System.Linq;
using Recliner2GCBM.Loader.Util;

namespace Recliner2GCBM.Loader
{
    public class AccessDataLoader : DataLoader
    {
        private readonly IEnumerable<SQLLoaderMapping> loaderMappings;
        private string inputDbPath;

        public AccessDataLoader(string inputDbPath, params SQLLoaderMapping[] loaderMappings)
        {
            this.inputDbPath = inputDbPath;
            this.loaderMappings = loaderMappings;
        }

        public int Count() => loaderMappings.Count();

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

        private OleDbConnection GetConnection(string path)
        {
            string provider = Environment.Is64BitProcess
                ? "Microsoft.ACE.OLEDB.12.0"
                : "Microsoft.Jet.OLEDB.4.0";

            var connectionString = $"Provider={provider};Data Source={path};";
            var inputDb = new OleDbConnection(connectionString);
            inputDb.Open();

            return inputDb;
        }

        private void LoadMapping(OleDbConnection inputDb,
                                 IDbConnection outputDb,
                                 SQLLoaderMapping mapping)
        {
            using (var command = new OleDbCommand(mapping.FetchSQL, inputDb))
            {
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
                            (cmd.Parameters[cmd.Parameters.Count - 1] as DbParameter).ParameterName = parameter;
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
