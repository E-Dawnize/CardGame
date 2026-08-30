using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CardGame.Domain
{
    /// <summary>单条数据校验/映射错误：来源文件、条目 Id、字段与描述。</summary>
    public sealed class DataValidationError
    {
        public DataValidationError(string file, string id, string field, string message)
        {
            File = file;
            Id = id;
            Field = field;
            Message = message;
        }

        public string File { get; }
        public string Id { get; }
        public string Field { get; }
        public string Message { get; }

        public override string ToString()
        {
            return $"{File}: {Id} {Field}: {Message}";
        }
    }

    /// <summary>
    /// 配置数据校验异常：携带全部错误条目（聚合而非 fail-fast），
    /// 是协作者面对的完整错误报告。风格对齐 RazorFramework.DI 的结构化异常。
    /// </summary>
    public sealed class DataValidationException : InvalidOperationException
    {
        public DataValidationException(IEnumerable<DataValidationError> errors)
            : base(BuildMessage(errors))
        {
            var list = errors == null
                ? new List<DataValidationError>()
                : new List<DataValidationError>(errors);
            Errors = new ReadOnlyCollection<DataValidationError>(list);
        }

        public IReadOnlyList<DataValidationError> Errors { get; }

        private static string BuildMessage(IEnumerable<DataValidationError> errors)
        {
            var list = errors?.ToList() ?? new List<DataValidationError>();
            var count = list.Count;
            if (count == 0) return "配置数据校验失败。";
            return $"配置数据校验失败（{count} 处）：\n" +
                   string.Join("\n", list.Take(50).Select(e => "  - " + e)) +
                   (count > 50 ? "\n  ...（仅显示前 50 条）" : string.Empty);
        }
    }

    /// <summary>映射/校验期间的错误收集器（公开以支持测试与 Unity 侧加载器注入）。</summary>
    public sealed class DataErrorSink
    {
        private readonly List<DataValidationError> _errors = new List<DataValidationError>();

        public void Add(string file, string id, string field, string message)
        {
            _errors.Add(new DataValidationError(file, id, field, message));
        }

        public bool IsEmpty => _errors.Count == 0;

        public IReadOnlyList<DataValidationError> Errors => _errors.AsReadOnly();
    }
}
