using System;

namespace SqlDemo
{
    /// <summary>
    /// 自定义映射名称
    /// </summary>
    /// <param name="name"></param>
    // AttributeUsage 用来限制这个特性能贴在什么位置，以及它的使用规则。
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Property,   // 允许写在 类 属性 上
        AllowMultiple = false, //同一个类或者属性上只能出现一次 [Name]
        Inherited = true // 如果用于类，子类可以继承
        )]
    public sealed class MyNameAttribute : Attribute
    {
        /// <summary>
        /// 表/字段 映射名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 初始化一个实例
        /// </summary>
        /// <param name="name"></param>
        public MyNameAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// 自定义主键标记特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property,AllowMultiple = false)]
    public class MyKeyAttribute : Attribute
    {

    }
}