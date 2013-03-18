namespace RestBox.Domain.Services
{
    public interface IJsonSerializer
    {
        string ToJsonString(object objectToSerialize);
        T FromJsonString<T>(string jsonString);
    }
}
