using Recliner2GCBM.ViewModel.Support;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Recliner2GCBM.ViewModel
{
    public class ServerProviderConfigurationViewModel : INotifyPropertyChanged
    {
        private ApplicationContext applicationContext;

        public ServerProviderConfigurationViewModel(ApplicationContext applicationContext)
        {
            this.applicationContext = applicationContext;
            applicationContext.PropertyChanged += OnProjectConfigurationChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Host
        {
            get => GetProviderParameter("host");
            set => SetProviderParameter("host", value);
        }

        public string UserName
        {
            get => GetProviderParameter("username");
            set => SetProviderParameter("username", value);
        }

        public string Password
        {
            get => GetProviderParameter("password");
            set => SetProviderParameter("password", value);
        }

        public string Database
        {
            get => GetProviderParameter("database");
            set => SetProviderParameter("database", value);
        }

        public string Schema
        {
            get => GetProviderParameter("schema");
            set => SetProviderParameter("schema", value);
        }

        private string GetProviderParameter(string name)
        {
            var providerParameters = applicationContext.ProjectConfiguration.OutputConfiguration.Parameters;
            return providerParameters.ContainsKey(name) ? providerParameters[name] : String.Empty;
        }

        private void SetProviderParameter(string configName, string value, [CallerMemberName] string name = null)
        {
            var providerParameters = applicationContext.ProjectConfiguration.OutputConfiguration.Parameters;
            if (!providerParameters.ContainsKey(configName))
            {
                providerParameters[configName] = String.Empty;
            }

            if (!Equals(providerParameters[configName], value))
            {
                providerParameters[configName] = value;
                OnPropertyChanged(name);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void OnProjectConfigurationChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ProjectConfiguration")
            {
                foreach (var boundProperty in new string[] { "Host", "UserName", "Password", "Database", "Schema" })
                {
                    OnPropertyChanged(boundProperty);
                }
            }
        }
    }
}
