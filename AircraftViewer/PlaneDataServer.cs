using System;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Data;

namespace AircraftViewer;

public class PlaneDataServer
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public PlaneDataServer(string baseUrl)
    {
        _baseUrl = baseUrl;

        var handler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, error) => true
        };
        
        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.95 Safari/537.11");
    }

    public async IAsyncEnumerable<string> StreamJSONAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            string json = "";
            try
            {
                json = await _client.GetStringAsync(_baseUrl, ct);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                json = $"Error: {e.Message}";
            }
            yield return json;
            await Task.Delay(5000, ct);
        }
    }
}