using System.Collections.Generic;

namespace CardGame.Domain
{
    /// <summary>
    /// 仓库构建器（纯 C#，可脱离 Unity 测试）：
    /// 校验 → 全部通过 → 生成描述/预览文本 → 冻结为 DataRepository。
    /// 任一校验失败抛出携带全部错误条目的 DataValidationException。
    /// </summary>
    public static class DataRepositoryBuilder
    {
        public static DataRepository Build(
            IReadOnlyList<CardDef> cards,
            IReadOnlyList<RelicDef> relics,
            IReadOnlyList<EnemyDef> enemies,
            IReadOnlyList<StatusDef> statuses,
            IReadOnlyList<EventDef> events,
            IReadOnlyList<DialogueSequence> dialogues,
            WorldDef world,
            UiStrings uiStrings)
        {
            var errors = new List<DataValidationError>();
            if (world == null)
            {
                errors.Add(new DataValidationError(DtoMapper.WorldFile, "", "", "world.json 缺失或无法解析"));
            }
            errors.AddRange(ContentValidator.ValidateAll(cards, relics, enemies, statuses, events, dialogues, world));
            if (errors.Count > 0)
            {
                throw new DataValidationException(errors);
            }

            var statusDict = DataRepository.IndexById(statuses, s => s.Id);
            var relicDict = DataRepository.IndexById(relics, r => r.Id);
            DescriptionBuilder.FillCardDescriptions(cards, statusDict, relicDict);
            DescriptionBuilder.FillIntentPreviews(enemies);

            return new DataRepository(
                DataRepository.IndexById(cards, c => c.Id),
                relicDict,
                DataRepository.IndexById(enemies, e => e.Id),
                statusDict,
                DataRepository.IndexById(events, e => e.Id),
                DataRepository.IndexById(dialogues, d => d.Id),
                world,
                uiStrings ?? new UiStrings(null));
        }
    }
}
