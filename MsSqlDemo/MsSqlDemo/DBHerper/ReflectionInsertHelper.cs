using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace SqlDemo
{    
    public static class ReflectionInsertHelper
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
        /// <typeparam name="T">任意类型</typeparam>
        /// <param name="conn">数据库连接对象</param>
        /// <param name="TableName">需要查询的表名</param>
        /// <returns>数据存在就以字典形式返回，如果数据不存在则返回空</returns>
        public static List<Dictionary<string,object>> Get<T>(SqlConnection conn, string TableName)
        {
            //确保连接已打开
            if (conn == null)
                return new List<Dictionary<string, object>>();
            //常规非空校验
            if (string.IsNullOrWhiteSpace(TableName))
                return new List<Dictionary<string, object>>();

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
    }
}