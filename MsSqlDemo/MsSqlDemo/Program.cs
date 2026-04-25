using System;
using Microsoft.Data.SqlClient;
using SqlDemo.Infrastructure;
using SqlDemo.Models;

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
            //ManualConnection();

            AutomaticConnection();
        }        

        /// <summary>
        /// 手动连接
        /// </summary>
        private static void ManualConnection()
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
                //DatabaseSchemaPrinter.PrintDatabaseInfo(conn);

                // 输出当前数据库中的表以及每张表对应的字段信息。
                //DatabaseSchemaPrinter.PrintTablesAndColumns(conn);

                #region 手动创建数据库连接CRUD
                #region 插入数据
                //var user = new UserInfos
                //{
                //    UserId = SnowflakeIdGenerator.NewId(),
                //    UserName = "zhaoliu",
                //    UserAddr = "Shanghai",
                //    UserRoleId = 1
                //};

                //var rows = Db.Insert(conn, user);
                #endregion
                #region 查询所有数据
                //var GetUserInfo = Db.Get<UserInfos>(conn);

                //Console.WriteLine($"所有数据：");
                //foreach (var item in GetUserInfo)
                //{
                //    foreach (var Values in item.Values)
                //    {
                //        Console.WriteLine($"{Values}");
                //    }
                //}
                #endregion
                #region 根据Id查询
                //var GetUserId = Db.GetById<UserInfos>(conn, 300174492209123328);
                //Console.WriteLine($"300174492209123328 所有数据：");
                //foreach (var item in GetUserId)
                //{
                //    Console.WriteLine($"{item}");
                //}
                #endregion
                #region 根据UserName查询第一条
                //var GetFirst = Db.GetFirstByField<UserInfos>(conn, "UserName", "zhangsan");

                //foreach (var item in GetFirst.Values)
                //{
                //    Console.WriteLine(item);
                //}
                #endregion
                #region 根据UserName查询所有数据
                //var GetAll = Db.GetAllByField<UserInfos>(conn, "UserName", "zhangsan");

                //foreach (var lists in GetAll)
                //{
                //    foreach(var item in lists)
                //    {
                //        Console.WriteLine(item.Value);
                //    }
                //}
                #endregion
                #region 更新数据
                //var user = new UserInfos
                //{
                //    UserId = 300174492209123328,
                //    UserName = "wangwu",
                //    UserAddr = "TangShan",
                //    UserRoleId = 2
                //};

                //var ok = Db.Update(conn, user);

                //Console.WriteLine(ok ? "更新成功" : "更新失败");
                #endregion
                #region 根据Id删除数据
                //var ok = Db.DeleteById<UserInfos>(conn, 300174492209123328);

                //Console.WriteLine(ok ? "删除成功" : "删除失败");
                #endregion
                #endregion
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                // 无论执行成功还是失败，都在 Main 中主动关闭数据库连接。
                conn?.Close();
            }
        }

        /// <summary>
        /// 自动连接
        /// </summary>
        private static void AutomaticConnection()
        {
            try
            {
                var users = AutomaticDB.GetAllByField<UserInfos>(x=>x.UserAddr == "廊坊");

                foreach(var userList in users)
                {
                    foreach(var item in userList)
                    {
                        Console.WriteLine(item.Value);
                    }
                }                

                #region 自动创建数据库连接CRUD

                //var user = new UserInfos
                //{
                //    UserId = SnowflakeIdGenerator.NewId(),
                //    UserName = "山鸡",
                //    UserAddr = "廊坊",
                //    UserRoleId = 2
                //};
                //var isTrue = AutomaticDB.Insert(user);
                //if (isTrue)
                //    Console.WriteLine("添加成功");
                //else
                //    Console.WriteLine("添加失败");

                //foreach (var item in AutomaticDB.GetFirstByField<UserInfos>(x => x.UserId == user.UserId))
                //{
                //    Console.WriteLine(item.Value);
                //}                

                //var users = new UserInfos
                //{
                //    UserId = user.UserId,
                //    UserName = "小山鸡",
                //    UserAddr = "廊坊",
                //    UserRoleId = 2
                //};                

                //Console.WriteLine(AutomaticDB.Update(users) ? "更新成功" : "更新失败");

                //foreach (var item in AutomaticDB.GetFirstByField<UserInfos>(x => x.UserId == user.UserId))
                //{
                //    Console.WriteLine(item.Value);
                //}

                //AutomaticDB.Delete<UserInfos>(user.UserId);

                //Console.WriteLine(AutomaticDB.GetFirstByField<UserInfos>(x => x.UserId == user.UserId) == null ? "已删除" : "未删除");

                #endregion
            }
            catch (Exception)
            {
                Console.WriteLine("连接失败");
            }
            finally                
            {
                //手动关闭连接
            }
        }
    }
}
