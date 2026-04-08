using SqlDemo;

namespace SqlDemo.Models
{
    /// <summary>
    /// 对应数据库表 dbo.RoleInfos。
    /// </summary>
    [MyName("RoleInfos")]
    public class RoleInfos
    {
        public long RoleId { get; set; }

        public string? RoleName { get; set; }

        public long? UserRoleId { get; set; }
    }

    /// <summary>
    /// 对应数据库表 dbo.UserInfos。
    /// </summary>
    [MyName("UserInfos")]
    public class UserInfos
    {
        [MyKey]
        public long UserId { get; set; }

        public string? UserName { get; set; }

        public string? UserAddr { get; set; }

        public long? UserRoleId { get; set; }
    }

    /// <summary>
    /// 对应数据库表 dbo.UserRoleInfos。
    /// </summary>
    [MyName("UserRoleInfos")]
    public class UserRoleInfos
    {
        public long UserRoleId { get; set; }

        public long? UserId { get; set; }

        public long? RoleId { get; set; }
    }
}
