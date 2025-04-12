using GLNetTools.Common.Configuration.BuiltIn;

namespace GLNetTools.Common.Configuration
{
	public static class IConfigurationBuilderExtensions
	{
		public static void AddProjection<TKey, TStaticModel>(
			this IConfigurationBuilderAccessor builder,
			ConfigurationScopeType<TKey> type,
			TKey key,
			ConfigurationModuleProjectionStaticPrototype<TStaticModel> prototype,
			TStaticModel model
		)
			where TKey : notnull
			where TStaticModel : class
		{
			builder.AddProjection(type, key, prototype.CreateProjectionStatic(model));
		}

		public static void AddProjection<TKey, TStaticModel>(
			this IConfigurationBuilderAccessor builder,
			ConfigurationScopeType<TKey> type,
			TKey key,
			TStaticModel model
		)
			where TKey : notnull
			where TStaticModel : class, IStaticModelWithMetadata
		{
			builder.AddProjection(type, key, model.Prototype.CreateProjectionStaticWeak(model));
		}

		public static void AddProjection<TStaticModel>(
			this IConfigurationBuilderAccessor builder,
			object key,
			TStaticModel model
		)
			where TStaticModel : class, IStaticModelWithMetadata
		{
			if (model.TargetScopeType is null)
				throw new InvalidOperationException("This static model has no target scope type, specify it explicitly");
			builder.AddProjection(model.TargetScopeType, key, model.Prototype.CreateProjectionStaticWeak(model));
		}

		public static void AddProjection<TKey, TStaticModel>(
			this IConfigurationBuilderAccessor builder,
			TKey key,
			TStaticModel model
		)
			where TKey : notnull
			where TStaticModel : class, IStaticModelWithMetadata<TStaticModel, TKey>
		{
			builder.AddProjection((object)key, model);
		}

		public static void AddProjection<TStaticModel>(
			this IConfigurationBuilderAccessor builder,
			TStaticModel model
		)
			where TStaticModel : class, IStaticModelWithMetadata<TStaticModel, NoScopeKey>
		{
			builder.AddProjection(NoScopeKey.Instance, model);
		}


		public static void EnsureScopeCreated<TKey>(
			this IConfigurationBuilderAccessor builder,
			ConfigurationScopeType<TKey> type,
			TKey key
		)
			where TKey : notnull
		{
			builder.EnsureScopeCreated(type, key);
		}
	}
}
