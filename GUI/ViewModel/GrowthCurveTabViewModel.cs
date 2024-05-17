using System;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;
using Recliner2GCBM.ViewModel.Support;
using unvell.ReoGrid;

namespace Recliner2GCBM.ViewModel
{
    public class GrowthCurveTabViewModel : BindableBase
    {
        private bool ready;

        public GrowthCurveTabViewModel(ApplicationContext applicationContext)
        {
            AppContext = applicationContext;
            AppContext.PropertyChanged += OnContextChanged;
            Ready = true;
        }

        public ApplicationContext AppContext { get; private set; }
        public ReoGridControl InputDataView { get; set; }

        public bool Ready
        {
            get => ready;
            set => SetProperty(ref ready, value);
        }

        public ICommand BackCommand => new DelegateCommand(Back);
        public ICommand NextCommand => new DelegateCommand(Next);
        public ICommand SelectIncrementColumnStartCommand => new DelegateCommand(SelectIncrementColumnStart);
        public ICommand SelectIncrementColumnEndCommand => new DelegateCommand(SelectIncrementColumnEnd);
        public ICommand SelectClassifierColumnCommand => new DelegateCommand(SelectClassifierColumn);
        public ICommand SelectSpeciesColumnCommand => new DelegateCommand(SelectSpeciesColumn);
        public ICommand BrowseForGrowthCurveFileCommand => new DelegateCommand(BrowseForGrowthCurveFile);

        private void OnContextChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedTabName")
            {
                if (AppContext.SelectedTabName == "GrowthCurve")
                {
                    RefreshInputDataView();
                }
                else
                {
                    GridHelper.Dispose(InputDataView);
                }
            }
        }

        private void RefreshInputDataView()
        {
            GridHelper.LoadFile(InputDataView,
                                AppContext.ProjectConfiguration.GrowthCurves.Path);
        }

        private void Back() => AppContext.SelectedTabIndex--;
        private void Next() => AppContext.SelectedTabIndex++;

        private void SelectConfigurationColumn(Action<RangePosition> SetConfiguration)
        {
            Ready = false;
            InputDataView.PickRange((inst, range) =>
            {
                AppContext.ProjectConfiguration.TransitionRules.Page =
                    inst.Workbook.GetWorksheetIndex(inst);

                SetConfiguration(range);
                Ready = true;
                return true;
            });
        }

        private void SelectIncrementColumnStart()
        {
            SelectConfigurationColumn((range) => {
                AppContext.ProjectConfiguration.GrowthCurves.IncrementStartCol = range.Col;
            });
        }

        private void SelectIncrementColumnEnd()
        {
            SelectConfigurationColumn((range) => {
                AppContext.ProjectConfiguration.GrowthCurves.IncrementEndCol = range.Col;
            });
        }

        private void SelectClassifierColumn(object parameter)
        {
            var classifierName = parameter as string;
            SelectConfigurationColumn((range) => {
                foreach (var classifier in AppContext.ProjectConfiguration.GrowthCurves.Classifiers)
                {
                    if (classifier.Name == classifierName)
                    {
                        classifier.Column = range.Col;
                        break;
                    }
                }
            });
        }

        private void SelectSpeciesColumn()
        {
            SelectConfigurationColumn((range) => {
                AppContext.ProjectConfiguration.GrowthCurves.SpeciesCol = range.Col;
            });
        }

        private void BrowseForGrowthCurveFile()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                AppContext.ProjectConfiguration.GrowthCurves.Path = dialog.FileName;
                RefreshInputDataView();
            }
        }
    }
}
