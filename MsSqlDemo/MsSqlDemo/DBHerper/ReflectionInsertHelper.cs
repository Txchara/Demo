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
        /// 插入一条数据【true】/【false】
        /// </summary>
        /// <typeparam name="T">任意类型</typeparam>
        /// <param name="conn">数据库连接对象</param>
        /// <param name="Enttiy">要插入的数据对象（实体）</param>
        /// <returns></returns>
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
    }
}