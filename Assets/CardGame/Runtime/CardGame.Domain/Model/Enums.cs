namespace CardGame.Domain
{
    /// <summary>卡牌类型。</summary>
    public enum CardType
    {
        Attack,
        Skill,
        Power
    }

    /// <summary>卡牌稀有度。</summary>
    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare
    }

    /// <summary>遗物稀有度（含 Boss，区别于卡牌稀有度）。</summary>
    public enum RelicRarity
    {
        Common,
        Uncommon,
        Rare,
        Boss
    }

    /// <summary>效果触发时机。</summary>
    public enum EffectTrigger
    {
        OnPlay,
        OnTurnStart,
        OnTurnEnd,
        OnDamageTaken,
        OnDamageDealt,
        OnKill,
        OnCardDrawn,
        OnCardDiscarded,
        OnBlockDepleted,
        OnHpBelow50Percent
    }

    /// <summary>效果目标。</summary>
    public enum EffectTarget
    {
        Self,
        SelectedEnemy,
        AllEnemies,
        RandomEnemy,
        All,
        HandPile,
        DrawPile,
        DiscardPile,
        RandomCardInHand
    }

    /// <summary>效果动作类型（效果原语词汇表）。</summary>
    public enum EffectAction
    {
        DealDamage,
        GainBlock,
        Heal,
        DrawCards,
        DiscardCards,
        ApplyStatus,
        RemoveStatus,
        DoubleStatus,
        ReduceStatusToZero,
        ExhaustCard,
        ExhaustRandomCard,
        ReduceCost,
        ReduceAllCostsInHand,
        RandomUpgrade,
        SpawnCopy,
        RepeatLastCard,
        NextTurnDraw,
        NextTurnEnergy,
        GainGold,
        Lifesteal,
        RetainCard
    }

    /// <summary>条件类型。</summary>
    public enum ConditionType
    {
        HasStatus,
        HpBelow,
        HpAbovePercent,
        HasRelic,
        CardPlayedThisTurnCount,
        EnemyCountAbove,
        NoEnemies,
        LastCardWas,
        PlayerHpLostThisCombat
    }

    /// <summary>敌人意图类型。</summary>
    public enum IntentType
    {
        Attack,
        Defend,
        Buff,
        Debuff,
        Summon,
        Special
    }

    /// <summary>事件奖励类型。</summary>
    public enum RewardType
    {
        Gold,
        CardChoice,
        Relic,
        Heal,
        RemoveCard,
        UpgradeCard,
        GainMaxHp,
        TransformCard
    }

    /// <summary>事件惩罚类型。</summary>
    public enum PenaltyType
    {
        LoseGold,
        LoseHp,
        AddCardCurse,
        RemoveRandomCard,
        LoseMaxHp
    }

    /// <summary>对话立绘位置。</summary>
    public enum PortraitPosition
    {
        Left,
        Center,
        Right,
        Off
    }

    /// <summary>对话行类型。</summary>
    public enum DialogueLineType
    {
        Normal,
        Choice,
        Narration,
        AutoAdvance
    }

    /// <summary>势力关系。</summary>
    public enum RelationType
    {
        Ally,
        Neutral,
        Hostile
    }
}
