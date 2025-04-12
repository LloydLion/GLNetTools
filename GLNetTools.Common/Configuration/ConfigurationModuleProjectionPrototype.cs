namespace GLNetTools.Common.Configuration
{
	public class ConfigurationModuleProjectionPrototype
	{
		private readonly IConfigurationModuleProjectionPrototypeBehavior _behavior;

		public IReadOnlyDictionary<string, ConfigurationProperty> Properties { get; }

		public IConfigurationModule Module { get; }


		public ConfigurationModuleProjectionPrototype(
			IReadOnlyDictionary<string, ConfigurationProperty> properties,
			IConfigurationModuleProjectionPrototypeBehavior behavior,
			IConfigurationModule module
		)
		{
			Properties = properties;
			Module = module;
			_behavior = behavior;
		}


		public ConfigurationModuleProjection CreateProjection(IReadOnlyDictionary<string, object?> values)
		{
			var linkedValues = Properties.ToDictionary(s => s.Key, s =>
			{
				if (values.TryGetValue(s.Key, out var value) == false)
					value = s.Value.DefaultValue;
				return new ConfigurationPropertyWithValue(s.Value, value);
			});
			return new ConfigurationModuleProjection(this, linkedValues);
		}

		public ConfigurationModuleProjection SumProjections(IReadOnlyCollection<ConfigurationModuleProjection> values)
		{
			if (values.Any(s => s.Prototype != this))
				throw new ArgumentException("Enable to sum projections of not that prototype", nameof(values));
			return _behavior.PerformSum(values);
		}

		public bool ValidateProjection(ConfigurationModuleProjection projection, bool isSummedProjection)
		{
			if (projection.Prototype != this)
				throw new ArgumentException("Enable to validate projections of not that prototype", nameof(projection));
			return _behavior.ValidateProjection(projection, isSummedProjection);
		}
	}
}
