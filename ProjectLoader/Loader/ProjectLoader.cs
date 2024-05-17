using System.Linq;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System;
using Recliner2GCBM.Configuration;
using System.Data;
using Recliner2GCBM.Loader.Error;

namespace Recliner2GCBM.Loader
{
    public class ProjectLoader
    {
        private readonly ProviderTypeFactory providerFactory;
        private readonly IEnumerable<DataLoader> loaders;

        public ProjectLoader(ProviderTypeFactory providerFactory, IEnumerable<DataLoader> loaders)
        {
            this.providerFactory = providerFactory;
            this.loaders = loaders;
        }

        public int Count()
        {
            int count = 0;
            foreach (var loader in loaders)
            {
                count += loader.Count();
            }

            return count;
        }

        public IEnumerable<string> Load(ProviderConfiguration providerConfig)
        {
            var provider = providerFactory.GetByName(providerConfig.Name);
            if (provider.IsFileDb)
            {
                var dbPath = (from param in providerConfig.Parameters
                              where param.Key == "path"
                              select param.Value).FirstOrDefault();

                if (String.IsNullOrWhiteSpace(dbPath))
                {
                    throw new LoaderException(
                        $"Path not specified for {providerConfig.Name} provider");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }

            var outputDb = GetConnection(
                provider.Invariant,
                provider.GetConnectionString(providerConfig.Parameters));

            try
            {
                foreach (var loader in loaders)
                {
                    foreach (var name in loader.Load(outputDb))
                    {
                        yield return name;
                    }
                }
            }
            finally
            {
                outputDb.Close();
            }
        }

        private IDbConnection GetConnection(string providerInvariant, string connectionString)
        {
            IDbConnection conn;
            try
            {
                var factory = DbProviderFactories.GetFactory(providerInvariant);
                conn = factory.CreateConnection();
                conn.ConnectionString = connectionString;
                conn.Open();
            }
            catch
            {
                conn = null;
                throw;
            }

            return conn;
        }
    }
}
