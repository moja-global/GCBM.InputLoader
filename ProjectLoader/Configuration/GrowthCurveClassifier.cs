using Recliner2GCBM.ViewModel.Support;

namespace Recliner2GCBM.Configuration
{
    public class ClassifierReference : BindableBase
    {
        private string name;
        private int? column;

        public ClassifierReference(string name) => this.name = name;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public int? Column
        {
            get => column;
            set => SetProperty(ref column, value);
        }
    }
}
