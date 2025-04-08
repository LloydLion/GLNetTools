
namespace GLNetTools.Common.Configuration.BuiltIn
{
	public class DefaultBehavior : IConfigurationModuleProjectionPrototypeBehavior
	{
		public static DefaultBehavior Instance { get; } = new DefaultBehavior();


		public ConfigurationModuleProjection PerformSum(IEnumerable<ConfigurationModuleProjection> projections)
		{
			throw new NotImplementedException();
		}
	}
}
