using System;
using System.Windows.Input;

namespace D2DAS.Mvvm
{
    // https://www.markwithall.com/programming/2013/03/01/worlds-simplest-csharp-wpf-mvvm-example.html

    public class DelegateCommand : ICommand
    {

        public DelegateCommand(Action action, Func<bool> canExecuteFunc = null)
        {
            _action = action;
            _canExecuteFunc = canExecuteFunc;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecuteFunc == null)
			{
                return true;
            }

            return _canExecuteFunc();
        }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        private readonly Action _action;
        private readonly Func<bool> _canExecuteFunc;
    }
}
