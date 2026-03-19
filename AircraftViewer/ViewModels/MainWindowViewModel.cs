using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AircraftViewer.Models;
using CommunityToolkit.Mvvm.ComponentModel;


namespace AircraftViewer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<BasicAircraft> _aircraft = new();

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
        _cts = new CancellationTokenSource();
        var server = new PlaneDataServer("https://opensky-network.org/api/states/all");

        await foreach (var json in server.StreamJSONAsync(_cts.Token))
        {
            var root   = JsonNode.Parse(json);
            var states = root?["states"]?.AsArray();
            if (states is null) continue;

            var updated = new ObservableCollection<BasicAircraft>();
            foreach (var state in states)
            {
                if (state is not JsonArray arr) continue;
                var ac = AircraftSentence.Parse(arr);
                if (ac is not null) updated.Add(ac);
            }

            Aircraft = updated;
        }
    }

    public void StopTracking() => _cts?.Cancel();
}