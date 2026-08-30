# 项目进度记录

## 当前状态

**最后更新：** 2026-08-30 +08:00
**当前功能：** feat-006 CardGame Data Foundation 进行中（代码与样例已写完，Unity 编译验证待跑）。
**状态：** feat-003 转 blocked（Lifecycle/Boot 简化待拍板，09 开放问题 #8）；feat-006 建立 CardGame.Domain（纯 C#）与 CardGame.Runtime（Unity）两程序集，配置表协议落地。

## 本次完成：feat-006 配置表协议（2026-08-30）

- `Assets/CardGame/Runtime/CardGame.Domain/`：纯 C# 数据模型（CardDef/EffectEntry/RelicDef/EnemyDef/StatusDef/DialogueSequence/EventDef/WorldDef/UiStrings 等）、字符串枚举映射（EnumMaps）、DTO 映射（DtoMapper，聚合全部错误）、结构化校验（ContentValidator + DataValidationException，仿 DI V2 错误风格）、卡牌描述/意图预览自动生成（DescriptionBuilder，含 7 个暂定动作模板）、Id 索引仓库（DataRepository/DataRepositoryBuilder，构建后冻结）。
- `Assets/CardGame/Runtime/CardGame.Runtime/`：JsonUtility 序列化接缝、GameDataLoader（8 个固定文件名管线）、GameDataCatalog（MonoBehaviour 引导入口，留 Addressables 换装点）。
- `Assets/CardGame/Content/Data/`：8 个样例 JSON（cards/relics/enemies/status/events/dialogues/world/ui-strings），全部按 CONTRACT 示例行，交叉引用可解析。
- `Assets/CardGame/Tests/EditMode/Data/`：7 个测试文件 + 工厂（枚举/映射/序列化/校验 14 规则/文案/加载/样例自证），测试 asmdef 已接线两程序集。
- 文档同步：CONTRACT.md 协议落地节 + cardClass 键名注 + ui-strings 拍平形状注 + 暂定模板表 + 已知缺口清单；07-cardgame 结构树；09 开放问题 #2/#4/#5 定案；design README 程序集图。

## 验证证据

| 检查 | 命令 | 实际结果 |
|---|---|---|
| Unity EditMode（batchmode） | Unity.exe -batchmode -runTests | **未运行**：失败于 "another Unity instance is running with this project open"（编辑器占用项目锁），不构成验证证据 |
| 便携 Harness | `node scripts/harness/verify.mjs` | 未运行：本机无 Node |
| git diff --check | — | 干净 |

## 已知限制与后续边界

- 全部新增资产（asmdef/脚本/JSON）的 .meta 尚未生成：编辑器刷新/下次打开后生成，需提交。
- IL2CPP/AOT 未验证；玩法本体未实现；Excel → JSON 转换器未开始（协议先行）。

## 下一步

1. 关闭 Unity 编辑器后跑 batchmode EditMode（或用 Test Runner UI 手动跑），修复可能的编译问题，提交生成的 .meta。
2. 证据齐后关闭 feat-006，更新 feature_list。
3. 下一个功能候选：Excel/Markdown → JSON 转换器（09 开放问题 #3 落点待拍板）。
