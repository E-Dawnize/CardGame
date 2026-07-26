using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RazorFramework.MVVM
{
    /// <summary>异步无参数 Command</summary>
    public class AsyncCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Func<Task> _executeAsync;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;

        public AsyncCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true;
            RaiseCanExecuteChanged();
            try { await _executeAsync(); }
            finally { _isExecuting = false; RaiseCanExecuteChanged(); }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>异步泛型 Command</summary>
    public class AsyncCommand<T> : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Func<T, Task> _executeAsync;
        private readonly Func<T, bool> _canExecute;
        private bool _isExecuting;

        public AsyncCommand(Func<T, Task> executeAsync, Func<T, bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => !_isExecuting && (_canExecute?.Invoke(parameter is T t ? t : default) ?? true);

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true;
            RaiseCanExecuteChanged();
            try { await _executeAsync(parameter is T t ? t : default); }
            finally { _isExecuting = false; RaiseCanExecuteChanged(); }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
