using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Tool.Core.FileTools
{
    /// <summary>
    /// 文件哈希计算工具 支持 MD5 和 SHA256
    /// </summary>
    public class FileHashTool
    {
        /// <summary>
        /// 支持的哈希算法类型
        /// </summary>
        public enum HashAlgorithmType
        {
            MD5,// 128位，输出32个十六进制字符，速度快，安全性较低
            SHA256// 256位，输出64个十六进制字符，速度稍慢，安全性高
        }

        /// <summary>
        /// 计算文件的哈希值
        /// </summary>
        /// <param name="filePath">文件完整路径</param>
        /// <param name="algorithmType">使用的哈希算法</param>
        /// <param name="progress">进度回调，参数为0-1，大文件用于更新进度条</param>
        /// <param name="cancellationToken">哈希结果对象</param>
        /// <returns></returns>
        public async Task<FileHashResult> ComputeAsync(
                string filePath,
                HashAlgorithmType algorithmType = HashAlgorithmType.SHA256,
                IProgress<double>? progress = null,
                CancellationToken cancellationToken = default
            )
        {
            // File.Exists 只检查路径是否指向一个实际存在的文件
            // 路径格式错误、权限不足等情况下也会返回 false，统一抛 FileNotFoundException
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在",filePath);

            var startTime = DateTime.Now;

            // using 块确保 HashAlgorithm 用完后立即释放非托管内存
            // HashAlgorithm 是抽象基类，MD5/SHA256 都继承自它，可以统一传给 ComputeHashAsync
            using HashAlgorithm hasher = algorithmType switch
            {
                HashAlgorithmType.MD5    => System.Security.Cryptography.MD5.Create(),
                HashAlgorithmType.SHA256 => System.Security.Cryptography.SHA256.Create(),
                _ => throw new ArgumentOutOfRangeException(nameof(algorithmType))
            };

            // 实际的分块读取和哈希计算在私有方法里完成，返回原始字节数组
            byte[] hashBytes = await ComputeHashAsync(filePath, hasher, progress, cancellationToken);

            return new FileHashResult
            {
                FilePath  = filePath,
                Algorithm = algorithmType.ToString(),
                // BitConverter.ToString 输出 "AB-CD-EF-..." 格式
                // Replace("-", "") 去掉连字符，ToUpperInvariant 统一大写，得到标准十六进制字符串
                HashHex   = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant(),
                // 这里再次 new FileInfo 是为了获取文件大小，开销极小
                FileSize  = new FileInfo(filePath).Length,
                ComputedAt = startTime,
                Elapsed   = DateTime.Now - startTime,
            };
        }

        /// <summary>
        /// 同时计算 MD5 和 SHA256，只读一次文件，比分别调用两次更高效
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<(FileHashResult MD5, FileHashResult SHA256)> ComputeHashAsync(
                string filePath,
                IProgress<double>? progress = null,
                CancellationToken cancellationToken = default
            )
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            var startTime = DateTime.Now;
            // FileInfo 只读取文件元数据（大小、时间等），不打开文件内容，开销很小
            var fileInfo = new FileInfo(filePath);

            // 两个算法实例各自维护独立的内部状态，互不干扰
            using var md5    = System.Security.Cryptography.MD5.Create();
            using var sha256 = System.Security.Cryptography.SHA256.Create();

            // 1 MB 缓冲区：太小会导致频繁 IO 系统调用，太大会占用过多内存
            // 对于大多数磁盘，1 MB 是吞吐量和内存的较好平衡点
            const int bufferSize = 1024 * 1024;
            byte[] buffer   = new byte[bufferSize];
            long totalRead  = 0;

            // FileMode.Open      — 打开已有文件，不创建新文件
            // FileAccess.Read    — 只读，防止意外写入
            // FileShare.Read     — 允许其他进程同时读取同一文件（如资源管理器预览）
            // useAsync: true     — 启用 Windows 异步 IO（IOCP），读取期间不阻塞线程池线程
            // await using        — 异步释放，确保流关闭后才继续执行后续代码
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize,
                useAsync: true
            );

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                // ReadAsync 返回 0 表示已到文件末尾，循环自然结束
                // 在 ReadAsync 之后再检查取消，避免取消时丢失已读数据导致状态不一致
                cancellationToken.ThrowIfCancellationRequested();

                // TransformBlock：将这一块数据"喂"给算法，更新内部哈希状态
                // 参数：输入缓冲区, 起始偏移, 有效字节数, 输出缓冲区(null=不需要), 输出偏移
                // 注意：bytesRead 而非 bufferSize，最后一块可能不足 1 MB
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);

                totalRead += bytesRead;
                // Report 值域 0.0~1.0，UI 层可直接绑定到 ProgressBar.Value（乘以100）
                progress?.Report((double)totalRead / fileInfo.Length);
            }

            // TransformFinalBlock：标记数据结束，算法在此完成 padding 和最终压缩
            // 传入空数据（长度0）表示没有额外的尾部数据，仅触发收尾计算
            // 调用后 .Hash 属性才可用，之前访问会得到 null
            md5.TransformFinalBlock(buffer, 0, 0);
            sha256.TransformFinalBlock(buffer, 0, 0);

            var elapsed = DateTime.Now - startTime;

            var md5Result = new FileHashResult
            {
                FilePath   = filePath,
                Algorithm  = "MD5",
                // md5.Hash 是 byte[]，! 断言非 null（TransformFinalBlock 之后必然有值）
                HashHex    = BitConverter.ToString(md5.Hash!).Replace("-", "").ToUpperInvariant(),
                FileSize   = fileInfo.Length,
                ComputedAt = startTime,
                Elapsed    = elapsed,
            };

            var sha256Result = new FileHashResult
            {
                FilePath   = filePath,
                Algorithm  = "SHA256",
                HashHex    = BitConverter.ToString(sha256.Hash!).Replace("-", "").ToUpperInvariant(),
                FileSize   = fileInfo.Length,
                ComputedAt = startTime,
                Elapsed    = elapsed,
            };

            return (md5Result, sha256Result);
        }

        /// <summary>
        /// 比较两个哈希值是否一致（忽略大小写）
        /// 用于验证：下载的文件哈希 vs 官方公布的哈希
        /// </summary>
        public static bool Verify(string hashA, string hashB)
            => string.Equals(hashA.Trim(), hashB.Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 内部核心方法：以分块方式读取文件并驱动哈希算法，返回原始哈希字节数组
        /// 调用方负责创建和释放 hasher 实例（using 在外层）
        /// </summary>
        private static async Task<byte[]> ComputeHashAsync(
            string filePath,
            HashAlgorithm hasher,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            const int bufferSize = 1024 * 1024; // 1 MB，单次 IO 读取量
            byte[] buffer    = new byte[bufferSize];
            long totalRead   = 0;
            // 提前获取文件长度用于进度计算，避免在循环内反复访问文件系统
            long fileLength  = new FileInfo(filePath).Length;

            // FileShare.Read 允许其他进程同时读取，不独占文件
            // useAsync:true 使用操作系统级异步 IO，避免线程阻塞等待磁盘
            await using var stream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read,
                FileShare.Read, bufferSize, useAsync: true);

            int bytesRead;
            // ReadAsync 返回实际读到的字节数，到文件末尾时返回 0，退出循环
            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 将本块数据累积进哈希状态机，不产生最终结果
                // 第4、5参数为输出缓冲区，传 null/0 表示不需要中间输出
                hasher.TransformBlock(buffer, 0, bytesRead, null, 0);

                totalRead += bytesRead;
                // 防止空文件（fileLength==0）时除以零
                progress?.Report(fileLength > 0 ? (double)totalRead / fileLength : 0);
            }

            // 文件读完后调用 TransformFinalBlock 完成 padding，之后 .Hash 才有值
            hasher.TransformFinalBlock(buffer, 0, 0);

            // Hash 在 TransformFinalBlock 之后必然非 null，! 仅用于消除编译器警告
            return hasher.Hash!;
        }

        /// <summary>
        /// 哈希计算结果
        /// </summary>
        public sealed class FileHashResult
        {
            /// <summary>被计算的文件路径</summary>
            public string FilePath { get; init; } = string.Empty;

            /// <summary>使用的算法名称，如 "MD5" / "SHA256"</summary>
            public string Algorithm { get; init; } = string.Empty;

            /// <summary>
            /// 哈希值的十六进制字符串（大写）
            /// MD5 示例：  D41D8CD98F00B204E9800998ECF8427E（32位）
            /// SHA256 示例：E3B0C44298FC1C149AFBF4C8996FB924...（64位）
            /// </summary>
            public string HashHex { get; init; } = string.Empty;

            /// <summary>文件大小（字节）</summary>
            public long FileSize { get; init; }

            /// <summary>计算开始时间</summary>
            public DateTime ComputedAt { get; init; }

            /// <summary>计算耗时</summary>
            public TimeSpan Elapsed { get; init; }
        }
    }
}
