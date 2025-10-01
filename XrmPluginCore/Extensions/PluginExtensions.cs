using XrmPluginCore.Enums;
using System;
using System.Linq.Expressions;

namespace XrmPluginCore.Extensions
{
    public static class PluginExtensions
    {
        public static EventOperation ToEventOperation(this string x)
        {
            return (EventOperation)Enum.Parse(typeof(EventOperation), x);
        }

        public static string GetMemberName<T>(this Expression<Func<T, object>> lambda)
        {
            MemberExpression body = lambda.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)lambda.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }
}
