using System.Windows;
using Recliner2GCBM.Configuration;
using Recliner2GCBM.ViewModel;
using Recliner2GCBM.ViewModel.Support;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Recliner2GCBM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var applicationContext = new ApplicationContext();
            var providerFactory = new ProviderTypeFactory();
            var loaderFactory = new DataLoaderFactory(providerFactory);

            Resources["MainWindowViewModel"] = new MainWindowViewModel(applicationContext, providerFactory);
            Resources["ClassifierTabViewModel"] = new ClassifierTabViewModel(applicationContext);
            Resources["GrowthCurveTabViewModel"] = new GrowthCurveTabViewModel(applicationContext);
            Resources["TransitionRuleTabViewModel"] = new TransitionRuleTabViewModel(applicationContext);
            Resources["FinishTabViewModel"] = new FinishTabViewModel(applicationContext, loaderFactory);
            Resources["FileProviderConfigurationViewModel"] = new FileProviderConfigurationViewModel(applicationContext);
            Resources["ServerProviderConfigurationViewModel"] = new ServerProviderConfigurationViewModel(applicationContext);
            Resources["DisturbanceCategoryTabViewModel"] = new DisturbanceCategoryTabViewModel(applicationContext);
        }
    }
}
