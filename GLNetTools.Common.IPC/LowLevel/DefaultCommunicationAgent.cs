using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Buffers;
using System.Text;

namespace GLNetTools.Common.IPC.LowLevel
{
	internal class DefaultCommunicationAgent : ICommunicationAgent
	{
		private readonly byte[] _buffer = new byte[10241];
		private readonly MemoryStream _bufferStream;
		private readonly BsonDataWriter _binaryJsonWriter;
		private Lazy<JsonSerializer> _json;


		public DefaultCommunicationAgent()
		{
			_bufferStream = new MemoryStream(_buffer);
			_binaryJsonWriter = new BsonDataWriter(_bufferStream);
			_json = new(CreateJsonSerializer);
		}


#if DEBUG
		public string ProtocolName => "GLNetTools.Default/DEBUG";
#else
		public string ProtocolName => "GLNetTools.Default";
#endif

		public int ProtocolVersion => 0;


		public LowLevelMessage CreateHandshake()
		{
			_buffer[0] = (byte)ProtocolVersion;
			return new LowLevelMessage(LLMessageType.Handshake, _buffer[0..1]);
		}

		public bool CheckHandshake(LowLevelMessage message)
		{
			return message.Type == LLMessageType.Handshake &&
				message.Bytes.Length == 1 &&
				message.Bytes[0] == ProtocolVersion;
		}

		public LowLevelMessage CreateDropConnection() => new(LLMessageType.DropConnection, []);

		public LowLevelMessage CreateEventUpdateMessage(string objectName, PropertyObject updatedValues)
		{
			var len = Encoding.ASCII.GetBytes(objectName, _buffer.AsSpan(1));
			_buffer[0] = (byte)len;
			if (len > byte.MaxValue)
				throw new Exception("Bad message"); //TODO: make normal exception type

			_bufferStream.Position = len + 1;
			_json.Value.Serialize(_binaryJsonWriter, updatedValues);
			var totalLen = (int)_bufferStream.Position - 1;

			return new LowLevelMessage(LLMessageType.Event, _buffer.AsSpan(0, totalLen));
		}

		private JsonSerializer CreateJsonSerializer()
		{

			var jsonSerializer = new JsonSerializer();
			jsonSerializer.Converters.Add(new PropertyObjectConverter());
			jsonSerializer.Converters.Add(new PropertyObjectValueConverter());
			return jsonSerializer;
		}

		public bool ShouldDisconnectByTimeout(DateTime lastMessage)
		{
			return DateTime.UtcNow - lastMessage >= TimeSpan.FromSeconds(10);
		}


		private class PropertyObjectConverter : JsonConverter<PropertyObject>
		{
			public override PropertyObject? ReadJson(JsonReader reader, Type objectType, PropertyObject? existingValue, bool hasExistingValue, JsonSerializer serializer)
			{
				var baseDic = serializer.Deserialize<Dictionary<string, PropertyObjectValue>>(reader);
				return baseDic is null ? null : new PropertyObject(baseDic);
			}

			public override void WriteJson(JsonWriter writer, PropertyObject? value, JsonSerializer serializer)
			{
				serializer.Serialize(writer, value?.GetEnumerator());
			}
		}

		private class PropertyObjectValueConverter : JsonConverter<PropertyObjectValue>
		{
			public override PropertyObjectValue ReadJson(JsonReader reader, Type objectType, PropertyObjectValue existingValue, bool hasExistingValue, JsonSerializer serializer)
			{
				return reader.TokenType switch
				{
					JsonToken.StartObject => new PropertyObjectValue(serializer.Deserialize<PropertyObject>(reader)!),
					JsonToken.Integer => new PropertyObjectValue(reader.ReadAsInt32()!.Value),
					JsonToken.Float => new PropertyObjectValue(reader.ReadAsDouble()!.Value),
					JsonToken.String => new PropertyObjectValue(reader.ReadAsString()!),
					JsonToken.Boolean => new PropertyObjectValue(reader.ReadAsBoolean()!.Value),
					JsonToken.Null => new PropertyObjectValue(),
					_ => throw new JsonException("Invalid token type for PropertyObjectValue")
				};
			}

			public override void WriteJson(JsonWriter writer, PropertyObjectValue value, JsonSerializer serializer)
			{
				switch (value.Type)
				{
					case PropertyObjectValue.PrimitiveType.Null:
						writer.WriteNull();
						break;
					case PropertyObjectValue.PrimitiveType.Integer:
						writer.WriteValue(value.AsInt());
						break;
					case PropertyObjectValue.PrimitiveType.Float:
						writer.WriteValue(value.AsFloat());
						break;
					case PropertyObjectValue.PrimitiveType.Boolean:
						writer.WriteValue(value.AsBoolean());
						break;
					case PropertyObjectValue.PrimitiveType.String:
						writer.WriteValue(value.AsString());
						break;
					case PropertyObjectValue.PrimitiveType.NestedObject:
						serializer.Serialize(writer, value.AsNestedObject());
						break;
				}
			}
		}
	}
}
