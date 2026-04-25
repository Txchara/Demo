using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqlDemo
{
    /// <summary>
    /// 基础方法【自动连接】
    /// </summary>
    public class AutomaticDB : BaseHelper
    {
        private static TResult UseConnection<TResult>(Func<SqlConnection,TResult> action)
        {
            using var conn = MsSqlConnectionHelper.MSSQL_CENNETING();
            return action(conn);
        }

        /// <summary>
        ///  新增
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool Insert<T>(T entity) where T : class 
            => UseConnection(conn => Db.Insert(conn, entity));

        /// <summary>
        ///  修改
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool Update<T>(T entity) where T : class
            => UseConnection(conn => Db.Update(conn,entity));

        /// <summary>
        ///  删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool Delete<T>(object id) where T : class 
            => UseConnection(conn => Db.DeleteById<T>(conn,id));

        /// <summary>
        ///  查询表中所有数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<Dictionary<string, object>> GetAll<T>() where T : class 
            => UseConnection(conn => Db.Get<T>(conn));

        /// <summary>
        ///  根据ID查询第一条
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Dictionary<string, object>? GetById<T>(object id) where T : class 
            => UseConnection(conn => Db.GetById<T>(conn,id));

        /// <summary>
        /// 支持【lambda】
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Dictionary<string, object>? GetFirstByField<T>(Expression<Func<T, bool>> predicate) where T : class
            => UseConnection(conn => Db.GetFirstByField<T>(conn, predicate));

        /// <summary>
        /// 根据某个字段查询所有符合条件的数据 支持 【lambda】
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static List<Dictionary<string, object>> GetAllByField<T>(Expression<Func<T, bool>> predicate) where T : class
            => UseConnection(conn => Db.GetAllByField<T>(conn, predicate));
    }
}
