using System;
using System.ComponentModel;
using Recliner2GCBM.Configuration;

namespace Recliner2GCBM.ViewModel.Support
{
    public class ApplicationContext : BindableBase
    {
        private int selectedTabIndex;
        private string selectedTabName;
        private ProjectConfiguration projectConfiguration;

        public ApplicationContext() => ProjectConfiguration = new ProjectConfiguration();

        public ProjectConfiguration ProjectConfiguration
        {
            get => projectConfiguration;
            set => SetProperty(ref projectConfiguration, value);
        }

        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set => SetProperty(ref selectedTabIndex, value);
        }

        public string SelectedTabName
        {
            get => selectedTabName;
            set => SetProperty(ref selectedTabName, value);
        }

        public void NotifyProjectConfigurationChanged() => OnPropertyChanged("ProjectConfiguration");
    }
}
