namespace GLNetTools.Common.Configuration
{
	public interface IConfigurationModuleProjectionPrototypeBehavior
	{
		public ConfigurationModuleProjection PerformSum(IEnumerable<ConfigurationModuleProjection> projections);
	}

	public interface IConfigurationModuleProjectionPrototypeBehavior<TStaticModel>
		where TStaticModel : class
	{
		public TStaticModel PerformSum(IEnumerable<TStaticModel> projections, ConfigurationModuleProjectionPrototype prototype);
	}

	public static class ConfigurationModuleProjectionPrototypeBehaviorExtensions
	{
		public static IConfigurationModuleProjectionPrototypeBehavior CastToWeakTyped<TStaticModel>(
			this IConfigurationModuleProjectionPrototypeBehavior<TStaticModel> self
		) where TStaticModel : class => new CastClass<TStaticModel>(self);

		public static IConfigurationModuleProjectionPrototypeBehavior<TStaticModel> CastToStrongTyped<TStaticModel>(
			this IConfigurationModuleProjectionPrototypeBehavior self
		) where TStaticModel : class => new CastClass<TStaticModel>(self);

		private class CastClass<TStaticModel> : IConfigurationModuleProjectionPrototypeBehavior, IConfigurationModuleProjectionPrototypeBehavior<TStaticModel> where TStaticModel : class
		{
			private readonly IConfigurationModuleProjectionPrototypeBehavior? _weak = null;
			private readonly IConfigurationModuleProjectionPrototypeBehavior<TStaticModel>? _strong = null;


			public CastClass(IConfigurationModuleProjectionPrototypeBehavior? weak)
			{
				_weak = weak;
			}

			public CastClass(IConfigurationModuleProjectionPrototypeBehavior<TStaticModel>? strong)
			{
				_strong = strong;
			}


			ConfigurationModuleProjection IConfigurationModuleProjectionPrototypeBehavior.PerformSum(IEnumerable<ConfigurationModuleProjection> projections)
			{
				if (_weak is not null)
					return _weak.PerformSum(projections);

				var prototype = projections.First().Prototype;
				var resultModel = _strong!.PerformSum(projections.Select(prototype.MapProjectionToStaticModel).OfType<TStaticModel>(), prototype);
				return prototype.CreateProjection(resultModel);
			}

			TStaticModel IConfigurationModuleProjectionPrototypeBehavior<TStaticModel>.PerformSum(IEnumerable<TStaticModel> projections, ConfigurationModuleProjectionPrototype prototype)
			{
				if (_strong is not null)
					return _strong.PerformSum(projections, prototype);

				var result = _weak!.PerformSum(projections.Select(prototype.CreateProjection));
				return (TStaticModel)prototype.MapProjectionToStaticModel(result);
			}
		}
	}
}
