using System;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;
using Recliner2GCBM.Configuration;
using Recliner2GCBM.ViewModel.Support;
using unvell.ReoGrid;

namespace Recliner2GCBM.ViewModel
{
    public class AddClassifierDialogViewModel : BindableBase, IDisposable
    {
        private ReoGridControl inputDataView;
        private bool ready;

        public AddClassifierDialogViewModel(ClassifierConfiguration classifier,
                                            ReoGridControl inputDataView)
        {
            this.inputDataView = inputDataView;
            Classifier = classifier;
            Classifier.PropertyChanged += OnConfigurationChanged;
            Ready = true;
        }

        public ClassifierConfiguration Classifier { get; private set; }

        public bool Ready
        {
            get => ready;
            set => SetProperty(ref ready, value);
        }

        public ICommand SelectFileCommand => new DelegateCommand(SelectFile);
        public ICommand SelectColumnCommand => new DelegateCommand(SelectColumn);

        private void OnConfigurationChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Path")
            {
                GridHelper.LoadFile(inputDataView, Classifier.Path);
            }
        }

        private void SelectFile()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                Classifier.Path = dialog.FileName;
            }
        }

        private void SelectColumn()
        {
            Ready = false;
            inputDataView.PickRange((inst, range) =>
            {
                Classifier.Page = inst.Workbook.GetWorksheetIndex(inst);
                Classifier.Column = range.Col;
                Ready = true;
                return true;
            });
        }

        public void Dispose()
        {
            Classifier.PropertyChanged -= OnConfigurationChanged;
            GridHelper.Dispose(inputDataView);
        }
    }
}
