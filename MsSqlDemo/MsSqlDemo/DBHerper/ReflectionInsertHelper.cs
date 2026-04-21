using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace SqlDemo
{    
    public class Db : BaseHelper
    {
        /// <summary>
        /// 插入一条数据
        /// </summary>
        /// <typeparam name="T">任意类型</typeparam>
        /// <param name="conn">数据库连接对象</param>
        /// <param name="Enttiy">要插入的数据对象（实体）</param>
        /// <returns>【true】/【false】</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Insert<T>(SqlConnection conn, T Enttiy) where T : class
        {
            try
            {
                //确保连接已打开
                if (conn == null)
                    return false;
                //常规非空校验
                if (Enttiy == null)
                    return false;

                //反射
                var type = typeof(T);
                var TableName = type.Name;

                //获取所有可读属性
                var properties = type
                                 .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(x => x.CanRead)
                                 .ToList();

                if (properties.Count == 0)
                    return false;

                var columnNames = new List<string>();
                var parameterNames = new List<string>();

                using var command = conn.CreateCommand();

                foreach (var property in properties)
                {
                    var columnName = property.Name;
                    var parameterName = "@" + property.Name;
                    var Value = property.GetValue(Enttiy) ?? DBNull.Value;

                    columnNames.Add($"[{columnName}]");
                    parameterNames.Add(parameterName);
                    command.Parameters.AddWithValue(parameterName, Value);
                }

                //构建 SQL（注意 VALUES）
                command.CommandText =
                    $"Insert Into [dbo].[{TableName}]({string.Join(",", columnNames)})" +
                    $"VALUES ({string.Join(",", parameterNames)})";

                var result = command.ExecuteNonQuery();

                return result > 0 ? true : false; ;
            }
            catch (Exception)
            {
                return false;                
            }
        }

        /// <summary>
        ///  查询所有数据
        /// </summary>
        /// <typeparam name="T">需要查询的表名</typeparam>
        /// <param name="conn">数据库连接对象</param>
        /// <returns>数据存在就以字典形式返回，如果数据不存在则返回空</returns>
        public static List<Dictionary<string,object>> Get<T>(SqlConnection conn) where T : class
        {
            //确保连接已打开
            if (conn == null)
                return new List<Dictionary<string, object>>();

            var TableName = GetMyTableName<T>();

            var result = new List<Dictionary<string, object?>>();
            var Sql = $"Select * From [dbo].[{TableName}]";

            using var command = new SqlCommand(Sql, conn);
            using var Reader = command.ExecuteReader();

            while (Reader.Read())
            {
                var row = new Dictionary<string, object>();

                for (int i = 0; i < Reader.FieldCount; i++)
                {
                    var ColumnName = Reader.GetName(i);
                    var Value = Reader.IsDBNull(i) ? null : Reader.GetValue(i);
                    row[ColumnName] = Value;
                }
                result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// 根据Id查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Dictionary<string,object>? GetById<T>(SqlConnection conn, object id) where T : class
        {
            if (conn == null || id == null)
                return null;

            // 通过实体类型读取表名
            var TableName = GetMyTableName<T>();

            // 通过实体类型推断主键字段名
            var IdColumnName = GetIdColumnName<T>();

            // 构造按主键查询的 SQL
            var sql = $"SELECT * FROM [dbo].[{TableName}] WHERE [{IdColumnName}] = @id";

            using var command = new SqlCommand(sql, conn);

            // 参数化查询，避免 SQL 注入
            command.Parameters.AddWithValue("@id",id);

            using var reader = command.ExecuteReader();

            // 如果没有查到数据，返回 null
            if (!reader.Read())
                return null;

            var result = new Dictionary<string, object>();

            // 将当前行转换成字典返回
            for (int i = 0; i< reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                result[columnName] = value;
            }

            return result;
        }

        /// <summary>
        /// 根据某个字段查询数据【不支持lambda】
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="conn">数据库连接对象</param>
        /// <param name="columnName">列名</param>
        /// <param name="value">该列需要查询第一条的值</param>
        /// <returns>只返回查询到的第一条</returns>
        public static Dictionary<string, object> GetFirstByField<T>(SqlConnection conn, string columnName, object value) where T : class
        {
            if (conn == null || string.IsNullOrWhiteSpace(columnName)) 
                return new Dictionary<string, object>();

            var type = typeof(T);
            var tableName = GetMyTableName<T>();
            var idColumnName = GetIdColumnName<T>();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (properties.Length == 0)
                throw new InvalidOperationException($"{type.Name}里没有公共属性啊？");

            var matchedProperty = properties.FirstOrDefault(
                    p => string.Equals(GetColumnName(p),columnName,StringComparison.OrdinalIgnoreCase)
                );

            if (matchedProperty == null)
                throw new InvalidOperationException($"{type.Name}里找不到列名:{columnName}");

            var realColumnName = GetColumnName(matchedProperty);

            using var command = conn.CreateCommand();

            if(value == null || value == DBNull.Value)
            {
                command.CommandText = $@"SELECT TOP 1 *
                                         FROM [dbo].[{tableName}]
                                         WHERE [{realColumnName}] IS NULL
                                         ORDER BY [{idColumnName}] ASC";
            }
            else
            {
                command.CommandText = $@"SELECT TOP 1 *
                                         FROM [dbo].[{tableName}]
                                         WHERE [{realColumnName}] = @value
                                         ORDER BY [{idColumnName}] ASC";

                command.Parameters.AddWithValue("@value", value);
            }

            using var reader = command.ExecuteReader();

            if(!reader.Read())
                return new Dictionary<string, object>();

            var result = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var currentColumnName = reader.GetName(i);
                var currentValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                result[currentColumnName] = currentValue;
            }
            return result;
        }

        /// <summary>
        /// 根据某个字段查询数据【不支持lambda】
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="conn">数据库连接对象</param>
        /// <param name="columnName">列名</param>
        /// <param name="value">该列需要查询所有的数据</param>
        /// <returns>返回查询到的所有数据</returns>
        public static List<Dictionary<string,object>> GetAllByField<T>(SqlConnection conn, string columnName, object value) where T : class
        {
            if (conn == null || string.IsNullOrWhiteSpace(columnName))
                return new List<Dictionary<string, object>>();

            var type = typeof(T);
            var tableName = GetMyTableName<T>();
            var idColumnName = GetIdColumnName<T>();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (properties.Length == 0)
                throw new InvalidOperationException($"{type.Name}里没有公共属性啊？");

            var matchedProperty = properties.FirstOrDefault(
                    p => string.Equals(GetColumnName(p), columnName, StringComparison.OrdinalIgnoreCase)
                );

            if (matchedProperty == null)
                throw new InvalidOperationException($"{type.Name}里找不到列名:{columnName}");

            var realColumnName = GetColumnName(matchedProperty);

            using var command = conn.CreateCommand();

            command.Parameters.Clear();

            if (value == null || value == DBNull.Value)
            {
                command.CommandText = $@"
                    SELECT *
                    FROM [dbo].[{tableName}]
                    WHERE [{realColumnName}] IS NULL
                    ORDER BY [{idColumnName}] ASC";
            }
            else
            {
                command.CommandText = $@"
                    SELECT *
                    FROM [dbo].[{tableName}]
                    WHERE [{realColumnName}] = @value
                    ORDER BY [{idColumnName}] ASC";

                command.Parameters.Add(new SqlParameter("@value", value));
            }

            using var reader = command.ExecuteReader();

            var result = new List<Dictionary<string, object>>();

            while (reader.Read())
            {
                var row = new Dictionary<string, object>(reader.FieldCount);

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var colName = reader.GetName(i);
                    var colValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[colName] = colValue;
                }

                result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// 根据主键更新一条数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="conn">数据库连接对象</param>
        /// <param name="entity">要更新的实体对象，必须带主键值</param>
        /// <returns>更新成功返回 true，失败返回 false</returns>
        public static bool Update<T>(SqlConnection conn, T entity) where T : class
        {
            try
            {
                // 基本判空
                if (conn == null || entity == null)
                    return false;

                // 获取表名
                var tableName = GetMyTableName<T>();

                // 获取主键列名
                // 这里会优先找 [MyKey]，找不到再按 UserInfos -> UserId 这种规则推断
                var idColumnName = GetIdColumnName<T>();

                // 获取当前实体的所有公共实例属性
                var type = typeof(T);
                var properties = type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead)
                    .ToList();

                if (properties.Count == 0)
                    return false;

                // 找到“主键列名”对应的属性
                // 为什么这里要同时比属性名和映射列名：
                // 因为属性上可能加了 [MyName("xxx")]，数据库字段名和属性名可能不一致
                var idProperty = properties.FirstOrDefault(
                    p => string.Equals(GetColumnName(p), idColumnName, StringComparison.OrdinalIgnoreCase)
                      || string.Equals(p.Name, idColumnName, StringComparison.OrdinalIgnoreCase)
                );

                if (idProperty == null)
                    return false;

                // 取出主键值，主键为空就无法定位要更新哪一行
                var idValue = idProperty.GetValue(entity);
                if (idValue == null || idValue == DBNull.Value)
                    return false;

                // 拼接 SET 子句
                var setClauses = new List<string>();

                using var command = conn.CreateCommand();

                foreach (var property in properties)
                {
                    // 主键字段不能出现在 SET 里，只能放到 WHERE 中
                    if (property == idProperty)
                        continue;

                    // 获取数据库里的真实列名
                    var columnName = GetColumnName(property);

                    // 参数名加前缀，避免和 @id 冲突
                    var parameterName = "@p_" + property.Name;

                    // 属性值为空时写入 DBNull.Value
                    var value = property.GetValue(entity) ?? DBNull.Value;

                    // 生成类似：[UserName] = @p_UserName
                    setClauses.Add($"[{columnName}] = {parameterName}");

                    // 添加参数，避免 SQL 注入
                    command.Parameters.AddWithValue(parameterName, value);
                }

                // 如果除了主键外没有别的字段可更新，就直接返回 false
                if (setClauses.Count == 0)
                    return false;

                // 主键参数单独添加，供 WHERE 使用
                command.Parameters.AddWithValue("@id", idValue);

                // 生成最终 SQL
                command.CommandText = $@"
                                      UPDATE [dbo].[{tableName}]
                                      SET {string.Join(", ", setClauses)}
                                      WHERE [{idColumnName}] = @id";

                // 执行更新
                var result = command.ExecuteNonQuery();

                // 影响行数大于 0 说明更新成功
                return result > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 根据主键 Id 删除一条数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="conn">数据库连接对象</param>
        /// <param name="id">主键值</param>
        /// <returns>删除成功返回 true，失败返回 false</returns>
        public static bool DeleteById<T>(SqlConnection conn, object id) where T : class
        {
            try
            {
                if (conn == null || id == null)
                    return false;

                var tableName = GetMyTableName<T>();

                // 获取主键列名
                // 这里会优先找 [MyKey]，找不到再按 UserInfos -> UserId 这种规则推断
                var idColumnName = GetIdColumnName<T>();

                // 创建命令对象
                using var command = conn.CreateCommand();

                // 删除 SQL
                command.CommandText = $@"
                                      DELETE FROM [dbo].[{tableName}]
                                      WHERE [{idColumnName}] = @id";

                // 避免 SQL 注入
                command.Parameters.AddWithValue("@id", id);

                // 执行删除
                var result = command.ExecuteNonQuery();

                // 影响行数大于 0 说明删除成功
                return result > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
