using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tool.Core.FileTools
{
    /// <summary>
    /// 递归统计指定文件夹
    /// </summary>
    public class DirectorySizeToolcs
    {
        /// <summary>
        /// 递归统计指定文件夹的总大小及文件数量。
        /// </summary>
        /// <param name="dirPath">要统计的文件夹路径</param>
        /// <returns>包含总大小、文件数、子文件夹数的结果对象</returns>
        /// <exception cref="ArgumentException">路径为空时抛出</exception>
        /// <exception cref="DirectoryNotFoundException">文件夹不存在时抛出</exception>
        public DirectorySizeResult GetSize(string dirPath)
        {
            if (string.IsNullOrWhiteSpace(dirPath))
                throw new ArgumentException("路径不能为空", nameof(dirPath));
            if (!Directory.Exists(dirPath))
                throw new DirectoryNotFoundException($"文件夹不存在{dirPath}");
            // 用 DirectoryInfo 后续可以直接拿 Name、FullName 等属性
            var root = new DirectoryInfo(dirPath);
            //用ref在递归中原地累加，避免每层都创建新对象或中间结果再合并
            long totalBytes = 0;
            int fileCount = 0;
            int dirCount = 0;
            //收集权限不足时跳过的路径
            var errors = new List<string>();
            //从根目录开始递归
            Traverse(root,ref totalBytes, ref fileCount, ref dirCount,errors);
            return new DirectorySizeResult
            {
                Name = root.Name,
                FullPath = root.FullName,
                TotalSizeBytes = totalBytes,
                FileCount = fileCount,
                SubDirCount = dirCount,  // 不含根目录本身，只统计子孙层
                AccessErrors = errors,
            };
        }

        /// <summary>
        /// 递归遍历文件夹，将当前层及所有子层的文件大小、数量累加到传入的引用变量中。
        /// 遇到无权限访问的目录时，将路径记入 errors 并跳过，不中断整体统计。
        /// </summary>
        /// <param name="dir">当前正在处理的目录</param>
        /// <param name="totalBytes">累计文件总字节数</param>
        /// <param name="fileCount">累计文件总数</param>
        /// <param name="dirCount">累计子文件夹总数（不含根目录）</param>
        /// <param name="errors">权限不足时记录被跳过的路径</param>
        private static void Traverse(
            DirectoryInfo dir,
            ref long totalBytes,
            ref int fileCount,
            ref int dirCount,
            List<string> errors)
        {
            // 统计当前目录下的文件
            FileInfo[] files;
            try
            {
                // GetFiles() 只返回当前层，不递归，不会在这里就把子目录的文件也拿进来
                files = dir.GetFiles();
            }
            catch (UnauthorizedAccessException)
            {
                // 连当前目录的文件列表都读不到，整个目录跳过，子目录也不再尝试
                errors.Add($"无权限访问：{dir.FullName}");
                return;
            }

            foreach (FileInfo file in files)
            {
                // FileInfo.Length 单位是字节，直接累加
                totalBytes += file.Length;
                fileCount++;
            }

            // 获取子文件夹列表，然后逐个递归
            DirectoryInfo[] subDirs;
            try
            {
                // GetDirectories() 同样只返回当前层的直接子目录
                subDirs = dir.GetDirectories();
            }
            catch (UnauthorizedAccessException)
            {
                // 能读文件但读不到子目录列表（极少见，但确实存在）
                errors.Add($"无权限访问子目录：{dir.FullName}");
                return;
            }

            foreach (DirectoryInfo sub in subDirs)
            {
                // 每进入一个子目录就计数一次，根目录本身不在这里计，所以最终不含根
                dirCount++;

                // 递归处理子目录，ref 变量会继续在同一块内存上累加
                Traverse(sub, ref totalBytes, ref fileCount, ref dirCount, errors);
            }
        }

        /// <summary>
        /// 统计结果。
        /// </summary>
        public sealed class DirectorySizeResult
        {
            /// <summary>文件夹名称，不含父路径。例如 "Documents"</summary>
            public string Name { get; init; } = string.Empty;

            /// <summary>文件夹完整路径</summary>
            public string FullPath { get; init; } = string.Empty;

            /// <summary>所有文件的总大小，单位字节（递归统计）</summary>
            public long TotalSizeBytes { get; init; }

            /// <summary>递归统计到的文件总数</summary>
            public int FileCount { get; init; }

            /// <summary>递归统计到的子文件夹总数（不含根目录本身）</summary>
            public int SubDirCount { get; init; }

            /// <summary>
            /// 遍历过程中因权限不足跳过的路径列表。
            /// 为空表示统计完整，不为空表示结果可能偏小。
            /// </summary>
            public IReadOnlyList<string> AccessErrors { get; init; } = Array.Empty<string>();

            /// <summary>是否存在访问错误（统计结果可能不完整）</summary>
            public bool HasErrors => AccessErrors.Count > 0;

            /// <summary>
            /// 可读的总大小字符串，自动选择 B / KB / MB / GB。
            /// </summary>
            public string FormattedSize => TotalSizeBytes switch
            {
                < 1024 => $"{TotalSizeBytes} B",
                < 1024 * 1024 => $"{TotalSizeBytes / 1024.0:F2} KB",
                < 1024 * 1024 * 1024 => $"{TotalSizeBytes / 1024.0 / 1024:F2} MB",
                _ => $"{TotalSizeBytes / 1024.0 / 1024 / 1024:F2} GB",
            };
        }
    }
}
