using System;
using Microsoft.Data.SqlClient;

namespace SqlDemo
{
    /// <summary>
    /// 控制台应用程序入口类。
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 应用程序主入口。
        /// 负责创建数据库连接、输出数据库信息与表结构，并在结束时关闭连接。
        /// </summary>
        /// <param name="args">命令行参数，当前示例未使用。</param>
        static void Main(string[] args)
        {
            // 在 Main 中持有连接引用，便于在 finally 块中统一关闭数据库连接。
            SqlConnection? conn = null;

            try
            {
                // 显式调用连接方法，获取一个已经打开的数据库连接。
                conn = MsSqlConnectionHelper.MSSQL_CENNETING();

                // 如果能执行到这里，说明数据库连接成功。
                Console.WriteLine("数据库连接成功");

                // 输出当前连接到的服务器、数据库名称和服务器时间。
                DatabaseSchemaPrinter.PrintDatabaseInfo(conn);

                // 输出当前数据库中的表以及每张表对应的字段信息。
                DatabaseSchemaPrinter.PrintTablesAndColumns(conn);
            }
            catch (Exception)
            {
                // 捕获所有异常（包括 SqlException）。
                // 当前示例不做额外处理，直接将异常抛给上层。
                throw;
            }
            finally
            {
                // 无论执行成功还是失败，都在 Main 中主动关闭数据库连接。
                conn?.Close();
            }
        }
    }
}
