namespace XrmPluginCore.Interfaces.CustomApi
{
	public interface IRequestParameter : IParameter
	{
		bool IsOptional { get; }
	}
}
