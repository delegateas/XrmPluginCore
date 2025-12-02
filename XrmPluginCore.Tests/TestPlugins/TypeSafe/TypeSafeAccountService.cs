using System;

// Import the generated PreImage/PostImage from the namespace
using XrmPluginCore.Tests.TestPlugins.TypeSafe.PluginRegistrations.TypeSafeAccountPlugin.AccountUpdatePreOperation;

namespace XrmPluginCore.Tests.TestPlugins.TypeSafe;

/// <summary>
/// Service for TypeSafeAccountPlugin that receives images directly
/// </summary>
public class TypeSafeAccountService(TypeSafeAccountPlugin plugin)
{
	private readonly TypeSafeAccountPlugin plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

	public void HandleUpdate(PreImage preImage, PostImage postImage)
	{
		if (preImage != null)
		{
			_ = preImage.Name;
			_ = preImage.AccountNumber;
			_ = preImage.Revenue;
		}

		plugin.SetExecutionResult(preImage, postImage);
	}
}
