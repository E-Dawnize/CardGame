# MVVM — ViewModel 与绑定

> **状态：🚫 已退役（2026-08-30）** —— 框架级 MVVM 不实施；本文件转为历史存档
> 退役评估见 [09-ui-resources.md](09-ui-resources.md)「MVVM 必要性评估」：
> UITK 官方 runtime binding 替代自研绑定；战斗层走代码视图；个人项目无框架复用包袱
> 处置：根目录旧 `MVVM/` 源码已于 2026-09-02 删除；`RazorFramework.MVVM` 与
> `RazorFramework.Unity.MVVM` 程序集不再建立；面板层数据源所需的 INPC 基类
> （约 30 行，SetProperty 值变才通知）按需写入 CardGame.Runtime（见下方「保留模式」）
>
> 以下为退役前的历史内容（旧实现与已废弃目标形态），仅存档不再维护。

## 保留模式：INPC 基类（留待后续按需使用）

旧 `ViewModelBase.SetProperty` 是唯一值得复用的模式（值变才通知，约 30 行）。
根目录源码已随退役删除，此模式**留待后续按需使用**：当面板/UI 层需要数据源变更通知时，
在 `CardGame.Runtime` 内按需新建一个干净基类——去掉 `[Inject]` 字段注入、EventCenter
引用与命名 Command 注册表，核心形态如下：

```csharp
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

- 不建 `RazorFramework.MVVM` 程序集；Commands/Binding 不迁移（已被 UITK runtime binding 与战斗层代码视图替代）。
- 需要 `INotifyPropertyChanged` 时按上面形态在 `CardGame.Runtime` 内实现，不要回头恢复旧源码。

## 旧实现（迁移输入）

```text
根目录 MVVM/（非编译域）
├─ ViewModelBase : INotifyPropertyChanged, IDisposable
│  ├─ [Inject] IEventCenter 字段      旧字段注入模式（目标：构造注入）
│  ├─ SetProperty<T>                  值变才通知（返回是否变更）
│  ├─ 命名 Command 注册表              Register/Unregister/GetCommand(name)
│  ├─ Command 工厂                     Relay / Async 各含无参与泛型
│  └─ abstract Initialize() / virtual Dispose()
├─ Commands（System.Windows.Input.ICommand，.NET Standard 2.1）
│  ├─ RelayCommand / RelayCommand<T>   CanExecute 委托 + RaiseCanExecuteChanged
│  └─ AsyncCommand / AsyncCommand<T>   _isExecuting 防重入；执行前后 RaiseCanExecuteChanged
├─ Binding 接口
│  ├─ IBinding（Bind / UnBind）
│  ├─ BindingMode（OneWay / TwoWay / OneWayToSource）
│  ├─ IValueConverter（Convert / ConvertBack）
│  └─ IBindingManager
└─ BindingManager
   ├─ 三索引：全量列表 + context（ViewModel）索引 + GameObject 索引（锁保护）
   ├─ 场景级操作：BindAllInContext / UnbindAllInContext / BindAll / UnbindAll
   ├─ 绑定失败隔离（记 Debug.LogError，不抛）
   └─ CleanupDestroyedGameObjects 清理已销毁对象索引
```

## 目标形态（feat-003 D3）

```text
RazorFramework.MVVM（纯 C#，refs Events）
├─ ViewModelBase   构造注入 IEventCenter（去掉 [Inject] 字段与 DI 引用）
├─ Commands + IBinding / BindingMode / IValueConverter 迁入
└─ 无 UnityEngine 依赖

RazorFramework.Unity.MVVM
└─ BindingManager   Unity 相关实现（GameObject 索引、场景清理）
```

## 关键决策与不变量

- 命令防重入：`AsyncCommand` 执行期间 `CanExecute` 恒 false。
- 命名 Command 表供 View 按名取用（View/VM 以字符串名耦合的弱约定）。
- 绑定按 context（ViewModel）聚合，支持整上下文绑定/解绑。

## 已知限制

- 旧 `ViewModelBase` 依赖 `[Inject]` 字段，与 DI V1 耦合；迁移时改构造注入（D3）。
- 无属性路径绑定（PropertyPath），仅单属性通知模型。
- `System.Windows.Input.ICommand` 依赖 .NET Standard 2.1 profile。
