using System.Collections.Generic;
using System.Collections.ObjectModel;
using Recliner2GCBM.ViewModel.Support;

namespace Recliner2GCBM.Configuration
{
    public class TransitionRuleConfiguration : BindableBase
    {
        private string path;
        private bool header = true;
        private int page;
        private int nameCol;
        private int ageCol;
        private int delayCol;
        private int? typeCol;
        private int? ruleDisturbanceTypeCol;

        public TransitionRuleConfiguration()
        {
            Classifiers = new ObservableCollection<ClassifierReference>();
            RuleClassifiers = new ObservableCollection<ClassifierReference>();
        }

        public string Path
        {
            get => path;
            set => SetProperty(ref path, value);
        }

        public int Page
        {
            get => page;
            set => SetProperty(ref page, value);
        }

        public bool Header
        {
            get => header;
            set => SetProperty(ref header, value);
        }

        public int NameCol
        {
            get => nameCol;
            set => SetProperty(ref nameCol, value);
        }

        public int AgeCol
        {
            get => ageCol;
            set => SetProperty(ref ageCol, value);
        }

        public int DelayCol
        {
            get => delayCol;
            set => SetProperty(ref delayCol, value);
        }

        public int? TypeCol
        {
            get => typeCol;
            set => SetProperty(ref typeCol, value);
        }

        public int? RuleDisturbanceTypeCol
        {
            get => ruleDisturbanceTypeCol;
            set => SetProperty(ref ruleDisturbanceTypeCol, value);
        }

        public ObservableCollection<ClassifierReference> Classifiers
        {
            get;
            private set;
        }

        public ObservableCollection<ClassifierReference> RuleClassifiers
        {
            get;
            private set;
        }

        public void RefreshClassifiers(IEnumerable<string> classifierNames)
        {
            foreach (var refCollection in new []{ Classifiers, RuleClassifiers })
            {
                refCollection.Clear();
                foreach (var classifier in classifierNames)
                {
                    refCollection.Add(new ClassifierReference(classifier));
                }
            }
        }
    }
}
