namespace CardGame.Runtime
{
    /// <summary>单局作用域标记：卡组 / 遗物 / 血量 / 金钱 / 地图进度等单局状态的锚定作用域（随开局创建、局终释放）。</summary>
    public sealed class RunScope { }

    /// <summary>遭遇作用域标记（RunScope 的子作用域）：战斗引擎 / 伤害管线 / 实体属性 / 意图调度等战斗状态的锚定作用域（进战斗创建、战斗结束释放）。</summary>
    public sealed class EncounterScope { }
}
