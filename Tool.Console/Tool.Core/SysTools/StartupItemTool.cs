using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using static Tool.Core.SysTools.StartupItemTool;

namespace Tool.Core.SysTools
{
    /// <summary>
    /// 启动项管理，读取逻辑与任务管理器保持一致：
    /// 覆盖 HKCU\Run、HKLM\Run、用户启动文件夹、公共启动文件夹四个来源，
    /// 启用/禁用状态通过 StartupApproved 键的二进制标志位判断。
    /// </summary>
    public class StartupItemTool
    {
        // 是否以管理员身份运行，构造时计算一次，避免重复调用
        private readonly bool _isAdmin = IsAdmin();

        #region 注册表操作

        /// <summary>
        /// 读取全部启动项，来源与任务管理器一致：
        /// HKCU\Run、HKLM\Run、用户启动文件夹、公共启动文件夹
        /// </summary>
        public List<StartUpItemInfo> LoadAll()
        {
            var result = new List<StartUpItemInfo>();

            // 当前用户注册表启动项，普通权限可读
            result.AddRange(ReadRegistryRun(
                Registry.CurrentUser,
                runPath: @"Software\Microsoft\Windows\CurrentVersion\Run",
                approvedPath: @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
                source: "HKCU",
                readOnly: false));

            // 所有用户注册表启动项，非管理员时标记只读
            result.AddRange(ReadRegistryRun(
                Registry.LocalMachine,
                runPath: @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                approvedPath: @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
                source: "HKLM",
                readOnly: !_isAdmin));

            // 64 位系统上 32 位程序的启动项单独存在 WOW6432Node 下
            result.AddRange(ReadRegistryRun(
                Registry.LocalMachine,
                runPath: @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                approvedPath: @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
                source: "HKLM(x86)",
                readOnly: !_isAdmin));

            // 用户启动文件夹：%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
            result.AddRange(ReadStartUpFolder(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                source: "用户文件夹"));

            // 公共启动文件夹：%ProgramData%\Microsoft\Windows\Start Menu\Programs\Startup
            // 任务管理器也会读取此路径，对应"所有用户"的启动项
            result.AddRange(ReadStartUpFolder(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
                source: "公共文件夹"));

            return result;
        }

        /// <summary>
        /// 从指定注册表根键读取启动项。
        /// 任务管理器的判断逻辑：
        ///   1. 从 Run 键枚举所有键名和路径
        ///   2. 在 StartupApproved\Run 里查同名键的二进制值
        ///   3. 二进制值第 1 字节（data[0]）：0x02 = 启用，0x03 = 禁用
        ///   4. StartupApproved 里没有记录的条目视为启用（从未被任务管理器操作过）
        /// </summary>
        private static List<StartUpItemInfo> ReadRegistryRun(
            RegistryKey hive,
            string runPath,
            string approvedPath,
            string source,
            bool readOnly)
        {
            var result = new List<StartUpItemInfo>();

            // 预读 StartupApproved，构建 名称 -> 是否启用 的映射
            var approvedMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            using (RegistryKey? approved = hive.OpenSubKey(approvedPath, writable: false))
            {
                if (approved != null)
                {
                    foreach (string name in approved.GetValueNames())
                    {
                        byte[]? data = approved.GetValue(name) as byte[];
                        // data[0] 是状态标志位：0x02 启用，0x03 禁用
                        // 数据异常时保守处理为启用，避免误判
                        bool enabled = data == null || data.Length < 1 || data[0] == 0x02;
                        approvedMap[name] = enabled;
                    }
                }
            }

            using (RegistryKey? run = hive.OpenSubKey(runPath, writable: false))
            {
                if (run == null) return result;

                foreach (string name in run.GetValueNames())
                {
                    // 不在 approvedMap 里 = 从未被任务管理器操作过，视为启用
                    bool enabled = !approvedMap.TryGetValue(name, out bool val) || val;

                    result.Add(new StartUpItemInfo
                    {
                        Name = name,
                        // 键值为启动命令行，可能含参数，如 "C:\foo\bar.exe" --arg
                        Path = run.GetValue(name)?.ToString() ?? string.Empty,
                        Source = source,
                        Enabled = enabled,
                        ReadOnly = readOnly
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 读取指定启动文件夹中的所有文件。
        /// 任务管理器对文件夹启动项的启用/禁用状态同样存在 StartupApproved\StartupFolder，
        /// 此处简化处理：文件存在即为启用（与任务管理器行为基本一致）。
        /// </summary>
        private static List<StartUpItemInfo> ReadStartUpFolder(string folder, string source)
        {
            var result = new List<StartUpItemInfo>();

            if (!Directory.Exists(folder)) return result;

            foreach (string file in Directory.GetFiles(folder))
            {
                result.Add(new StartUpItemInfo
                {
                    Name = System.IO.Path.GetFileNameWithoutExtension(file),
                    Path = file,
                    Source = source,
                    Enabled = true   // 文件夹里的文件默认视为启用
                });
            }

            return result;
        }

        /// <summary>
        /// 判断当前进程是否以管理员身份运行。
        /// </summary>
        private static bool IsAdmin()
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// 启用或禁用一条注册表启动项。
        /// 原理：写 StartupApproved 对应键的二进制值，第 1 字节 0x02=启用，0x03=禁用。
        /// </summary>
        /// <param name="item">要操作的启动项，Source 必须是 HKCU / HKLM / HKLM(x86)</param>
        /// <param name="enable">true=启用，false=禁用</param>
        /// <exception cref="InvalidOperationException">文件夹来源不支持此操作</exception>
        /// <exception cref="UnauthorizedAccessException">HKLM 来源需要管理员权限</exception>
        public void SetEnabled(StartUpItemInfo item, bool enable)
        {
            (RegistryKey hive, string approvedPath) = item.Source switch
            {
                "HKCU" => (Registry.CurrentUser,
                                @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"),
                "HKLM" => (Registry.LocalMachine,
                                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"),
                "HKLM(x86)" => (Registry.LocalMachine,
                                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"),
                _ => throw new InvalidOperationException($"来源 '{item.Source}' 不支持启用/禁用操作")
            };

            using RegistryKey approved = hive.CreateSubKey(approvedPath, writable: true)
                ?? throw new InvalidOperationException("无法打开 StartupApproved 注册表键");

            // 保留原有 12 字节结构（任务管理器写入格式），仅修改第 1 字节状态标志
            byte[] data = approved.GetValue(item.Name) as byte[] ?? new byte[12];
            if (data.Length < 12) Array.Resize(ref data, 12);

            data[0] = enable ? (byte)0x02 : (byte)0x03;

            approved.SetValue(item.Name, data, RegistryValueKind.Binary);
        }

        #endregion

        #region 数据模型

        /// <summary>
        /// 单条启动项信息
        /// </summary>
        public sealed class StartUpItemInfo
        {
            /// <summary>启动项名称，对应注册表键名或文件名（不含扩展名）</summary>
            public string Name { get; init; } = string.Empty;

            /// <summary>启动项路径，注册表项为命令行字符串，文件夹项为完整文件路径</summary>
            public string Path { get; init; } = string.Empty;

            /// <summary>来源标识：HKCU / HKLM / HKLM(x86) / 用户文件夹 / 公共文件夹</summary>
            public string Source { get; init; } = string.Empty;

            /// <summary>是否处于启用状态，由 StartupApproved 标志位决定</summary>
            public bool Enabled { get; init; } = true;

            /// <summary>是否只读，HKLM 来源且非管理员时为 true，UI 据此禁用操作按钮</summary>
            public bool ReadOnly { get; init; } = false;
        }

        #endregion
    }
}
