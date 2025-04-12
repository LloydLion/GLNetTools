namespace GLNetTools.Common.Configuration
{
	public interface IStaticModelWithMetadata
	{
		public ConfigurationModuleProjectionStaticPrototype Prototype { get; }

		public ConfigurationScopeType? TargetScopeType { get; }
	}

	public interface IStaticModelWithMetadata<TSelf, TKey> : IStaticModelWithMetadata
		where TSelf : class, IStaticModelWithMetadata<TSelf, TKey>
		where TKey : notnull
	{
		public new ConfigurationModuleProjectionStaticPrototype<TSelf> Prototype { get; }

		public new ConfigurationScopeType<TKey>? TargetScopeType { get; }
	}
}
