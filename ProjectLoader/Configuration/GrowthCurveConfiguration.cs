using System.Collections.Generic;
using System.Collections.ObjectModel;
using Recliner2GCBM.ViewModel.Support;

namespace Recliner2GCBM.Configuration
{
    public class GrowthCurveConfiguration : BindableBase
    {
        private string path;
        private bool header = true;
        private int page;
        private int incrementStartCol;
        private int incrementEndCol;
        private int speciesCol;
        private int interval;

        public GrowthCurveConfiguration()
        {
            Classifiers = new ObservableCollection<ClassifierReference>();
            interval = 5;
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

        public int SpeciesCol
        {
            get => speciesCol;
            set => SetProperty(ref speciesCol, value);
        }

        public int IncrementStartCol
        {
            get => incrementStartCol;
            set => SetProperty(ref incrementStartCol, value);
        }

        public int IncrementEndCol
        {
            get => incrementEndCol;
            set => SetProperty(ref incrementEndCol, value);
        }

        public int Interval
        {
            get => interval;
            set => SetProperty(ref interval, value);
        }

        public ObservableCollection<ClassifierReference> Classifiers
        {
            get;
            private set;
        }

        public void RefreshClassifiers(IEnumerable<string> classifierNames)
        {
            Classifiers.Clear();
            foreach (var classifier in classifierNames)
            {
                Classifiers.Add(new ClassifierReference(classifier));
            }
        }
    }
}
