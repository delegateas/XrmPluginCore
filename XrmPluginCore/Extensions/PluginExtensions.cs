using XrmPluginCore.Enums;
using System;
using System.Linq.Expressions;

namespace XrmPluginCore.Extensions;

public static class PluginExtensions
{
	public static EventOperation ToEventOperation(this string x)
	{
		return (EventOperation)Enum.Parse(typeof(EventOperation), x);
	}

	public static string GetMemberName<T>(this Expression<Func<T, object>> lambda)
	{
		if (lambda.Body is not MemberExpression body)
		{
			var ubody = (UnaryExpression)lambda.Body;
			body = ubody.Operand as MemberExpression
				?? throw new ArgumentException(
					$"Cannot extract member name from expression. Expected property access but got: {lambda.Body.GetType().Name}.",
					nameof(lambda));
		}

		return body.Member.Name;
	}
}
