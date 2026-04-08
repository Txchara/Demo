using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace SqlDemo
{    
    public static class Db
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

        #region 获取表名/字段名

        /// <summary>
        /// 获取表名[MyName("UserInfos")]
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <returns></returns>
        private static string GetMyTableName<T>() where T : class
        {
            // 获取当前泛型类型 T 的 Type 对象
            var type = typeof(T);
            // 尝试获取类型上标注的 NameAttribute 特性[MyName("UserInfos")]
            var nameAttribute = type.GetCustomAttribute<MyNameAttribute>();

            // 判断特性中的 MyName 是否为空，如果不为空则使用 MyName 中的名称，如果没有 MyName ，则使用类名作为表名
            if (!string.IsNullOrWhiteSpace(nameAttribute?.Name))
                return nameAttribute.Name;

            return type.Name;
        }

        /// <summary>
        /// 获取属性对应的字段名
        /// </summary>
        /// <param name="property">属性信息</param>
        /// <returns>字段名</returns>
        private static string GetColumnName(PropertyInfo property)
        {
            var nameAttribute = property.GetCustomAttribute<MyNameAttribute>();

            if (!string.IsNullOrWhiteSpace(nameAttribute?.Name))
                return nameAttribute.Name;

            return property.Name;
        }

        /// <summary>
        /// 根据实体类型推断主键字段名称（列名）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>主键对应的列名</returns>
        private static string GetIdColumnName<T>() where T : class
        {
            try
            {
                // 获取泛型类型 T 的 Type 对象
                var type = typeof(T);

                // 获取该类型的所有公共实例属性
                // BindingFlags.Public：只获取 public 属性
                // BindingFlags.Instance：只获取实例属性（排除静态属性）
                var properties = type.GetProperties(
                        BindingFlags.Public | BindingFlags.Instance
                    );

                if (properties.Length == 0)
                    throw new InvalidOperationException($"{type.Name}里哪他妈有公共属性啊？");

                var keyProperties = properties
                    .Where(
                        p => p.GetCustomAttribute<MyKeyAttribute>() != null
                    ).ToList();

                if (keyProperties.Count > 1)
                    throw new InvalidOperationException($"{type.Name}上写那么多[Mykey]干鸡毛");
                if (keyProperties.Count == 1)
                    return GetColumnName(keyProperties[0]);


                // 推断默认主键名
                //通常会有如下命名习惯
                // UserInfos     -> UserId
                // RoleInfos     -> RoleId
                // UserRoleInfos -> UserRoleId

                var entityName = type.Name;
                // 如果类名以 Infos 结尾，就去掉 Infos   UserInfos -> User
                if (entityName.EndsWith("Infos", StringComparison.OrdinalIgnoreCase))
                    entityName = entityName.Substring(0, entityName.Length - "Infos".Length);
                // 如果类名以 Info 结尾，就去掉 Info   UserInfo -> User
                else if (entityName.EndsWith("Info", StringComparison.OrdinalIgnoreCase))
                    entityName = entityName.Substring(0, entityName.Length - "Info".Length);
                // 如果类名只是普通复数 s 结尾，也去掉最后一个 s   Users -> User
                else if (entityName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                    entityName = entityName.Substring(0, entityName.Length - 1);

                // 拼出一个预期的主键字段名。
                // 例如：
                // User      -> UserId
                // Role      -> RoleId
                // UserRole  -> UserRoleId
                var expectedIdName = entityName + "Id";

                // 优先查找“预期主键名”对应的属性
                // 这里会同时比较两种名称
                // 属性本身的名称 p.Name
                // 属性通过 [MyName] 映射后的数据库字段名 GetColumnName(p)
                // 而且比较时使用 StringComparison.OrdinalIgnoreCase，
                // 所以不会区分大小写：
                // Id / ID / id / iD 都能匹配
                var matchedProperty = properties.FirstOrDefault(
                        p => string.Equals(
                            p.Name,
                            expectedIdName,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                            string.Equals(
                                GetColumnName(p),
                                expectedIdName,
                                StringComparison.OrdinalIgnoreCase
                        )
                    );

                // 如果找到了符合“XxxId”规则的属性，
                // 直接返回其映射后的数据库列名。
                if (matchedProperty != null)
                    return GetColumnName(matchedProperty);

                // 如果还没找到，
                // 再尝试匹配一个最通用的主键名：Id。
                //
                // 同样地，这里也同时比较：
                // 属性名是否叫 Id
                // 映射后的字段名是否叫 Id
                // 并且大小写不敏感
                matchedProperty = properties.FirstOrDefault(
                        p => string.Equals(
                            p.Name,
                            "Id",
                            StringComparison.OrdinalIgnoreCase)
                        ||
                            string.Equals(
                                GetColumnName(p),
                                "Id",
                                StringComparison.OrdinalIgnoreCase
                        )
                    );

                // 如果找到了名为 Id 的属性或字段映射，也直接返回。
                if (matchedProperty != null)
                    return GetColumnName(matchedProperty);

                throw new InvalidOperationException($"{type.Name} 主键字段在哪？嗯？为什么不写? 你要炸数据库？");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        #endregion
    }
}