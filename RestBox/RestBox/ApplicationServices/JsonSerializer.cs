using Newtonsoft.Json;

namespace RestBox.Domain.Services
{
    public class JsonSerializer : IJsonSerializer
    {
        public string ToJsonString(object objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize, Formatting.Indented);
        }

        public T FromJsonString<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}
