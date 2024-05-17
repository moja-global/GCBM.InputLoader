using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Recliner2GCBM.ViewModel.Support
{
    public abstract class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<T>(ref T storage,
                                      T value,
                                      [CallerMemberName] string name = null)
        {
            if (!Equals(storage, value))
            {
                storage = value;
                OnPropertyChanged(name);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
