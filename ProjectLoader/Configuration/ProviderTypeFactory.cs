using System.Collections.Generic;
using System.Linq;

namespace Recliner2GCBM.Configuration
{
    public class ProviderTypeFactory
    {
        private readonly IEnumerable<ProviderType> providerTypes = new List<ProviderType>()
        {
            new ProviderType("SQLite",
                             "System.Data.SQLite",
                             "Data Source={0};Version=3;",
                             new List<string>() { "path" }),

            new ProviderType("PostgreSQL",
                             "Npgsql",
                             "Host={0};Username={1};Password={2};Database={3};Search Path={4}",
                             new List<string>() { "host", "username", "password", "database", "schema" })
        };

        public IEnumerable<string> ProviderNames => from provider in providerTypes
                                                    select provider.Name;
        
        public ProviderType GetByName(string name)
        {
            return (from provider in providerTypes
                    where provider.Name == name
                    select provider).FirstOrDefault();
        }
    }
}
