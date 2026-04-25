using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SqlDemo
{
    public class BaseHelper
    {

        /// <summary>
        /// 获取表名[MyName("UserInfos")]
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <returns></returns>
        protected static string GetMyTableName<T>() where T : class
        {
            var type = typeof(T);
            var nameAttribute = type.GetCustomAttribute<MyNameAttribute>();

            if (!string.IsNullOrWhiteSpace(nameAttribute?.Name))
                return nameAttribute.Name;

            return type.Name;
        }

        /// <summary>
        /// 获取属性对应的字段名
        /// </summary>
        /// <param name="property">属性信息</param>
        /// <returns>字段名</returns>
        protected static string GetColumnName(PropertyInfo property)
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
        protected static string GetIdColumnName<T>() where T : class
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
                    throw new InvalidOperationException($"{type.Name} 里没有 public 实例属性。");

                var keyProperties = properties
                    .Where(p => p.GetCustomAttribute<MyKeyAttribute>() != null)
                    .ToList();

                if (keyProperties.Count > 1)
                    throw new InvalidOperationException($"{type.Name} 上存在多个 [MyKey]。");

                if (keyProperties.Count == 1)
                    return GetColumnName(keyProperties[0]);

                var entityName = type.Name;

                if (entityName.EndsWith("Infos", StringComparison.OrdinalIgnoreCase))
                    entityName = entityName.Substring(0, entityName.Length - "Infos".Length);
                else if (entityName.EndsWith("Info", StringComparison.OrdinalIgnoreCase))
                    entityName = entityName.Substring(0, entityName.Length - "Info".Length);
                else if (entityName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                    entityName = entityName.Substring(0, entityName.Length - 1);

                var expectedIdName = entityName + "Id";

                var matchedProperty = properties.FirstOrDefault(
                    p => string.Equals(p.Name, expectedIdName, StringComparison.OrdinalIgnoreCase)
                      || string.Equals(GetColumnName(p), expectedIdName, StringComparison.OrdinalIgnoreCase));

                if (matchedProperty != null)
                    return GetColumnName(matchedProperty);

                matchedProperty = properties.FirstOrDefault(
                    p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(GetColumnName(p), "Id", StringComparison.OrdinalIgnoreCase));

                if (matchedProperty != null)
                    return GetColumnName(matchedProperty);

                throw new InvalidOperationException($"{type.Name} 找不到主键列。");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        /// <summary>
        ///  Expression Visitor（表达式树解析器）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        protected static string BuildWhere<T>(Expression<Func<T, bool>> predicate, SqlCommand command) where T : class
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var parameterIndex = 0;

            string Parse(Expression expression)
            {
                while (expression is UnaryExpression unary &&
                       (unary.NodeType == ExpressionType.Convert ||
                        unary.NodeType == ExpressionType.ConvertChecked))
                {
                    expression = unary.Operand;
                }

                if (expression is BinaryExpression binary)
                {
                    if (binary.NodeType == ExpressionType.AndAlso || binary.NodeType == ExpressionType.OrElse)
                    {
                        var leftSql = Parse(binary.Left);
                        var rightSql = Parse(binary.Right);
                        var logic = binary.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                        return $"({leftSql} {logic} {rightSql})";
                    }

                    Expression left = binary.Left;
                    Expression right = binary.Right;

                    while (left is UnaryExpression leftUnary &&
                           (leftUnary.NodeType == ExpressionType.Convert ||
                            leftUnary.NodeType == ExpressionType.ConvertChecked))
                    {
                        left = leftUnary.Operand;
                    }

                    while (right is UnaryExpression rightUnary &&
                           (rightUnary.NodeType == ExpressionType.Convert ||
                            rightUnary.NodeType == ExpressionType.ConvertChecked))
                    {
                        right = rightUnary.Operand;
                    }

                    MemberExpression? propertyExpression = null;
                    Expression? valueExpression = null;

                    var propertyOnLeft = false;

                    if (left is MemberExpression leftMember &&
                        leftMember.Member is PropertyInfo &&
                        leftMember.Expression is ParameterExpression)
                    {
                        propertyExpression = leftMember;
                        valueExpression = right;
                        propertyOnLeft = true;
                    }
                    else if (right is MemberExpression rightMember &&
                             rightMember.Member is PropertyInfo &&
                             rightMember.Expression is ParameterExpression)
                    {
                        propertyExpression = rightMember;
                        valueExpression = left;
                        propertyOnLeft = false;
                    }
                    else
                    {
                        throw new NotSupportedException($"不支持的比较表达式：{binary}");
                    }

                    var propert = (PropertyInfo)propertyExpression.Member;
                    var columnName = GetColumnName(propert);

                    object? value;
                    if (valueExpression is ConstantExpression constant)
                    {
                        value = constant.Value;
                    }
                    else
                    {
                        var boxedValue = Expression.Convert(valueExpression, typeof(object));
                        value = Expression.Lambda<Func<object?>>(boxedValue).Compile().Invoke();
                    }

                    /*
                     * SQL 里的 null 不能写成 = null / <> null，
                     * 必须翻译成 IS NULL / IS NOT NULL。
                     */
                    if (value == null)
                    {
                        return binary.NodeType switch
                        {
                            ExpressionType.Equal => $"[{columnName}] IS NULL",
                            ExpressionType.NotEqual => $"[{columnName}] IS NOT NULL",
                            _ => throw new NotSupportedException("只有 == 和 != 支持 null 判断。")
                        };
                    }

                    /*
                     * 把 C# 比较运算符映射成 SQL 运算符。
                     * 如果属性在右边，就要把方向翻过来。
                     * 例如：18 < x.Age => [Age] > @p0
                     */
                    var sqlOperator = binary.NodeType switch
                    {
                        ExpressionType.Equal => "=",
                        ExpressionType.NotEqual => "<>",
                        ExpressionType.GreaterThan => propertyOnLeft ? ">" : "<",
                        ExpressionType.GreaterThanOrEqual => propertyOnLeft ? ">=" : "<=",
                        ExpressionType.LessThan => propertyOnLeft ? "<" : ">",
                        ExpressionType.LessThanOrEqual => propertyOnLeft ? "<=" : ">=",
                        _ => throw new NotSupportedException($"不支持的比较运算：{binary.NodeType}")
                    };

                    /* 所有值都走参数化，避免把真实值直接拼进 SQL。 */
                    var parameterName = $"@p{parameterIndex++}";
                    command.Parameters.AddWithValue(parameterName, value);
                    return $"[{columnName}] {sqlOperator} {parameterName}";
                }

                /*
                 * 处理字符串实例方法调用：
                 * x.UserName.Contains("a")
                 * x.UserName.StartsWith("a")
                 * x.UserName.EndsWith("a")
                 */
                if (expression is MethodCallExpression call &&
                    call.Object is MemberExpression member &&
                    member.Member is PropertyInfo property &&
                    member.Expression is ParameterExpression)
                {
                    if (call.Arguments.Count != 1)
                        throw new NotSupportedException($"不支持的方法参数数量：{call}");

                    /* 方法参数可能是常量，也可能来自外部变量。 */
                    object? argValue;
                    if (call.Arguments[0] is ConstantExpression argConstant)
                    {
                        argValue = argConstant.Value;
                    }
                    else
                    {
                        var boxedValue = Expression.Convert(call.Arguments[0], typeof(object));
                        argValue = Expression.Lambda<Func<object?>>(boxedValue).Compile().Invoke();
                    }

                    /*
                     * 把字符串方法映射成 SQL LIKE 模式。
                     * Contains   -> %abc%
                     * StartsWith -> abc%
                     * EndsWith   -> %abc
                     */
                    var text = argValue?.ToString() ?? string.Empty;
                    var likeValue = call.Method.Name switch
                    {
                        nameof(string.Contains) => $"%{text}%",
                        nameof(string.StartsWith) => $"{text}%",
                        nameof(string.EndsWith) => $"%{text}",
                        _ => throw new NotSupportedException($"不支持的方法：{call.Method.Name}")
                    };

                    var parameterName = $"@p{parameterIndex++}";
                    command.Parameters.AddWithValue(parameterName, likeValue);

                    var columnName = GetColumnName(property);
                    return $"[{columnName}] LIKE {parameterName}";
                }

                /* 走到这里说明当前表达式类型还不在翻译器支持范围内。 */
                throw new NotSupportedException($"不支持的表达式类型：{expression.NodeType}");
            }

            /* 从 lambda 主体开始解析。 */
            return Parse(predicate.Body);
        }
    }
}
