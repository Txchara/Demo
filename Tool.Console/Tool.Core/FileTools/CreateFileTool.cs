using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Tool.Core.FileTools;

/// <summary>
/// 批量创建文件
/// </summary>
public class CreateFileTool
{
    /// <summary>
    /// 生成用于导入文件名称模板
    /// </summary>
    /// <param name="filePath">模板保存的路径</param>
    public void CreateTemplate(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("模板保存路径不能为空", nameof(filePath));
        }

        // 取出目录部分 D:\模板.xlsx => D:
        string? directory = Path.GetDirectoryName(filePath);
        // 如果目录不存在，就创建对应的目录
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 创建一个 .xlsx 工作簿
        using var workbook = new XSSFWorkbook();
        // 创建一个工作表
        ISheet sheet = workbook.CreateSheet("文件名称");

        // 创建表头
        IRow headerRow = sheet.CreateRow(0);
        headerRow.CreateCell(0).SetCellValue("文件名称");

        // 插入第2、3行为示例数据
        IRow row1 = sheet.CreateRow(1);
        row1.CreateCell(0).SetCellValue("示例文件1");

        IRow row2 = sheet.CreateRow(2);
        row2.CreateCell(0).SetCellValue("示例文件2");

        // 自动调整第一列宽度
        sheet.AutoSizeColumn(0);

        // 创建输出文件流，写入磁盘
        using FileStream stream = File.Create(filePath);
        workbook.Write(stream);
    }

    /// <summary>
    /// 从Excel中读取文件名称
    /// </summary>
    /// <param name="filePath">文件完整路径</param>
    /// <returns>去重后的文件名列表，不带扩展名</returns>
    public List<string> ReadFileNamesFromTemplate(string filePath)
    {
        // Excel 路径不能为空
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Excel 路径不能为空", nameof(filePath));
        }

        // 文件不存在
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Excel 文件不存在", filePath);
        }

        // 打开 Excel 文件（只读）
        using FileStream stream = File.OpenRead(filePath);
        // 读取 xlsx 工作簿
        using var workbook = new XSSFWorkbook(stream);
        // 取第一个工作簿（只取第一个，不做多sheet兼容）
        ISheet sheet = workbook.GetSheetAt(0);

        if (sheet is null)
        {
            throw new InvalidOperationException("Excel 中没有工作表");
        }

        // 结果集合，最终返回给上层
        var result = new List<string>();

        // 从第2行开始读，NPOI行号从0开始，所以i = 1表示Excel的第2行
        for (int i = 1; i <= sheet.LastRowNum; i++)
        {
            IRow? row = sheet.GetRow(i);    // 获取当前行
            if (row is null) continue;      // 空行跳过

            ICell? cell = row.GetCell(0);   // 只取第一列
            if (cell is null) continue;     // 第一列没有文件名直接跳过

            // 把单元格内容统一转为字符串
            string rawValue = cell.CellType switch
            {
                CellType.String => cell.StringCellValue ?? string.Empty,
                CellType.Numeric => cell.ToString() ?? string.Empty,
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.CachedFormulaResultType switch
                {
                    CellType.String => cell.StringCellValue ?? string.Empty,
                    CellType.Numeric => cell.NumericCellValue.ToString(),
                    CellType.Boolean => cell.BooleanCellValue.ToString(),
                    _ => cell.ToString() ?? string.Empty
                },
                _ => cell.ToString() ?? string.Empty
            };

            // 去掉首尾空格
            rawValue = rawValue.Trim();
            // 空内容跳过
            if (string.IsNullOrWhiteSpace(rawValue)) continue;

            // 后缀应该由单独的输入框决定
            string fileName = Path.GetFileNameWithoutExtension(rawValue);
            // 去掉非法字符 不能包含 / \ : * ? " < > | 等字符
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar.ToString(), string.Empty);
            }

            // 再次清除首尾空格
            fileName = fileName.Trim();
            // 空内容跳过
            if (string.IsNullOrWhiteSpace(fileName)) continue;

            // 大小写去重 保留一个
            bool exists = result.Any(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase));
            // 加入要创建的文件名称
            if (!exists) result.Add(fileName);
        }

        return result;
    }

    /// <summary>
    /// 根据导入的文件名称批量创建空文件。
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public BatchCreateFilesResult CreateFiles(BatchCreateFilesOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.TargetDirectory))
            throw new ArgumentException("保存位置不能为空", nameof(options.TargetDirectory));
        if (string.IsNullOrWhiteSpace(options.Extension))
            throw new ArgumentException("后缀不能为空", nameof(options.Extension));
        if (options.Count <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.Count), "创建数量必须大于 0");
        if (options.FileNames is null || options.FileNames.Count == 0)
            throw new ArgumentException("请先导入文件名称", nameof(options.FileNames));

        // 如果目录不存在就先创建
        Directory.CreateDirectory(options.TargetDirectory);
        // 规范后缀，无论输入 txt 还是 .txt
        string extension = options.Extension.Trim();
        if (!extension.StartsWith('.')) extension = "." + extension;
        // 实际处理数量不能超过导入的数量，导入5个，但是count填10，只能处理前5个
        int actualProcessedCount = Math.Min(options.Count, options.FileNames.Count);
        // 记录创建成功的完整路径
        var createdFilePaths = new List<string>();
        // 记录因为同名已经存在而跳过的完整路径
        var skippedExistingFilePaths = new List<string>();

        // 按顺序处理前 actualProcessedCount 个文件名
        for (int i = 0; i < actualProcessedCount; i++)
        {
            // 取出当前文件名，去掉首尾空格
            string fileName = options.FileNames[i].Trim();
            if (string.IsNullOrWhiteSpace(fileName)) continue; // 跳过

            fileName = Path.GetFileNameWithoutExtension(fileName);
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar.ToString(), string.Empty);
            }

            fileName = fileName.Trim();
            if (string.IsNullOrWhiteSpace(fileName)) continue; // 跳过

            // 完整路径
            string fullPath = Path.Combine(options.TargetDirectory, fileName + extension);
            // 如果文件已存在，跳过
            if (File.Exists(fullPath))
            {
                skippedExistingFilePaths.Add(fullPath);
                continue;
            }

            // 创建空文件
            using (File.Create(fullPath)) { }
            createdFilePaths.Add(fullPath);
        }

        return new BatchCreateFilesResult
        {
            ImportedCount = options.FileNames.Count,
            Requestedcount = options.Count,
            ActualProcessedCount = actualProcessedCount,
            CreatedFilePaths = createdFilePaths,
            SkippedExistingFilePaths = skippedExistingFilePaths
        };
    }

    /// <summary>
    /// 批量创建文件 输入参数
    /// </summary>
    public sealed class BatchCreateFilesOptions
    {
        /// <summary>
        /// 文件保存目录
        /// </summary>
        public string TargetDirectory { get; init; } = string.Empty;

        /// <summary>
        /// 统一文件后缀
        /// </summary>
        public string Extension { get; init; } = ".txt";

        /// <summary>
        /// 创建数量
        /// </summary>
        public int Count { get; init; }

        /// <summary>
        /// 导入的文件名称列表
        /// </summary>
        public List<string> FileNames { get; init; } = new();
    }

    /// <summary>
    /// 批量创建文件的输出结果
    /// </summary>
    public sealed class BatchCreateFilesResult
    {
        /// <summary>
        /// 从Excel实际导入了多少个文件名称
        /// </summary>
        public int ImportedCount { get; init; }

        /// <summary>
        /// 用户要求创建多少个文件
        /// </summary>
        public int Requestedcount { get; init; }

        /// <summary>
        /// 本次实际参与处理的数量
        /// 一般是 min(请求数量，导入数量)
        /// </summary>
        public int ActualProcessedCount { get; init; }

        /// <summary>
        /// 成功创建的文件完整路径列表
        /// </summary>
        public IReadOnlyList<string> CreatedFilePaths { get; init; } = Array.Empty<string>();

        /// <summary>
        /// 因同名已存在而跳过的文件完整路径列表
        /// </summary>
        public IReadOnlyList<string> SkippedExistingFilePaths { get; init; } = Array.Empty<string>();
    }
}
