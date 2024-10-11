// using System.Text.Json;
// using System.Text.Json.Serialization;

// namespace Pulse.Configuration;

// public class ExceptionConverter : JsonConverter<Exception>
// {
//     public override Exception? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//     {
//         return new NotImplementedException();
//     }
//     public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
//     {
//         writer.WriteStartObject();
//         writer.WriteString("Type", value.GetType().Name);
//         writer.WriteString("Message", value.Message);
// 		if (value.Data.Count > 0) {
// 			writer.WriteString("Data", JsonSerializer.Serialize(value.Data, JsonContext.Default.IDictionary));
// 		}
// 		if (value.StackTrace != null) {
// 			writer.WriteString("StackTrace", value.StackTrace);
// 		}
//         if (value.InnerException is { } innerException)
//         {
//             writer.WritePropertyName("InnerException");
//             Write(writer, innerException, options);
//         }
//         writer.WriteEndObject();
//     }
// }