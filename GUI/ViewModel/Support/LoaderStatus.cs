namespace Recliner2GCBM.ViewModel.Support
{
    public class LoaderStatus : BindableBase
    {
        private int loaderCount = 1;
        private int currentLoaderNumber = 0;
        private string text = "Ready.";
        private string error = "";
        private bool ready = true;

        public int LoaderCount
        {
            get => loaderCount;
            set => SetProperty(ref loaderCount, value);
        }

        public int CurrentLoaderNumber
        {
            get => currentLoaderNumber;
            set => SetProperty(ref currentLoaderNumber, value);
        }

        public string Text
        {
            get => text;
            set => SetProperty(ref text, value);
        }

        public string Error
        {
            get => error;
            set => SetProperty(ref error, value);
        }

        public bool Ready
        {
            get => ready;
            set => SetProperty(ref ready, value);
        }
    }
}
