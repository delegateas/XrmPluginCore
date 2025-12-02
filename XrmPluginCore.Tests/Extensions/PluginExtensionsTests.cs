using FluentAssertions;
using System;
using System.Linq.Expressions;
using Xunit;
using XrmPluginCore.Extensions;
using XrmPluginCore.Tests.Context.BusinessDomain;

namespace XrmPluginCore.Tests.Extensions;

public class PluginExtensionsTests
{
	[Fact]
	public void GetMemberName_WithValidPropertyExpression_ReturnsPropertyName()
	{
		// Arrange
		Expression<Func<Account, object>> expression = a => a.Name;

		// Act
		var result = expression.GetMemberName();

		// Assert
		result.Should().Be("Name");
	}

	[Fact]
	public void GetMemberName_WithValueTypeProperty_ReturnsPropertyName()
	{
		// Arrange
		// Value types get wrapped in a Convert (UnaryExpression) by the compiler
		Expression<Func<Account, object>> expression = a => a.AccountId;

		// Act
		var result = expression.GetMemberName();

		// Assert
		result.Should().Be("AccountId");
	}

	[Fact]
	public void GetMemberName_WithNullableValueTypeProperty_ReturnsPropertyName()
	{
		// Arrange
		Expression<Func<Account, object>> expression = a => a.Revenue;

		// Act
		var result = expression.GetMemberName();

		// Assert
		result.Should().Be("Revenue");
	}

	[Fact]
	public void GetMemberName_WithConstantExpression_ThrowsInvalidCastException()
	{
		// Arrange
		// A constant expression is not a member access expression and cannot be cast to UnaryExpression
		Expression<Func<Account, object>> expression = _ => "constant";

		// Act
		Action act = () => expression.GetMemberName();

		// Assert
		// The current implementation attempts to cast to UnaryExpression when not a MemberExpression,
		// which throws InvalidCastException for constant expressions
		act.Should().Throw<InvalidCastException>();
	}

	[Fact]
	public void GetMemberName_WithMethodCallExpression_ThrowsInvalidCastException()
	{
		// Arrange
		// A method call expression returns a string (reference type), which doesn't need boxing conversion,
		// so it's not wrapped in a UnaryExpression. The direct cast to UnaryExpression throws InvalidCastException.
		Expression<Func<Account, object>> expression = a => a.ToString();

		// Act
		Action act = () => expression.GetMemberName();

		// Assert
		// The current implementation attempts to cast to UnaryExpression when not a MemberExpression,
		// which throws InvalidCastException for method call expressions
		act.Should().Throw<InvalidCastException>();
	}

	[Fact]
	public void GetMemberName_WithBoxedMethodCallExpression_ThrowsArgumentException()
	{
		// Arrange
		// GetHashCode() returns int (value type), which requires boxing to object,
		// wrapping the MethodCallExpression in a UnaryExpression (Convert).
		// The ArgumentException is thrown when trying to cast the Operand (MethodCallExpression) to MemberExpression.
		Expression<Func<Account, object>> expression = a => a.GetHashCode();

		// Act
		Action act = () => expression.GetMemberName();

		// Assert
		act.Should().Throw<ArgumentException>()
			.WithMessage("Cannot extract member name from expression*")
			.And.ParamName.Should().Be("lambda");
	}
}
