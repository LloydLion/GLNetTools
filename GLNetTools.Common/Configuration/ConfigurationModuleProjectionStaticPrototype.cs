using System.Reflection;

namespace GLNetTools.Common.Configuration
{
	public abstract class ConfigurationModuleProjectionStaticPrototype : ConfigurationModuleProjectionPrototype
	{
		public ConfigurationModuleProjectionStaticPrototype(
			IReadOnlyDictionary<string, ConfigurationProperty> properties,
			IConfigurationModuleProjectionPrototypeBehavior behavior,
			StaticModelLayout layout,
			IConfigurationModule module
		) : base(properties, behavior, module)
		{
			Layout = layout;
		}


		public StaticModelLayout Layout { get; }


		public ConfigurationModuleProjection CreateProjectionStaticWeak(object staticModel)
		{
			if (staticModel.GetType() != Layout.StaticType)
				throw new ArgumentException("Invalid type of model", nameof(staticModel));

			return CreateProjection(Properties.Keys.ToDictionary(s => s, s => Layout.Mapping[s].GetValue(staticModel) ?? throw new NullReferenceException()));
		}

		public object MapProjectionToStaticModelWeak(ConfigurationModuleProjection projection)
		{
			if (projection.Prototype != this)
				throw new ArgumentException("Not my projection", nameof(projection));

			var model = Activator.CreateInstance(Layout.StaticType) ?? throw new NullReferenceException();
			foreach (var property in Properties.Keys)
				Layout.Mapping[property].SetValue(model, projection.GetWeak(property));
			return model;
		}

		public static ConfigurationModuleProjectionStaticPrototype Create(Type staticModel, IConfigurationModuleProjectionPrototypeBehavior behavior, IConfigurationModule module)
		{
			var modelWithDefaultValues = Activator.CreateInstance(staticModel);
			var fields = staticModel.GetFields(BindingFlags.Instance | BindingFlags.Public);
			var properties = fields.ToDictionary(s => s.Name, s =>
			{
				var defaultValue = s.GetValue(modelWithDefaultValues);
				return new ConfigurationProperty(s.FieldType, IsDefaultValueAllowed: false, DefaultValue: defaultValue);
			});

			var layout = new StaticModelLayout(staticModel, fields.ToDictionary(s => s.Name));

			return (ConfigurationModuleProjectionStaticPrototype)typeof(ConfigurationModuleProjectionStaticPrototype<>)
				.MakeGenericType(staticModel)
				.GetConstructors().Single().Invoke([properties, behavior, layout, module]);
		}


		public record StaticModelLayout(Type StaticType, IReadOnlyDictionary<string, FieldInfo> Mapping);
	}

	public class ConfigurationModuleProjectionStaticPrototype<TStaticModel> : ConfigurationModuleProjectionStaticPrototype where TStaticModel : class
	{
		public ConfigurationModuleProjectionStaticPrototype(IReadOnlyDictionary<string, ConfigurationProperty> properties,
			IConfigurationModuleProjectionPrototypeBehavior behavior,
			StaticModelLayout layout,
			IConfigurationModule module
		) : base(properties, behavior, layout, module) { }


		public ConfigurationModuleProjection CreateProjectionStatic(TStaticModel staticModel) => CreateProjectionStaticWeak(staticModel);

		public TStaticModel MapProjectionToStaticModel(ConfigurationModuleProjection projection) => (TStaticModel)MapProjectionToStaticModelWeak(projection);


		public static ConfigurationModuleProjectionStaticPrototype<TStaticModel> Create(IConfigurationModuleProjectionPrototypeBehavior<TStaticModel> behavior, IConfigurationModule module)
			=> (ConfigurationModuleProjectionStaticPrototype<TStaticModel>)Create(typeof(TStaticModel), behavior.CastToWeakTyped(), module);

		public static ConfigurationModuleProjectionStaticPrototype<TStaticModel> Create(IConfigurationModuleProjectionPrototypeBehavior weakBehavior, IConfigurationModule module)
			=> (ConfigurationModuleProjectionStaticPrototype<TStaticModel>)Create(typeof(TStaticModel), weakBehavior, module);
	}
}
