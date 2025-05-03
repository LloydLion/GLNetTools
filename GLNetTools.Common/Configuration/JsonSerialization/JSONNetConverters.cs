using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Net;

namespace GLNetTools.Common.Configuration.JsonSerialization
{
	public static class JSONNetConverters
	{
		public class IPAddressConverter : JsonConverter<IPAddress>
		{
			public override IPAddress? ReadJson(JsonReader reader, Type objectType, IPAddress? existingValue, bool hasExistingValue, JsonSerializer serializer)
				=> reader.Value is not string value ? null : IPAddress.Parse(value);

			public override void WriteJson(JsonWriter writer, IPAddress? value, JsonSerializer serializer)
				=> writer.WriteValue(value?.ToString());
		}

		public class PhysicalAddressConverter : JsonConverter<PhysicalAddress>
		{
			public override PhysicalAddress? ReadJson(JsonReader reader, Type objectType, PhysicalAddress? existingValue, bool hasExistingValue, JsonSerializer serializer)
				=> reader.Value is not string value ? null : PhysicalAddress.Parse(value);

			public override void WriteJson(JsonWriter writer, PhysicalAddress? value, JsonSerializer serializer)
				=> writer.WriteValue(value?.ToString());
		}


		public static IEnumerable<JsonConverter> ListConverters()
		{
			yield return new IPAddressConverter();
			yield return new PhysicalAddressConverter();
		}

		public static void AddConverters(JsonConverterCollection collections)
		{
			foreach (var item in ListConverters())
				collections.Add(item);
		}

		public static JsonSerializer CreateSerializer()
		{
			var j = new JsonSerializer();
			AddConverters(j.Converters);
			return j;

		}
	}
}
