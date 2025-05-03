using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLNetTools.Common.Configuration.JsonSerialization
{
	public class JsonServiceConfigurationSerializer : IJsonServiceConfigurationSerializer
	{
		private readonly JsonSerializer _serializer;


		public JsonServiceConfigurationSerializer(JsonSerializer serializer,
			ConfigurationScopeTypeRegistry scopes,
			ConfigurationModuleRegistry modules
		)
		{
			_serializer = serializer;
			_serializer.Converters.Add(new ServiceConfigurationConverter(scopes, modules));
		}


		public string Serialize(ServiceConfiguration configuration)
		{
			var textWriter = new StringWriter();
			_serializer.Serialize(textWriter, configuration);
			return textWriter.GetStringBuilder().ToString();
		}
		
		public ServiceConfiguration Deserialize(string serializedForm)
		{
			return _serializer.Deserialize<ServiceConfiguration>(new JsonTextReader(new StringReader(serializedForm)))
				?? throw new NullReferenceException();
		}


		private class ServiceConfigurationConverter : JsonConverter<ServiceConfiguration>
		{
			private const string VersionPropertyName = "#Version";
			private const char TypeKeySeparator = ':';


			private readonly ConfigurationScopeTypeRegistry _scopes;
			private readonly ConfigurationModuleRegistry _modules;


			public ServiceConfigurationConverter(ConfigurationScopeTypeRegistry scopes, ConfigurationModuleRegistry modules)
			{
				_scopes = scopes;
				_modules = modules;
			}


			public override ServiceConfiguration? ReadJson(JsonReader reader,
				Type objectType,
				ServiceConfiguration? existingValue,
				bool hasExistingValue,
				JsonSerializer serializer
			)
			{
				var rawData = serializer.Deserialize<Dictionary<string, JToken>>(reader);
				if (rawData is null)
					return null;
				DateTime? version = null;
				if (rawData.TryGetValue(VersionPropertyName, out var preVersion))
				{
					version = preVersion.ToObject<DateTime>();
					rawData.Remove(VersionPropertyName);
				}
				var data = rawData.ToDictionary(s => s.Key, s => s.Value.ToObject<Dictionary<string, JObject>>());

				var scopes = data.Select(preScope =>
				{
					var split = preScope.Key.Split(TypeKeySeparator);
					var type = _scopes.GetByName(split[0]);
					var key = type.Serializer!.Deserialize(split[1]);

					var projections = preScope.Value!
						.Select(s => KeyValuePair.Create(s.Key, readProjection(s.Value, type, key, _modules.GetByName(s.Key))))
						.Where(s => s.Value is not null).ToDictionary(s => s.Key, s => s.Value!);

					return ConfigurationScope.CreateWeak(type, key, projections);
				}).ToArray();

				if (hasExistingValue && existingValue is not null)
					scopes = scopes.Concat(existingValue.Scopes).ToArray();

				return new ServiceConfiguration(scopes) { Version = version };



				ConfigurationModuleProjection? readProjection(JObject preProjection, ConfigurationScopeType type, object key, IConfigurationModule module)
				{
					var prototype = module.ProvidePrototypeFor(type);
					if (prototype is null)
						return null;

					var data = new Dictionary<string, object>();
					foreach (var property in prototype.Properties)
					{
						var value = preProjection[property.Key];
						if (value is not null)
							data.Add(property.Key, value.ToObject(property.Value.Type, serializer) ?? throw new NullReferenceException());
					}
					return prototype.CreateProjection(data);
				}
			}

			public override void WriteJson(JsonWriter writer, ServiceConfiguration? value, JsonSerializer serializer)
			{
				if (value is null)
				{
					writer.WriteNull();
					return;
				}

				writer.WriteStartObject();
				if (value.Version is not null)
				{
					writer.WritePropertyName(VersionPropertyName);
					writer.WriteValue(value.Version.Value);
				}

				foreach (var scope in value.Scopes)
				{
					var property = scope.WeakScopeType.Name + TypeKeySeparator + scope.WeakScopeType.Serializer!.Serialize(scope.WeakKey);
					writer.WritePropertyName(property);
					writer.WriteStartObject();
					foreach (var projection in scope.Projections)
					{
						writer.WritePropertyName(projection.Key);
						writer.WriteStartObject();
						writeProjection(projection.Value);
						writer.WriteEndObject();
					}
					writer.WriteEndObject();
				}
				writer.WriteEndObject();



				void writeProjection(ConfigurationModuleProjection projection)
				{
					foreach (var property in projection.Data)
					{
						writer.WritePropertyName(property.Key);
						serializer.Serialize(writer, property.Value.Value);
					}
				}
			}
		}
	}
}
