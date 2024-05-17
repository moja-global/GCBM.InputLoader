using System;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;
using Recliner2GCBM.ViewModel.Support;
using unvell.ReoGrid;

namespace Recliner2GCBM.ViewModel
{
    public class TransitionRuleTabViewModel : BindableBase
    {
        private bool ready;

        public TransitionRuleTabViewModel(ApplicationContext applicationContext)
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

        public ICommand SelectClassifierColumnCommand => new DelegateCommand(SelectClassifierColumn);
        public ICommand SelectRuleClassifierColumnCommand => new DelegateCommand(SelectRuleClassifierColumn);
        public ICommand SelectIDColumnCommand => new DelegateCommand(SelectIDColumn);
        public ICommand SelectAgeColumnCommand => new DelegateCommand(SelectAgeColumn);
        public ICommand SelectDelayColumnCommand => new DelegateCommand(SelectDelayColumn);
        public ICommand SelectTypeColumnCommand => new DelegateCommand(SelectTypeColumn);
        public ICommand SelectDisturbanceTypeColumnCommand => new DelegateCommand(SelectDisturbanceTypeColumn);
        public ICommand BrowseForTransitionRuleFileCommand => new DelegateCommand(BrowseForTransitionRuleFile);

        private void OnContextChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedTabName")
            {
                if (AppContext.SelectedTabName == "TransitionRule")
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
                                AppContext.ProjectConfiguration.TransitionRules.Path);
        }

        private void Back() => AppContext.SelectedTabIndex--;
        private void Next() => AppContext.SelectedTabIndex++;

        private void SelectConfigurationColumn(Action<int> SetConfiguration)
        {
            Ready = false;
            InputDataView.PickRange((inst, range) =>
            {
                AppContext.ProjectConfiguration.TransitionRules.Page =
                    inst.Workbook.GetWorksheetIndex(inst);

                SetConfiguration(range.Col);
                Ready = true;
                return true;
            });
        }

        private void SelectClassifierColumn(object parameter)
        {
            var classifierName = parameter as string;
            SelectConfigurationColumn((selectedCol) => {
                foreach (var classifier in AppContext.ProjectConfiguration.TransitionRules.Classifiers)
                {
                    if (classifier.Name == classifierName)
                    {
                        classifier.Column = selectedCol;
                        break;
                    }
                }
            });
        }

        private void SelectRuleClassifierColumn(object parameter)
        {
            var classifierName = parameter as string;
            SelectConfigurationColumn((selectedCol) => {
                foreach (var classifier in AppContext.ProjectConfiguration.TransitionRules.RuleClassifiers)
                {
                    if (classifier.Name == classifierName)
                    {
                        classifier.Column = selectedCol;
                        break;
                    }
                }
            });
        }

        private void SelectIDColumn()
        {
            SelectConfigurationColumn((selectedCol) => {
                AppContext.ProjectConfiguration.TransitionRules.NameCol = selectedCol;
            });
        }

        private void SelectAgeColumn()
        {
            SelectConfigurationColumn((selectedCol) => {
                AppContext.ProjectConfiguration.TransitionRules.AgeCol = selectedCol;
            });
        }

        private void SelectDelayColumn()
        {
            SelectConfigurationColumn((selectedCol) => {
                AppContext.ProjectConfiguration.TransitionRules.DelayCol = selectedCol;
            });
        }

        private void SelectTypeColumn()
        {
            SelectConfigurationColumn((selectedCol) => {
                AppContext.ProjectConfiguration.TransitionRules.TypeCol = selectedCol;
            });
        }

        private void SelectDisturbanceTypeColumn()
        {
            SelectConfigurationColumn((selectedCol) => {
                AppContext.ProjectConfiguration.TransitionRules.RuleDisturbanceTypeCol = selectedCol;
            });
        }

        private void BrowseForTransitionRuleFile()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                AppContext.ProjectConfiguration.TransitionRules.Path = dialog.FileName;
                RefreshInputDataView();
            }
        }
    }
}
