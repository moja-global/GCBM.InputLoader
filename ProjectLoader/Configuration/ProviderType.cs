using System;
using System.Linq;
using System.Collections.Generic;
using Recliner2GCBM.Loader.Error;

namespace Recliner2GCBM.Configuration
{
    public class ProviderType
    {
        private string name;
        private string invariant;
        private string connectionStringTemplate;

        public ProviderType(string name,
                            string invariant,
                            string connectionStringTemplate,
                            IEnumerable<string> connectionParameters)
        {
            this.name = name;
            this.invariant = invariant;
            this.connectionStringTemplate = connectionStringTemplate;
            ConnectionParameters = connectionParameters;
        }

        public string Name => name;
        public string Invariant => invariant;
        public bool IsFileDb => ConnectionParameters.Contains("path");
        public IEnumerable<string> ConnectionParameters { get; private set; }

        public string GetConnectionString(IDictionary<string, string> configuration)
        {
            var missingParameters = from parameter in ConnectionParameters
                                    where !configuration.ContainsKey(parameter)
                                    select parameter;

            if (missingParameters.Count() > 0)
            {
                throw new LoaderException(
                    $"{name} provider configuration is incomplete - missing " +
                    String.Join(",", missingParameters));
            }

            return String.Format(connectionStringTemplate,
                                 (from parameter in ConnectionParameters
                                  select configuration[parameter]).ToArray());
        }
    }
}