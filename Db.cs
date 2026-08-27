using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;

namespace UvncAddressBook
{
    /// <summary>
    /// 本地 SQLite 数据访问层。
    /// 数据库文件与 exe 同目录，无网络、无外部依赖。
    /// </summary>
    public static class Db
    {
        // 数据库文件固定与 exe 同目录
        private static readonly string DbPath =
            System.IO.Path.Combine(AppContext.BaseDirectory, "uvnc_addressbook.db");

        private static SqliteConnection Open()
        {
            var conn = new SqliteConnection("Data Source=" + DbPath);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// 首次启动自动建库建表。
        /// </summary>
        public static void Init()
        {
            using var conn = Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Groups (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GroupName TEXT NOT NULL,
                    ParentId INTEGER NOT NULL DEFAULT -1
                );
                CREATE TABLE IF NOT EXISTS Hosts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GroupId INTEGER NOT NULL,
                    DisplayName TEXT NOT NULL,
                    IpAddress TEXT NOT NULL,
                    Port INTEGER NOT NULL DEFAULT 5900,
                    DefaultMode TEXT NOT NULL DEFAULT 'FullControl',
                    Comment TEXT,
                    CreateTime TEXT NOT NULL,
                    UpdateTime TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        // ---------- 分组 ----------

        public static List<Group> GetAllGroups()
        {
            var list = new List<Group>();
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, GroupName, ParentId FROM Groups ORDER BY Id";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Group
                {
                    Id = reader.GetInt32(0),
                    GroupName = reader.GetString( 1),
                    ParentId = reader.GetInt32(2)
                });
            }
            return list;
        }

        public static int AddGroup(string name, int parentId)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Groups (GroupName, ParentId) VALUES (@name, @pid); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@pid", parentId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void UpdateGroup(int id, string name)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Groups SET GroupName=@name WHERE Id=@id";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 删除分组及其所有子分组、子分组下的主机（递归）。
        /// </summary>
        public static void DeleteGroupRecursive(int id)
        {
            var all = GetAllGroups();
            var toDelete = new List<int>();
            CollectDescendants(all, id, toDelete);
            toDelete.Add(id);

            using var conn = Open();
            using var tx = conn.BeginTransaction();
            // 删除这些分组下的所有主机
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Hosts WHERE GroupId = @gid";
                foreach (var gid in toDelete)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@gid", gid);
                    cmd.ExecuteNonQuery();
                }
            }
            // 删除分组本身
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Groups WHERE Id = @id";
                foreach (var gid in toDelete)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@id", gid);
                    cmd.ExecuteNonQuery();
                }
            }
            tx.Commit();
        }

        private static void CollectDescendants(List<Group> all, int parentId, List<int> result)
        {
            foreach (var g in all)
            {
                if (g.ParentId == parentId)
                {
                    result.Add(g.Id);
                    CollectDescendants(all, g.Id, result);
                }
            }
        }

        // ---------- 主机 ----------

        /// <summary>
        /// 读取主机。groupId = -1 表示读取全部主机（用于“全部主机”根节点）。
        /// </summary>
        public static List<Host> GetHosts(int groupId)
        {
            var list = new List<Host>();
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            if (groupId == -1)
                cmd.CommandText = "SELECT Id, GroupId, DisplayName, IpAddress, Port, DefaultMode, Comment, CreateTime, UpdateTime FROM Hosts ORDER BY Id";
            else
            {
                cmd.CommandText = "SELECT Id, GroupId, DisplayName, IpAddress, Port, DefaultMode, Comment, CreateTime, UpdateTime FROM Hosts WHERE GroupId=@gid ORDER BY Id";
                cmd.Parameters.AddWithValue("@gid", groupId);
            }
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Host
                {
                    Id = reader.GetInt32(0),
                    GroupId = reader.GetInt32(1),
                    DisplayName = reader.GetString(2),
                    IpAddress = reader.GetString(3),
                    Port = reader.GetInt32(4),
                    DefaultMode = reader.IsDBNull(5) ? "FullControl" : reader.GetString(5),
                    Comment = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    CreateTime = reader.GetString(7),
                    UpdateTime = reader.GetString(8)
                });
            }
            return list;
        }

        public static int AddHost(Host h)
        {
            var now = Now();
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Hosts (GroupId, DisplayName, IpAddress, Port, DefaultMode, Comment, CreateTime, UpdateTime)
                                VALUES (@gid, @name, @ip, @port, @mode, @comment, @ct, @ut);
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@gid", h.GroupId);
            cmd.Parameters.AddWithValue("@name", h.DisplayName);
            cmd.Parameters.AddWithValue("@ip", h.IpAddress);
            cmd.Parameters.AddWithValue("@port", h.Port);
            cmd.Parameters.AddWithValue("@mode", h.DefaultMode);
            cmd.Parameters.AddWithValue("@comment", h.Comment ?? "");
            cmd.Parameters.AddWithValue("@ct", now);
            cmd.Parameters.AddWithValue("@ut", now);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void UpdateHost(Host h)
        {
            var now = Now();
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Hosts SET GroupId=@gid, DisplayName=@name, IpAddress=@ip, Port=@port,
                                DefaultMode=@mode, Comment=@comment, UpdateTime=@ut WHERE Id=@id";
            cmd.Parameters.AddWithValue("@gid", h.GroupId);
            cmd.Parameters.AddWithValue("@name", h.DisplayName);
            cmd.Parameters.AddWithValue("@ip", h.IpAddress);
            cmd.Parameters.AddWithValue("@port", h.Port);
            cmd.Parameters.AddWithValue("@mode", h.DefaultMode);
            cmd.Parameters.AddWithValue("@comment", h.Comment ?? "");
            cmd.Parameters.AddWithValue("@ut", now);
            cmd.Parameters.AddWithValue("@id", h.Id);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateHostGroup(int hostId, int groupId)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Hosts SET GroupId=@gid, UpdateTime=@ut WHERE Id=@id";
            cmd.Parameters.AddWithValue("@gid", groupId);
            cmd.Parameters.AddWithValue("@ut", Now());
            cmd.Parameters.AddWithValue("@id", hostId);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteHost(int id)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Hosts WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 用备份数据覆盖现有数据（清空后写入）。用于导入迁移。
        /// 保留原记录的 CreateTime/UpdateTime；缺失则用当前时间。
        /// </summary>
        public static void ReplaceAll(BackupData data)
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();

            using (var c = conn.CreateCommand()) { c.CommandText = "DELETE FROM Hosts"; c.ExecuteNonQuery(); }
            using (var c = conn.CreateCommand()) { c.CommandText = "DELETE FROM Groups"; c.ExecuteNonQuery(); }

            using (var gcmd = conn.CreateCommand())
            {
                gcmd.CommandText = "INSERT INTO Groups (Id, GroupName, ParentId) VALUES (@id, @name, @pid)";
                foreach (var g in data.Groups)
                {
                    gcmd.Parameters.Clear();
                    gcmd.Parameters.AddWithValue("@id", g.Id);
                    gcmd.Parameters.AddWithValue("@name", g.GroupName ?? "");
                    gcmd.Parameters.AddWithValue("@pid", g.ParentId);
                    gcmd.ExecuteNonQuery();
                }
            }

            using (var hcmd = conn.CreateCommand())
            {
                hcmd.CommandText = @"INSERT INTO Hosts (Id, GroupId, DisplayName, IpAddress, Port, DefaultMode, Comment, CreateTime, UpdateTime)
                                     VALUES (@id, @gid, @name, @ip, @port, @mode, @comment, @ct, @ut)";
                foreach (var h in data.Hosts)
                {
                    hcmd.Parameters.Clear();
                    hcmd.Parameters.AddWithValue("@id", h.Id);
                    hcmd.Parameters.AddWithValue("@gid", h.GroupId);
                    hcmd.Parameters.AddWithValue("@name", h.DisplayName ?? "");
                    hcmd.Parameters.AddWithValue("@ip", h.IpAddress ?? "");
                    hcmd.Parameters.AddWithValue("@port", h.Port);
                    hcmd.Parameters.AddWithValue("@mode", h.DefaultMode ?? "FullControl");
                    hcmd.Parameters.AddWithValue("@comment", h.Comment ?? "");
                    hcmd.Parameters.AddWithValue("@ct", string.IsNullOrEmpty(h.CreateTime) ? Now() : h.CreateTime);
                    hcmd.Parameters.AddWithValue("@ut", string.IsNullOrEmpty(h.UpdateTime) ? Now() : h.UpdateTime);
                    hcmd.ExecuteNonQuery();
                }
            }

            tx.Commit();
        }

        // ---------- 配置（Settings 表） ----------

        public static string GetSetting(string key, string defaultValue = "")
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key=@k";
            cmd.Parameters.AddWithValue("@k", key);
            var v = cmd.ExecuteScalar();
            return v == null ? defaultValue : v.ToString();
        }

        public static void SetSetting(string key, string value)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Settings (Key, Value) VALUES (@k, @v) ON CONFLICT(Key) DO UPDATE SET Value=@v";
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@v", value);
            cmd.ExecuteNonQuery();
        }

        // ---------- 校验 ----------

        /// <summary>
        /// 校验内网 IPv4 地址：格式合法且属于私有/链路本地网段（10.x / 172.16-31.x / 192.168.x / 169.254.x）。
        /// </summary>
        public static bool IsValidInternalIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            if (!System.Net.IPAddress.TryParse(ip, out var addr)) return false;
            if (addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false; // 仅 IPv4
            var bytes = addr.GetAddressBytes();
            // 10.0.0.0/8
            if (bytes[0] == 10) return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            // 169.254.0.0/16 链路本地
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            return false;
        }

        public static bool IsValidPort(int port) => port >= 1 && port <= 65535;

        private static string Now()
        {
            // 本地时间字符串，避免依赖云时间
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
