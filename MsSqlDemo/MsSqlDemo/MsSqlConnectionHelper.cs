using Microsoft.Data.SqlClient;

namespace SqlDemo
{
    /// <summary>
    /// SQL Server 连接辅助类。
    /// 负责集中维护连接参数并创建数据库连接。
    /// </summary>
    public static class MsSqlConnectionHelper
    {
        /// <summary>
        /// SQL Server 实例地址。
        /// </summary>
        private const string SqlServerDataSource = "127.0.0.1";

        /// <summary>
        /// 默认连接的数据库名称。
        /// </summary>
        private const string SqlServerDatabaseName = "CeShi";

        /// <summary>
        /// 登录 SQL Server 使用的用户名。
        /// </summary>
        private const string SqlServerUserId = "sa";

        /// <summary>
        /// 登录 SQL Server 使用的密码。
        /// </summary>
        private const string SqlServerPassword = "M+123456";

        /// <summary>
        /// 是否启用传输加密。
        /// </summary>
        private const bool SqlServerEncrypt = false;

        /// <summary>
        /// 是否信任服务器证书。
        /// </summary>
        private const bool SqlServerTrustServerCertificate = true;

        /// <summary>
        /// 连接超时时间，单位为秒。
        /// </summary>
        private const int SqlServerConnectTimeoutSeconds = 5;

        /// <summary>
        /// 创建并打开一个 SQL Server 数据库连接。
        /// </summary>
        /// <returns>已经打开的 <see cref="SqlConnection"/> 实例。</returns>
        public static SqlConnection MSSQL_CENNETING()
        {
            // 使用 SqlConnectionStringBuilder 构建连接字符串，
            // 避免手写连接字符串时出现拼接错误。
            var builder = new SqlConnectionStringBuilder
            {
                // 数据源，即 SQL Server 实例的地址。
                DataSource = SqlServerDataSource,

                // 指定连接后默认使用的数据库。
                InitialCatalog = SqlServerDatabaseName,

                // 指定用于登录 SQL Server 的用户名。
                UserID = SqlServerUserId,

                // 指定用于登录 SQL Server 的密码。
                Password = SqlServerPassword,

                // 设置是否启用加密连接。
                Encrypt = SqlServerEncrypt,

                // 设置是否信任服务器证书。
                TrustServerCertificate = SqlServerTrustServerCertificate,

                // 设置建立连接时的超时时间。
                ConnectTimeout = SqlServerConnectTimeoutSeconds
            };

            // 根据构建好的连接字符串创建连接对象。
            var conn = new SqlConnection(builder.ConnectionString);

            // 主动打开连接，并将已打开的连接返回给调用方使用。
            conn.Open();
            return conn;
        }
    }
}
