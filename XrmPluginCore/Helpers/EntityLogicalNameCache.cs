using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Concurrent;

namespace XrmPluginCore.Helpers;

internal static class EntityLogicalNameCache
{
	private static readonly ConcurrentDictionary<Type, string> LogicalNameCache = new();

	public static string GetLogicalName<T>() where T : Entity, new()
	{
		return LogicalNameCache.GetOrAdd(typeof(T), _ => new T().LogicalName);
	}
}
