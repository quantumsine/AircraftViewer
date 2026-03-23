using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AircraftViewer.Models;
using Avalonia.Platform;
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
    
    private static readonly Dictionary<int, string> _iconPaths = new();
    
    static MainWindowViewModel()
    {
        for (int i = 0; i < 25; i++)
        {
            var uri    = new Uri($"avares://AircraftViewer/Assets/PlaneIcons/plane{i:D2}.png");
            var stream = AssetLoader.Open(uri);
        
            // Write to a temp file so Mapsui can load it via file://
            var tempPath = Path.Combine(Path.GetTempPath(), $"plane{i:D2}.png");
            using (var fs = File.Create(tempPath))
                stream.CopyTo(fs);
        
            _iconPaths[i] = $"file://{tempPath}";
            Console.WriteLine($"[Init] Extracted plane{i:D2}.png → {_iconPaths[i]}");
        }
    }
    
    
    [ObservableProperty]
    private ObservableCollection<BasicAircraft> _aircraft = new();
    
    [ObservableProperty]
    private Map _map = CreateMap();

    private static Map CreateMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        map.Layers.Add(CreateAircraftLayer([]));
        map.Navigator.CenterOnAndZoomTo(new MPoint(0, 0), map.Navigator.Resolutions[3]);
        return map;
    }
    
    private static ILayer CreateAircraftLayer(IEnumerable<BasicAircraft> aircraftList)
    {
        var features = new List<IFeature>();

        foreach (var aircraft in aircraftList)
        {
            if (aircraft.Coordinate is null)
            {
                Console.WriteLine($"[Layer] Skipping {aircraft.Icao} null coordinate");
                continue;
            }
            
            var point = SphericalMercator.FromLonLat(
                aircraft.Longitude, 
                aircraft.Latitude
            ).ToMPoint();

            var feature = new PointFeature(point);
            feature["icao"] = aircraft.Icao;

            var heading   = aircraft.Trak;
            var iconIndex =  (int)Math.Round(heading / 14.4) % 25;;

            feature.Styles.Add(new ImageStyle
            {
                Image       = _iconPaths[iconIndex],
                SymbolScale    = 0.5,
                SymbolRotation = heading,
                RotateWithMap  = true,
            });

            features.Add(feature);
        }

        Console.WriteLine($"[Layer] Built aircraft layer with {features.Count} features");

        return new MemoryLayer
        {
            Name     = "Aircraft",
            Features = features,
            Style    = null,
        };
    }
    
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(HasNoSelection))]
    private BasicAircraft? _selectedAircraft;

    // Used to show/hide the "No aircraft selected" placeholder
    public bool HasSelection   => SelectedAircraft is not null;
    public bool HasNoSelection => SelectedAircraft is null;
    
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
            
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var layer    = CreateAircraftLayer(updated);
                var existing = Map.Layers.FindLayer("Aircraft").FirstOrDefault();
                if (existing is not null)
                    Map.Layers.Remove(existing);
                Map.Layers.Add(layer);
                Map.RefreshData();
            });

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Map layer refreshed with {updated.Count} aircraft");
        }
    }

     public void StopTracking() => _cts?.Cancel();
}