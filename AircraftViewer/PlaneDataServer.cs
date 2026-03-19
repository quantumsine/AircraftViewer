using System;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    
    public async IAsyncEnumerable<string> StreamJSONAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Fetching from OpenSky...");
        
            HttpResponseMessage response;
            try
            {
                response = await _client.GetAsync(_baseUrl, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] ERROR fetching: {ex.Message}");
                await Task.Delay(10_000, ct);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] HTTP {(int)response.StatusCode} — retrying in 10s");
                await Task.Delay(10_000, ct);
                continue;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] OK — {json.Length} bytes received");
            yield return json;

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Waiting 10s before next poll...");
            await Task.Delay(10_000, ct);
        }

        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Tracking stopped.");
    }
   
}