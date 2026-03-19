using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AircraftViewer.Models;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;


namespace AircraftViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<BasicAircraft> _aircraft = new();
    
    [ObservableProperty]
    private Map _map = CreateMap();

    private static Map CreateMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        map.Layers.Add(CreateAircraftLayer());
        return map;
    }

    private static ILayer CreateAircraftLayer()
    {
        var features = new List<IFeature>();

        var aircraft = new[]
        {
            (callsign: "DLH123", lat: 50.0379, lon: 8.5622),  // Frankfurt
            (callsign: "BAW456", lat: 51.5074, lon: -0.1278),  // London
            (callsign: "AFR789", lat: 48.8566, lon:  2.3522),  // Paris
        };


        foreach (var (callsign, lat, lon) in aircraft)
        {
            var point = SphericalMercator.FromLonLat(lon, lat).ToMPoint();
            var feature = new PointFeature(point);
            feature["callsign"] = callsign;
            feature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 0.7,
                Fill = new Brush(Color.FromArgb(220, 30, 144, 255)),
                Outline = new Pen(Color.White, 1.5)
            });
            features.Add(feature);
        }

        return new MemoryLayer
        {
            Name = "Aircraft",
            Features = features,
            Style = null,
        };
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(HasNoSelection))]
    private BasicAircraft? _selectedAircraft;

    // Used to show/hide the "No aircraft selected" placeholder
    public bool HasSelection   => SelectedAircraft is not null;
    public bool HasNoSelection => SelectedAircraft is null;

    // Set to false once your real map control is wired up
    public bool IsMapEmpty => true;

    private CancellationTokenSource? _cts;

    public async Task StartTrackingAsync()
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Starting to track...");
        _cts = new CancellationTokenSource();
        var server = new PlaneDataServer("https://opensky-network.org/api/states/all");

        await foreach (var json in server.StreamJSONAsync(_cts.Token))
        {
            var root   = JsonNode.Parse(json);
            var states = root?["states"]?.AsArray();
            if (states is null)
            {
                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] No states in response, skipping");
                continue;
            }

            var updated = new ObservableCollection<BasicAircraft>();
            foreach (var state in states)
            {
                if (state is not JsonArray arr) continue;
                var ac = AircraftSentence.Parse(arr);
                if (ac is not null) updated.Add(ac);
            }
            
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Parsed {updated.Count} aircraft");
            Aircraft = updated;
        }
    }
    public void StopTracking() => _cts?.Cancel();
}