using Recliner2GCBM.ViewModel.Support;

namespace Recliner2GCBM.Configuration
{
    public class ClassifierConfiguration : BindableBase
    {
        private string name;
        private string path;
        private int page;
        private int columnn;
        private bool header = true;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
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

        public int Column
        {
            get => columnn;
            set => SetProperty(ref columnn, value);
        }

        public bool Header
        {
            get => header;
            set => SetProperty(ref header, value);
        }

        public override string ToString() => name;
    }
}
