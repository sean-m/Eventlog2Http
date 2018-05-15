using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventLog2Http
{
    public class ForwardWithHttp : IHandleEventEntry
    {
        // TODO Refactor all of this out into some IHandler type thing,
        // it shouldn't care about http or anything, just hand it a playload
        // and cancellation token.
        static HttpClient _client = new HttpClient();
        static Uri _uri;

        public ForwardWithHttp(string uri) {
            _uri = new Uri(uri);
            _client.BaseAddress = new Uri($"{_uri.Scheme}://{_uri.Host}:{_uri.Port}");
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void HandleEntry<T>(T input, CancellationToken Token) {
            HandleEntryAsync(input, Token);
        }

        public async void HandleEntryAsync<T>(T input, CancellationToken Token) {
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, _uri?.AbsolutePath ?? "");
            message.Content = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json");

            await _client.SendAsync(message, Token)
                .ContinueWith(response => { Console.WriteLine($"Response: {response.Result}"); });
        }
    }
}
