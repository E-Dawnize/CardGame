using System;
using CardGame.Domain;
using RazorFramework.DI;

namespace CardGame.Runtime
{
    /// <summary>
    /// Composition root（组合根）：应用知道全部具体类型、把它们装配在一起的那一处。
    /// 职责：作用域定义 + 代码注册 + 显式启动（Build → Resolve → Start 的屏障语义）。
    /// 注册错误在 Build() 构建期全量暴露（依赖图校验），不在启动后的运行期。
    /// 未来扩展点：伤害管线服务等遭遇级服务的注册位（决策规格 §1.1，届时追加 AddScoped 注册）。
    /// </summary>
    public sealed class GameComposition : IDisposable
    {
        public ServiceContainer Container { get; }

        public GameFlow Flow { get; }

        public GameComposition(DataRepository data)
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunScope>();
            builder.DefineScope<EncounterScope, RunScope>();
            builder.AddSingleton(data);       // 外部实例：仓库归组合根所有，容器不 Dispose
            builder.AddSingleton<GameFlow>(); // 构造注入 DataRepository
            Container = builder.Build();
            Flow = Container.Resolve<GameFlow>();
        }

        /// <summary>显式启动（屏障语义）：全部服务构造完毕后才开始交互。</summary>
        public void StartFlow() => Flow.Start();

        public void Dispose() => Container.Dispose();
    }
}
