namespace GLNetTools.Common.Configuration
{
	public record struct ConfigurationProperty(Type Type, bool IsDefaultValueAllowed = false, object? DefaultValue = null);
}
