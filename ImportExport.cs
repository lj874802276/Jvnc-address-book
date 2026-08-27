using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UvncAddressBook
{
    /// <summary>
    /// 主机/分组 JSON 备份与恢复（用于运维换机迁移）。
    /// 使用内置 System.Text.Json，无第三方、无联网。
    /// </summary>
    public static class ImportExport
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>导出分组与主机到 JSON 文件。</summary>
        public static void ExportToFile(string filePath, List<Group> groups, List<Host> hosts)
        {
            var data = new BackupData { Groups = groups, Hosts = hosts };
            File.WriteAllText(filePath, JsonSerializer.Serialize(data, Options));
        }

        /// <summary>导出选中的主机（同时包含它们所属的分组，保证文件自包含）。</summary>
        public static void ExportSelectedToFile(string filePath, List<Host> selected)
        {
            var allGroups = Db.GetAllGroups();
            var usedIds = new HashSet<int>();
            foreach (var h in selected) usedIds.Add(h.GroupId);
            var usedGroups = allGroups.FindAll(g => usedIds.Contains(g.Id));
            ExportToFile(filePath, usedGroups, selected);
        }

        /// <summary>从 JSON 文件读取备份数据。</summary>
        public static BackupData ImportFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<BackupData>(json);
            if (data == null) throw new InvalidOperationException("文件内容无法识别，不是有效的备份文件。");
            data.Groups ??= new List<Group>();
            data.Hosts ??= new List<Host>();
            return data;
        }
    }
}
