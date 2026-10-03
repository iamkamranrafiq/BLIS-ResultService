
namespace Bioreference.LIS
{
    public interface IWebClientManager
    {

        string GetResponse(string url);

        string PostRequest(string url, string[] paramNames, string[] paramValues);

        string PostRequestUri(string url);

        string PostRequestWithJson(string url, string json);
    }
}