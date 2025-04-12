
namespace GLNetTools.Common.Configuration.BuiltIn
{
	public class NullTracker : IConfigurationProvider.ITracker
	{
		public static readonly NullTracker Instance = new();


		private NullTracker()
		{

		}


		public void SetCallback(Action<IConfigurationProvider> callback)
		{

		}

		public void StartTracking()
		{

		}

		public void StopTracking()
		{

		}
	}
}
