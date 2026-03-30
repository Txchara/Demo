using System;
using Microsoft.Data.SqlClient;

namespace SqlDemo
{
    /// <summary>
    /// 数据库结构输出辅助类。
    /// 负责查询并打印数据库基本信息、表信息和字段信息。
    /// </summary>
    public static class DatabaseSchemaPrinter
    {
        /// <summary>
        /// 查询当前 SQL Server 实例名称、数据库名称和服务器时间的 SQL 语句。
        /// </summary>
        private const string DatabaseInfoSql = @"
                    SELECT
                        @@SERVERNAME AS ServerName,
                        DB_NAME() AS DatabaseName,
                        GETDATE() AS ServerTime
                    ;";

        /// <summary>
        /// 查询当前数据库所有基础表及其字段信息的 SQL 语句。
        /// </summary>
        private const string TableColumnsSql = @"
                    SELECT
                        TABLE_SCHEMA,
                        TABLE_NAME,
                        COLUMN_NAME,
                        DATA_TYPE,
                        ORDINAL_POSITION
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME IN
                    (
                        SELECT TABLE_NAME
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_TYPE = 'BASE TABLE'
                    )
                    ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION
                    ;";

        /// <summary>
        /// 输出当前连接对应的服务器名称、数据库名称和服务器时间。
        /// </summary>
        /// <param name="conn">已经打开的 SQL Server 连接。</param>
        public static void PrintDatabaseInfo(SqlConnection conn)
        {
            // 使用当前连接创建命令对象，准备查询数据库基本信息。
            using var command = new SqlCommand(DatabaseInfoSql, conn);

            // 执行查询并读取返回的单行结果。
            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                // 连接成功但没有返回任何结果时，给出提示并结束方法。
                Console.WriteLine("连接成功，但没有读取到数据库信息。");
                return;
            }

            // 按列名读取查询结果并输出到控制台。
            Console.WriteLine($"服务器: {reader["ServerName"]}");
            Console.WriteLine($"数据库: {reader["DatabaseName"]}");
            Console.WriteLine($"时间: {reader["ServerTime"]}");
        }

        /// <summary>
        /// 输出当前数据库中所有基础表及其字段信息。
        /// </summary>
        /// <param name="conn">已经打开的 SQL Server 连接。</param>
        public static void PrintTablesAndColumns(SqlConnection conn)
        {
            // 创建查询表结构的命令对象。
            using var command = new SqlCommand(TableColumnsSql, conn);

            // 执行查询并逐行读取所有表字段元数据。
            using var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                // 数据库中没有表结构信息时，输出提示信息。
                Console.WriteLine("当前数据库没有读取到表结构。");
                return;
            }

            // 先输出一个空行，增强控制台显示的可读性。
            Console.WriteLine();
            Console.WriteLine("数据库表和字段:");

            // 记录当前已经输出到哪一张表，用于在表切换时打印表头。
            string? currentTable = null;

            while (reader.Read())
            {
                // 读取当前字段所在的架构名、表名、字段名和字段数据类型。
                var schemaName = reader["TABLE_SCHEMA"].ToString();
                var tableName = reader["TABLE_NAME"].ToString();
                var columnName = reader["COLUMN_NAME"].ToString();
                var dataType = reader["DATA_TYPE"].ToString();
                var fullTableName = $"{schemaName}.{tableName}";

                if (!string.Equals(currentTable, fullTableName, StringComparison.Ordinal))
                {
                    // 当读取到新的表时，先输出表名，再继续输出该表下的字段。
                    currentTable = fullTableName;
                    Console.WriteLine();
                    Console.WriteLine($"表: {currentTable}");
                }

                // 输出当前表下的字段名和对应的数据类型。
                Console.WriteLine($"  字段: {columnName} ({dataType})");
            }
        }
    }
}
