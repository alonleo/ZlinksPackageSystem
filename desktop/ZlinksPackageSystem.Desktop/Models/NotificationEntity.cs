namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 与后端 <c>notification</c> 表对应的 DTO。
    /// 字段名与后端驼峰映射（map-underscore-to-camel-case）。
    /// </summary>
    public class NotificationEntity
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        /// <summary>通知所属模块（如：games / products / tools / system）</summary>
        public string Module { get; set; } = string.Empty;
        public long? TargetId { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public long? SenderId { get; set; }
        public string ReceiverIds { get; set; } = string.Empty;
        public string ReceiverType { get; set; } = string.Empty;
        /// <summary>1=置顶，0=普通</summary>
        public int IsPinned { get; set; }
        /// <summary>状态（"0"=草稿，"1"=已发布 等；具体语义由后端约定）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>发送者姓名（关联查询字段，仅展示）</summary>
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverNames { get; set; } = string.Empty;

        public string CreateBy { get; set; } = string.Empty;
        public string CreateTime { get; set; } = string.Empty;
    }
}
