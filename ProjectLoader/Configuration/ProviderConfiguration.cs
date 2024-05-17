using Recliner2GCBM.ViewModel.Support;
using System;
using System.Collections.Generic;

namespace Recliner2GCBM.Configuration
{
    public class ProviderConfiguration : BindableBase
    {
        private string name;
        private IDictionary<string, string> parameters = new Dictionary<string, string>();

        public ProviderConfiguration(string name, params string[] paramNames)
        {
            this.name = name;
            foreach (var paramName in paramNames)
            {
                parameters[paramName] = String.Empty;
            }
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public IDictionary<string, string> Parameters
        {
            get => parameters;
            set => SetProperty(ref parameters, value);
        }
    }
}