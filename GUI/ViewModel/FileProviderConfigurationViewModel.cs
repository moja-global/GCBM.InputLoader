using Microsoft.Win32;
using Recliner2GCBM.ViewModel.Support;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Recliner2GCBM.ViewModel
{
    public class FileProviderConfigurationViewModel : INotifyPropertyChanged
    {
        private ApplicationContext applicationContext;

        public FileProviderConfigurationViewModel(ApplicationContext applicationContext)
        {
            this.applicationContext = applicationContext;
            applicationContext.PropertyChanged += OnProjectConfigurationChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string OutputPath
        {
            get => GetProviderParameter("path");
            set => SetProviderParameter("path", value);
        }

        public ICommand BrowseForOutputPathCommand => new DelegateCommand(BrowseForOutputPath);

        private string GetProviderParameter(string name)
        {
            var providerParameters = applicationContext.ProjectConfiguration.OutputConfiguration.Parameters;
            return providerParameters.ContainsKey(name) ? providerParameters[name] : String.Empty;
        }

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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

        private void OnProjectConfigurationChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ProjectConfiguration")
            {
                OnPropertyChanged("OutputPath");
            }
        }

        private void BrowseForOutputPath()
        {
            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "db",
                Filter = "SQLite databases (*.db)|*.db|All files (*.*)|*.*"
            };

            if (!String.IsNullOrWhiteSpace(OutputPath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(OutputPath);
            }

            if (dialog.ShowDialog() == true)
            {
                OutputPath = dialog.FileName;
            }
        }
    }
}
