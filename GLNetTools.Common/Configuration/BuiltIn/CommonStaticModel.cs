namespace GLNetTools.Common.Configuration.BuiltIn
{
	public abstract class CommonStaticModel<TSelf, TKey> : IStaticModelWithMetadata<TSelf, TKey>
		where TSelf : CommonStaticModel<TSelf, TKey>
		where TKey : notnull
	{
		private readonly ConfigurationModuleProjectionStaticPrototype<TSelf> _prototype;
		private readonly ConfigurationScopeType<TKey>? _targetScopeType;


		public CommonStaticModel(ConfigurationModuleProjectionStaticPrototype<TSelf> prototype, ConfigurationScopeType<TKey>? targetScopeType = null)
		{
			_prototype = prototype;
			_targetScopeType = targetScopeType;
		}


		ConfigurationModuleProjectionStaticPrototype<TSelf> IStaticModelWithMetadata<TSelf, TKey>.Prototype => _prototype;

		ConfigurationModuleProjectionStaticPrototype IStaticModelWithMetadata.Prototype => _prototype;

		ConfigurationScopeType<TKey>? IStaticModelWithMetadata<TSelf, TKey>.TargetScopeType => _targetScopeType;

		ConfigurationScopeType? IStaticModelWithMetadata.TargetScopeType => _targetScopeType;
	}
}
