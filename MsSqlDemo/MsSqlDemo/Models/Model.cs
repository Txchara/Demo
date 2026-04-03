namespace SqlDemo.Models
{
    /// <summary>
    /// 对应数据库表 dbo.RoleInfos。
    /// </summary>
    public class RoleInfos
    {
        public int RoleId { get; set; }

        public string? RoleName { get; set; }

        public int? UserRoleId { get; set; }
    }

    /// <summary>
    /// 对应数据库表 dbo.UserInfos。
    /// </summary>
    public class UserInfos
    {
        public int UserId { get; set; }

        public string? UserName { get; set; }

        public string? UserAddr { get; set; }

        public int? UserRoleId { get; set; }
    }

    /// <summary>
    /// 对应数据库表 dbo.UserRoleInfos。
    /// </summary>
    public class UserRoleInfos
    {
        public int UserRoleId { get; set; }

        public int? UserId { get; set; }

        public int? RoleId { get; set; }
    }
}
