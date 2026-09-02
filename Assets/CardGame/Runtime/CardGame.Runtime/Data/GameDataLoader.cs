using System;
using System.Collections.Generic;
using CardGame.Domain;
using UnityEngine;

namespace CardGame.Runtime
{
    /// <summary>
    /// 配置数据加载管线：反序列化 → DTO 映射 → 仓库构建。
    /// 全部错误聚合为单个 DataValidationException（协作者面对的完整错误报告）。
    /// 固定文件名约定见 DtoMapper 的 *File 常量。
    /// </summary>
    public sealed class GameDataLoader
    {
        private static readonly string[] RequiredFiles =
        {
            DtoMapper.CardsFile,
            DtoMapper.RelicsFile,
            DtoMapper.EnemiesFile,
            DtoMapper.StatusFile,
            DtoMapper.EventsFile,
            DtoMapper.DialoguesFile,
            DtoMapper.WorldFile,
            DtoMapper.UiStringsFile
        };

        private readonly IGameDataSerializer _serializer;

        public GameDataLoader(IGameDataSerializer serializer)
        {
            _serializer = serializer;
        }

        /// <summary>从 TextAsset 列表加载（文件名即类别约定）。</summary>
        public DataRepository Load(IReadOnlyList<TextAsset> assets)
        {
            var rawByFileName = new Dictionary<string, string>();
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset == null) continue;
                    // Unity 的 TextAsset.name 不含扩展名（如 "cards"），需补回 .json 以匹配约定文件名。
                    var fileName = asset.name.EndsWith(".json", StringComparison.Ordinal)
                        ? asset.name
                        : asset.name + ".json";
                    rawByFileName[fileName] = asset.text;
                }
            }
            return Load(rawByFileName);
        }

        /// <summary>从文件名 → JSON 原文的字典加载（可脱离 Unity 对象测试）。</summary>
        public DataRepository Load(IReadOnlyDictionary<string, string> rawByFileName)
        {
            var sink = new DataErrorSink();

            var cards = new List<CardDef>();
            var relics = new List<RelicDef>();
            var enemies = new List<EnemyDef>();
            var statuses = new List<StatusDef>();
            var events = new List<EventDef>();
            var dialogues = new List<DialogueSequence>();
            WorldDef world = null;
            UiStrings uiStrings = null;

            foreach (var file in RequiredFiles)
            {
                if (rawByFileName == null || !rawByFileName.TryGetValue(file, out var json) || json == null)
                {
                    sink.Add(file, "", "", "缺失必需文件");
                    continue;
                }
                LoadFile(file, json, sink, cards, relics, enemies, statuses, events, dialogues,
                    out var parsedWorld, out var parsedUiStrings);
                if (parsedWorld != null) world = parsedWorld;
                if (parsedUiStrings != null) uiStrings = parsedUiStrings;
            }

            foreach (var unknown in UnknownFileNames(rawByFileName))
            {
                sink.Add(unknown, "", "", "未知数据文件（约定文件名：cards/relics/enemies/status/events/dialogues/world/ui-strings.json）");
            }

            if (!sink.IsEmpty)
            {
                throw new DataValidationException(sink.Errors);
            }

            return DataRepositoryBuilder.Build(
                cards, relics, enemies, statuses, events, dialogues, world, uiStrings);
        }

        private void LoadFile(
            string file, string json, DataErrorSink sink,
            List<CardDef> cards, List<RelicDef> relics, List<EnemyDef> enemies,
            List<StatusDef> statuses, List<EventDef> events, List<DialogueSequence> dialogues,
            out WorldDef world, out UiStrings uiStrings)
        {
            world = null;
            uiStrings = null;

            if (file == DtoMapper.CardsFile)
            {
                if (TryParse<CardSetDto>(file, json, sink, out var dto))
                {
                    cards.AddRange(DtoMapper.MapCardSet(dto, sink));
                }
            }
            else if (file == DtoMapper.RelicsFile)
            {
                if (TryParse<RelicSetDto>(file, json, sink, out var dto))
                {
                    relics.AddRange(DtoMapper.MapRelicSet(dto, sink));
                }
            }
            else if (file == DtoMapper.EnemiesFile)
            {
                if (TryParse<EnemySetDto>(file, json, sink, out var dto))
                {
                    enemies.AddRange(DtoMapper.MapEnemySet(dto, sink));
                }
            }
            else if (file == DtoMapper.StatusFile)
            {
                if (TryParse<StatusSetDto>(file, json, sink, out var dto))
                {
                    statuses.AddRange(DtoMapper.MapStatusSet(dto, sink));
                }
            }
            else if (file == DtoMapper.EventsFile)
            {
                if (TryParse<EventSetDto>(file, json, sink, out var dto))
                {
                    events.AddRange(DtoMapper.MapEventSet(dto, sink));
                }
            }
            else if (file == DtoMapper.DialoguesFile)
            {
                if (TryParse<DialogueSetDto>(file, json, sink, out var dto))
                {
                    dialogues.AddRange(DtoMapper.MapDialogueSet(dto, sink));
                }
            }
            else if (file == DtoMapper.WorldFile)
            {
                if (TryParse<WorldDto>(file, json, sink, out var dto))
                {
                    world = DtoMapper.MapWorld(dto, sink);
                }
            }
            else if (file == DtoMapper.UiStringsFile)
            {
                if (TryParse<UiStringsRootDto>(file, json, sink, out var dto))
                {
                    uiStrings = DtoMapper.MapUiStrings(dto, sink);
                }
            }
        }

        private bool TryParse<T>(string file, string json, DataErrorSink sink, out T dto)
        {
            dto = default;
            try
            {
                dto = _serializer.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                sink.Add(file, "", "", $"JSON 解析失败：{ex.Message}");
                return false;
            }
            if (dto == null)
            {
                sink.Add(file, "", "", "内容为空或与协议结构不符");
                return false;
            }
            return true;
        }

        private static IEnumerable<string> UnknownFileNames(IReadOnlyDictionary<string, string> rawByFileName)
        {
            if (rawByFileName == null) yield break;
            var known = new HashSet<string>(RequiredFiles);
            foreach (var name in rawByFileName.Keys)
            {
                if (!known.Contains(name)) yield return name;
            }
        }
    }
}
