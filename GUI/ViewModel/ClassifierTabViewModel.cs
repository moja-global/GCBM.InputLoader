using System.Windows.Input;
using Recliner2GCBM.Configuration;
using Recliner2GCBM.View;
using Recliner2GCBM.ViewModel.Support;

namespace Recliner2GCBM.ViewModel
{
    public class ClassifierTabViewModel
    {
        public ClassifierTabViewModel(ApplicationContext applicationContext)
        {
            AppContext = applicationContext;
        }

        public ApplicationContext AppContext { get; private set; }
        public ClassifierConfiguration SelectedClassifier { get; set; }

        public ICommand AddClassifierCommand => new DelegateCommand(AddClassifier);
        public ICommand RemoveClassifierCommand => new DelegateCommand(
            RemoveClassifier, RemoveClassifierCanExecute);

        public ICommand NextCommand => new DelegateCommand(Next);
        public ICommand BackCommand => new DelegateCommand(Back);

        private void AddClassifier()
        {
            var classifier = new ClassifierConfiguration();
            var dialog = new AddClassifierDialog(classifier);
            if (dialog.ShowDialog() == true)
            {
                AppContext.ProjectConfiguration.ClassifierSet.Add(classifier);
                AppContext.ProjectConfiguration.RefreshClassifiers();
            }
        }

        private void RemoveClassifier()
        {
            AppContext.ProjectConfiguration.ClassifierSet.Remove(SelectedClassifier);
            AppContext.ProjectConfiguration.RefreshClassifiers();
        }

        private bool RemoveClassifierCanExecute() => SelectedClassifier != null;

        private void Next() => AppContext.SelectedTabIndex++;
        private void Back() => AppContext.SelectedTabIndex--;
    }
}
