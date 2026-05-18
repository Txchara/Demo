using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tool.Core.FileTools
{
    /// <summary>
    /// 查询文件或文件夹的基本信息
    /// </summary>
    public class FileInfoTool
    {
        /// <summary>
        /// 查询文件或文件夹的基本信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns>包含大小、时间戳、属性等信息的结果对象</returns>
        public FileInfoResult GetFileInfo(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("路径不能为空",nameof(path));
            bool isDirectory = Directory.Exists(path);
            bool isFile = File.Exists(path);
            //两者都不存在 则路径无效
            if (!isFile && !isDirectory)
                throw new FileNotFoundException("文件或文件夹不存在",path);
            if (isDirectory)
            {
                var dirsize = new DirectorySizeToolcs();
                var dir = new DirectoryInfo(path);
                return new FileInfoResult
                {
                    Name = dir.Name,
                    FullPath = dir.FullName,
                    IsDirectory = true,
                    SizeBytes = dirsize.GetSize(path).TotalSizeBytes,
                    CreatedAt = dir.CreationTime,
                    ModifiedAt = dir.LastWriteTime,
                    AccessedAt = dir.LastAccessTime,
                    Attributes = dir.Attributes,
                    // 文件夹没有只读概念（只读属性对文件夹无实际约束）
                    IsReadOnly = false,
                    IsHidden = dir.Attributes.HasFlag(FileAttributes.Hidden),
                    IsSystem = dir.Attributes.HasFlag(FileAttributes.System),
                };
            }
            else
            {
                var file = new FileInfo(path);
                return new FileInfoResult
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    IsDirectory = false,
                    Extension = file.Extension,
                    // FileInfo.Length 单位是字节
                    SizeBytes = file.Length,
                    CreatedAt = file.CreationTime,
                    ModifiedAt = file.LastWriteTime,
                    AccessedAt = file.LastAccessTime,
                    Attributes = file.Attributes,
                    // FileInfo 直接提供 IsReadOnly，不用手动检查 Attributes
                    IsReadOnly = file.IsReadOnly,
                    IsHidden = file.Attributes.HasFlag(FileAttributes.Hidden),
                    IsSystem = file.Attributes.HasFlag(FileAttributes.System),
                };
            }
        }
        /// <summary>
        /// 查询结果
        /// </summary>
        public sealed class FileInfoResult
        {
            /// <summary>文件或文件夹的名称，不含父路径。例如 "report.xlsx"</summary>
            public string Name { get; init; } = string.Empty;

            /// <summary>完整路径。例如 "D:\Documents\report.xlsx"</summary>
            public string FullPath { get; init; } = string.Empty;

            /// <summary>true 表示这是一个文件夹，false 表示文件</summary>
            public bool IsDirectory { get; init; }

            /// <summary>
            /// 文件扩展名，含点号。例如 ".xlsx"
            /// 文件夹时为 null
            /// </summary>
            public string? Extension { get; init; }

            /// <summary>
            /// 文件大小，单位字节
            /// 文件夹时为 null，不做递归统计
            /// </summary>
            public long? SizeBytes { get; init; }

            /// <summary>创建时间（本地时间）</summary>
            public DateTime CreatedAt { get; init; }

            /// <summary>最后修改时间（本地时间）</summary>
            public DateTime ModifiedAt { get; init; }

            /// <summary>最后访问时间（本地时间）</summary>
            public DateTime AccessedAt { get; init; }

            /// <summary>
            /// 原始文件属性标志位，可用于判断 Archive、Compressed 等不常用属性。
            /// 常用的 Hidden / ReadOnly / System 已单独提取为布尔属性。
            /// </summary>
            public FileAttributes Attributes { get; init; }

            /// <summary>是否只读。文件夹始终为 false</summary>
            public bool IsReadOnly { get; init; }

            /// <summary>是否隐藏（资源管理器默认不显示）</summary>
            public bool IsHidden { get; init; }

            /// <summary>是否系统文件（通常不应手动修改）</summary>
            public bool IsSystem { get; init; }

            /// <summary>
            /// 可读的文件大小字符串
            /// 自动选择合适的单位：B / KB / MB / GB
            /// 文件夹返回 "-"
            /// </summary>
            public string FormattedSize => SizeBytes is null
                ? "-"
                : SizeBytes switch
                {
                    < 1024 => $"{SizeBytes} B",
                    < 1024 * 1024 => $"{SizeBytes / 1024.0:F2} KB",
                    < 1024 * 1024 * 1024 => $"{SizeBytes / 1024.0 / 1024:F2} MB",
                    _ => $"{SizeBytes / 1024.0 / 1024 / 1024:F2} GB",
                };
        }
    }
}
