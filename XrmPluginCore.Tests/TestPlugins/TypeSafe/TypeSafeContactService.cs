using System;

// Import the generated PreImage from the namespace
using XrmPluginCore.Tests.TestPlugins.TypeSafe.PluginImages.TypeSafeContactPlugin.ContactCreatePostOperation;

namespace XrmPluginCore.Tests.TestPlugins.TypeSafe
{
	/// <summary>
	/// Service for TypeSafeContactPlugin that receives PreImage directly
	/// </summary>
	public class TypeSafeContactService
    {
        private readonly TypeSafeContactPlugin plugin;

        public TypeSafeContactService(TypeSafeContactPlugin plugin)
        {
            this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public void HandleCreate(PreImage preImage)
        {
            if (preImage != null)
            {
                _ = preImage.Firstname;
                _ = preImage.Lastname;
                _ = preImage.Mobilephone;
            }

            plugin.SetExecutionResult(preImage);
        }
    }
}
