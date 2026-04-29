using System.Collections.Generic;

namespace SqlDemo
{
    /// <summary>
    /// 分页模式
    /// </summary>
    public enum PageMode
    {
        /// <summary>
        /// 自动模式：前面页码走 Offset，深分页走 Keyset
        /// </summary>
        Auto = 0,

        /// <summary>
        /// 页码分页（OFFSET/FETCH）
        /// </summary>
        Offset = 1,

        /// <summary>
        /// 游标分页（Keyset/Seek）
        /// </summary>
        Keyset = 2
    }

    /// <summary>
    /// 排序字段
    /// </summary>
    public sealed class QuerySort
    {
        /// <summary>
        /// 字段名（可传属性名或映射列名）
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// 是否倒序
        /// </summary>
        public bool Desc { get; set; }
    }

    /// <summary>
    /// 分页查询请求
    /// </summary>
    public sealed class PageQueryRequest
    {
        /// <summary>
        /// 分页模式：Auto / Offset / Keyset
        /// </summary>
        public PageMode Mode { get; set; } = PageMode.Auto;

        /// <summary>
        /// 页码（Offset 模式使用，从 1 开始）
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Auto 模式切换到 Keyset 的页码阈值
        /// </summary>
        public int AutoToKeysetPage { get; set; } = 50;

        /// <summary>
        /// 是否返回总条数（高并发建议按需开启）
        /// </summary>
        public bool IncludeTotal { get; set; } = true;

        /// <summary>
        /// Keyset 游标
        /// </summary>
        public string? Cursor { get; set; }

        /// <summary>
        /// 查询条件
        /// </summary>
        public List<QueryCondition> Conditions { get; set; } = new();

        /// <summary>
        /// 排序列表
        /// </summary>
        public List<QuerySort> Sorts { get; set; } = new();
    }

    /// <summary>
    /// 分页查询返回
    /// </summary>
    public sealed class PageQueryResult
    {
        /// <summary>
        /// 当前页数据
        /// </summary>
        public List<Dictionary<string, object?>> Items { get; set; } = new();

        /// <summary>
        /// 当前页码（Offset 模式有效）
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// 当前每页大小
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 总条数（按 IncludeTotal 决定是否返回）
        /// </summary>
        public long? Total { get; set; }

        /// <summary>
        /// 是否还有下一页
        /// </summary>
        public bool HasMore { get; set; }

        /// <summary>
        /// 下一页游标（Keyset 模式使用）
        /// </summary>
        public string? NextCursor { get; set; }

        /// <summary>
        /// 本次实际使用的分页模式
        /// </summary>
        public PageMode AppliedMode { get; set; }
    }
}
