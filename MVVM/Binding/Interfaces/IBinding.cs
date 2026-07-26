using System;

namespace RazorFramework.MVVM
{
    /// <summary>数据绑定接口</summary>
    public interface IBinding
    {
        void Bind();
        void UnBind();
    }

    /// <summary>绑定模式</summary>
    public enum BindingMode
    {
        OneWay,        // ViewModel → View
        TwoWay,        // ViewModel ↔ View
        OneWayToSource // View → ViewModel
    }

    /// <summary>值转换器接口</summary>
    public interface IValueConverter
    {
        object Convert(object value, Type targetType, object parameter);
        object ConvertBack(object value, Type targetType, object parameter);
    }

    /// <summary>绑定管理器接口</summary>
    public interface IBindingManager
    {
        void RegisterBinding(IBinding binding, object context = null);
        void UnregisterBinding(IBinding binding, object context = null);
        void BindAllInContext(object context);
        void UnbindAllInContext(object context);
        void BindAll();
        void UnbindAll();
    }
}
