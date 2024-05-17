using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using log4net;
using Microsoft.Win32;
using Recliner2GCBM.Configuration;
using Recliner2GCBM.ViewModel.Support;

namespace Recliner2GCBM.ViewModel
{
    public class FinishTabViewModel
    {
        private DataLoaderFactory loaderFactory;
        private BackgroundWorker loaderWorker = new BackgroundWorker();
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public FinishTabViewModel(ApplicationContext applicationContext, DataLoaderFactory loaderFactory)
        {
            Status = new LoaderStatus();
            Status.PropertyChanged += OnStatusChanged;
            AppContext = applicationContext;
            this.loaderFactory = loaderFactory;
            loaderWorker.WorkerReportsProgress = true;
            loaderWorker.DoWork += RunLoader;
            loaderWorker.ProgressChanged += OnLoaderProgressChanged;
            loaderWorker.RunWorkerCompleted += OnLoaderCompleted;
            SaveRelativePaths = true;
        }

        public ApplicationContext AppContext { get; private set; }
        public LoaderStatus Status { get; set; }
        public bool SaveRelativePaths { get; set; }

        public ICommand RunLoaderCommand => new DelegateCommand(Run);
        public ICommand SaveCommand => new DelegateCommand(Save);
        public ICommand BackCommand => new DelegateCommand(Back);
        public ICommand ExitCommand => new DelegateCommand(Exit);

        private void RunLoader(object sender, DoWorkEventArgs e)
        {
            var loader = loaderFactory.GetLoader(AppContext.ProjectConfiguration);
            Status.LoaderCount = loader.Count();
            int loaderNum = 1;
            foreach (var name in loader.Load(AppContext.ProjectConfiguration.OutputConfiguration))
            {
                loaderWorker.ReportProgress(loaderNum++, name);
            }
        }

        private void OnStatusChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Text")
            {
                log.Info(Status.Text);
            }
            else if (e.PropertyName == "Error")
            {
                log.Fatal(Status.Error);
            }
        }

        private void OnLoaderProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Status.CurrentLoaderNumber = e.ProgressPercentage;
            Status.Text = $"Loading {e.ProgressPercentage} of {Status.LoaderCount}: {e.UserState}.";
        }

        private void OnLoaderCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Status.Text = "Finished.";
            }
            else
            {
                Status.Text = $"ERROR: {Status.Text}";
                Status.Error = e.Error.Message;
                Status.CurrentLoaderNumber = 0;
            }

            Status.Ready = true;
        }

        private void Save()
        {
            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = "json";
            dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            var outputPath = (from param in AppContext.ProjectConfiguration.OutputConfiguration.Parameters
                              where param.Key == "path"
                              select param.Value).FirstOrDefault();

            if (!String.IsNullOrWhiteSpace(outputPath))
            {
                dialog.InitialDirectory = Path.GetFullPath(Path.GetDirectoryName(outputPath));
            }
            
            if (dialog.ShowDialog() == true)
            {
                var s = new ProjectConfigurationSerializer(SaveRelativePaths);
                s.Save(dialog.FileName, AppContext.ProjectConfiguration);
            }
        }

        private void Run()
        {
            Status.Text = $"Creating GCBM input database";
            Status.Error = "";
            Status.CurrentLoaderNumber = 0;
            Status.Ready = false;
            loaderWorker.RunWorkerAsync();
        }

        private void Back() => AppContext.SelectedTabIndex--;
        private void Exit() => Application.Current.Shutdown();
    }
}
