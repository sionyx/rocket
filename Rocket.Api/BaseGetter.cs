using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Rocket.Api
{
    class BaseGetter
    {
        private readonly string _baseAddress;

        internal BaseGetter(string baseAddress)
        {
            _baseAddress = baseAddress;
        }

        internal async Task<T> GetData<T>(string request)
        {
            var client = new HttpClient {BaseAddress = new Uri(_baseAddress)};
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(request);

            return (response.IsSuccessStatusCode) ? await response.Content.ReadAsAsync<T>() : default(T);
        }
    }
}
