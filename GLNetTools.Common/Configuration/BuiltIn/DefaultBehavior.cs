using System.Collections;

namespace GLNetTools.Common.Configuration.BuiltIn
{
	public class DefaultBehavior : IConfigurationModuleProjectionPrototypeBehavior
	{
		public static DefaultBehavior Instance { get; } = new DefaultBehavior();


		public ConfigurationModuleProjection PerformSum(IEnumerable<ConfigurationModuleProjection> projections)
		{
			var prototype = projections.First().Prototype;
			var result = new Dictionary<string, object>();
			foreach (var property in prototype.Properties)
			{
				var values = projections.Select(s => s.GetWeak(property.Key));
				object unionValue;
				if (property.Value.Type.IsGenericType && property.Value.Type.GetGenericTypeDefinition() == typeof(List<>))
				{
					var preUnionValue = values.Cast<IEnumerable>().Select(s => s.Cast<object>()).SelectMany(s => s);
					var list = (IList)Activator.CreateInstance(property.Value.Type)!;
					foreach (var item in preUnionValue)
						list.Add(item);
					unionValue = list;
				}
				else
				{
					var preUnionValue = values.Where(s => Equals(s, property.Value.DefaultValue) == false).FirstOrDefault();
					preUnionValue ??= property.Value.DefaultValue;
					if (preUnionValue is null)
						throw new Exception($"No value for '{property.Key}' property provided");

					unionValue = preUnionValue;
				}

				result.Add(property.Key, unionValue);
			}

			return prototype.CreateProjection(result);
		}

		public bool ValidateProjection(ConfigurationModuleProjection projection, bool isSummedProjection)
		{
			if (isSummedProjection == false)
				return true;

			var prototype = projection.Prototype;
			foreach (var property in prototype.Properties)
			{
				if (property.Value.IsDefaultValueAllowed == false &&
					Equals(projection.GetWeak(property.Key), property.Value.DefaultValue))
					return false;
			}
			return true;
		}
	}
}
