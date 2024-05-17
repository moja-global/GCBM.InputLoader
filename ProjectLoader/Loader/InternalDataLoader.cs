using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Recliner2GCBM.Loader
{
    public class InternalDataLoader : DataLoader
    {
        private readonly IEnumerable<InternalLoaderMapping> loaderMappings;

        public InternalDataLoader(params InternalLoaderMapping[] loaderMappings)
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

        private void LoadMapping(IDbConnection outputDb, InternalLoaderMapping mapping)
        {
            using (var tx = outputDb.BeginTransaction())
            {
                using (var cmd = outputDb.CreateCommand())
                {
                    cmd.CommandText = mapping.SQL;
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
        }
    }
}
