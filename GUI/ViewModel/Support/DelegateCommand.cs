using System;
using System.Windows.Input;

namespace Recliner2GCBM.ViewModel.Support
{
    class DelegateCommand : ICommand
    {
        private readonly Action action;
        private readonly Action<object> parameterizedAction;
        private readonly Func<bool> canExecute;

        public DelegateCommand(Action action, Func<bool> canExecute = null)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public DelegateCommand(Action<object> parameterizedAction,
                               Func<bool> canExecute = null)
        {
            this.parameterizedAction = parameterizedAction;
            this.canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            if (action != null)
            {
                action();
            }
            else
            {
                parameterizedAction?.Invoke(parameter);
            }
        }

        public bool CanExecute(object parameter)
        {
            if (canExecute != null)
            {
                return canExecute();
            }

            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
