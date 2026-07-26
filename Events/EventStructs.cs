// === RazorFramework Event Definitions Template ===
// 在此文件中定义项目级事件结构体。所有事件必须是 struct。
//
// 示例:
//   struct GameReadyEvent { }
//   struct ItemCollectedEvent { public string ItemId; }
//
// 使用方式:
//   发布: _eventCenter.Publish(new GameReadyEvent());
//   订阅: _eventCenter.Subscribe<GameReadyEvent>(OnGameReady);
//   取消: _eventCenter.Unsubscribe<GameReadyEvent>(OnGameReady);
//
// 规则:
//   - 事件必须是 struct（值类型，避免 GC 分配）
//   - 事件命名以 "Event" 结尾
//   - 同一事件被多个系统订阅时，发布顺序不确定
//   - 在 OnShutdown() 或 Dispose() 中取消订阅，防止泄漏

namespace RazorFramework.Events
{
    // TODO: 在此定义项目事件
}
