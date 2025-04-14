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
                var data = serializer.Deserialize<Dictionary<string, Dictionary<string,  JObject>>>(reader);

                var scopes = data!.Select(preScope =>
                {
                    var split = preScope.Key.Split(':');
                    var type = _scopes.GetByName(split[0]);
                    var key = type.Serializer!.Deserialize(split[1]);

                    var projections = preScope.Value
                        .Select(s => KeyValuePair.Create(s.Key, readProjection(s.Value, type, key, _modules.GetByName(s.Key))))
                        .Where(s => s.Value is not null).ToDictionary(s => s.Key, s => s.Value!);

                    return ConfigurationScope.CreateWeak(type, key, projections);
                }).ToArray();

                if (hasExistingValue && existingValue is not null)
                    scopes = scopes.Concat(existingValue.Scopes).ToArray();

                return new ServiceConfiguration(scopes);



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
                            data.Add(property.Key, value.ToObject(property.Value.Type) ?? throw new NullReferenceException());
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
                foreach (var scope in value.Scopes)
                {
                    var property = $"{scope.WeakScopeType.Name}:{scope.WeakScopeType.Serializer!.Serialize(scope.WeakKey)}";
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
