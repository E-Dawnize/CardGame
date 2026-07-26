using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using RazorFramework.DI;
using RazorFramework.Events;

namespace RazorFramework.MVVM
{
    /// <summary>
    /// MVVM ViewModel 基类 — 纯 C#，无 UnityEngine 依赖。
    /// 提供 SetProperty、Command 工厂、事件总线引用。
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>事件总线 — 由 DI 注入，供子类发布/订阅事件</summary>
        [Inject] protected IEventCenter EventCenter;

        private readonly Dictionary<string, ICommand> _commands = new();

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>注册命名 Command，供 View 通过名称获取</summary>
        protected void RegisterCommand(string name, ICommand command)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Command name cannot be null or empty", nameof(name));
            _commands[name] = command;
        }

        protected void UnregisterCommand(string name) => _commands.Remove(name);

        protected ICommand GetCommand(string name)
            => _commands.TryGetValue(name, out var cmd) ? cmd : null;

        protected ICommand CreateCommand(Action execute, Func<bool> canExecute = null)
            => new RelayCommand(execute, _ => canExecute?.Invoke() ?? true);

        protected ICommand CreateCommand<T>(Action<T> execute, Func<T, bool> canExecute = null)
            => new RelayCommand<T>(execute ?? throw new ArgumentNullException(nameof(execute)), canExecute);

        protected ICommand CreateAsyncCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
            => new AsyncCommand(executeAsync, canExecute);

        protected ICommand CreateAsyncCommand<T>(Func<T, Task> executeAsync, Func<T, bool> canExecute = null)
            => new AsyncCommand<T>(executeAsync, canExecute);

        /// <summary>设置属性值并在变化时触发 PropertyChanged。返回是否实际变更。</summary>
        public virtual bool SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(property, value)) return false;
            property = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public abstract void Initialize();
        public virtual void Dispose() { }
    }
}
