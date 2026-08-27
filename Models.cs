using System;
using System.Collections.ObjectModel;

namespace UvncAddressBook
{
    /// <summary>
    /// 主机记录（对应 Hosts 表）。
    /// 注意：绝不持久化 VNC 连接密码；密码由 UltraVNC 自身在连接时弹出输入框。
    /// </summary>
    public class Host
    {
        public int Id { get; set; }

        /// <summary>所属分组 Id；-1 表示未分组（通常不出现，所有主机应有分组）</summary>
        public int GroupId { get; set; }

        /// <summary>显示名称，例如“采血室-03”</summary>
        public string DisplayName { get; set; } = "";

        /// <summary>内网 IPv4 地址字符串</summary>
        public string IpAddress { get; set; } = "";

        /// <summary>端口，默认 5900</summary>
        public int Port { get; set; } = 5900;

        /// <summary>默认连接模式：FullControl=完全控制，ViewOnly=仅查看</summary>
        public string DefaultMode { get; set; } = "FullControl";

        /// <summary>备注</summary>
        public string Comment { get; set; } = "";

        public string CreateTime { get; set; } = "";
        public string UpdateTime { get; set; } = "";

        /// <summary>列表展示用的中文模式文本（不入库）</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string ModeText => DefaultMode == "ViewOnly" ? "仅查看" : "完全控制";
    }

    /// <summary>
    /// 分组（对应 Groups 表），支持父子层级（ParentId）。
    /// </summary>
    public class Group
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = "";
        /// <summary>-1 表示顶级分组</summary>
        public int ParentId { get; set; } = -1;
    }

    /// <summary>
    /// 树形节点（仅用于 UI 绑定，不入库）。
    /// </summary>
    public class GroupNode
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = "";
        public bool IsRoot { get; set; } = false;
        public ObservableCollection<GroupNode> Children { get; set; } = new ObservableCollection<GroupNode>();
    }

    /// <summary>
    /// 备份文件结构：分组 + 主机，用于导出/导入迁移。
    /// </summary>
    public class BackupData
    {
        public System.Collections.Generic.List<Group> Groups { get; set; } = new System.Collections.Generic.List<Group>();
        public System.Collections.Generic.List<  Host> Hosts { get; set; } = new System.Collections.Generic.List<Host>();
    }
}
