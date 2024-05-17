using Microsoft.Win32;
using Recliner2GCBM.Configuration;
using Recliner2GCBM.ViewModel.Support;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Recliner2GCBM.ViewModel
{
    public class MainWindowViewModel : BindableBase
    {
        private ProviderTypeFactory providerFactory;
        private int outputProviderView;

        public MainWindowViewModel(ApplicationContext applicationContext, ProviderTypeFactory providerFactory)
        {
            AppContext = applicationContext;
            this.providerFactory = providerFactory;
            AppContext.ProjectConfiguration.OutputConfiguration.PropertyChanged += OnProviderConfigurationChanged;

            ProjectModes = new Dictionary<ProjectMode, string>()
            {
                { ProjectMode.SpatiallyExplicit, "Spatially Explicit" },
                { ProjectMode.SpatiallyReferenced, "Spatially Referenced" }
            };

            ModuleConfigurations = new Dictionary<ModuleConfiguration, string>()
            {
                { ModuleConfiguration.CBMClassic, "CBM Classic" },
                { ModuleConfiguration.CBMNoGrowthCurves, "CBM No Growth Curve Tables" }
            };
        }

        public ApplicationContext AppContext { get; private set; }
        public IDictionary<ProjectMode, string> ProjectModes { get; private set; }
        public IDictionary<ModuleConfiguration, string> ModuleConfigurations { get; private set; }
        public IEnumerable<string> OutputProviderTypes => providerFactory.ProviderNames;

        public int OutputProviderView
        {
            get => outputProviderView;
            set => SetProperty(ref outputProviderView, value);
        }

        public ICommand BrowseForAIDBCommand => new DelegateCommand(BrowseForAIDB);
        public ICommand NextCommand => new DelegateCommand(Next);
        public ICommand BackCommand => new DelegateCommand(Back);
        public ICommand LoadCommand => new DelegateCommand(Load);

        private void OnProviderConfigurationChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                var providerName = AppContext.ProjectConfiguration.OutputConfiguration.Name;
                var providerType = providerFactory.GetByName(providerName);
                OutputProviderView = providerType.IsFileDb ? 0 : 1;

                // Remove any current configuration items which are not relevant to the
                // selected provider type.
                var outputProviderConfig = AppContext.ProjectConfiguration.OutputConfiguration.Parameters;
                foreach (var configParameter in outputProviderConfig.Keys.ToList())
                {
                    if (!providerType.ConnectionParameters.Contains(configParameter))
                    {
                        outputProviderConfig.Remove(configParameter);
                    }
                }

                AppContext.NotifyProjectConfigurationChanged();
            }
        }

        private void BrowseForAIDB()
        {
            var dialog = new OpenFileDialog();
            if (!String.IsNullOrWhiteSpace(AppContext.ProjectConfiguration.AIDBPath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(
                    AppContext.ProjectConfiguration.AIDBPath);
            }

            if (dialog.ShowDialog() == true)
            {
                AppContext.ProjectConfiguration.AIDBPath = dialog.FileName;
            }
        }

        private void Load()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                var s = new ProjectConfigurationSerializer();
                AppContext.ProjectConfiguration.OutputConfiguration.PropertyChanged -= OnProviderConfigurationChanged;
                AppContext.ProjectConfiguration = s.Load(dialog.FileName);
                AppContext.ProjectConfiguration.OutputConfiguration.PropertyChanged += OnProviderConfigurationChanged;
                OnProviderConfigurationChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }

        private void Next() => AppContext.SelectedTabIndex++;
        private void Back() => AppContext.SelectedTabIndex--;
    }
}
