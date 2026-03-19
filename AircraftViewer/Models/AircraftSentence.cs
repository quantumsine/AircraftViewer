using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using AircraftViewer.Models;

namespace AircraftViewer.Models;

public static class AircraftSentence
{

    public static BasicAircraft? Parse(JsonArray state)
    {
        try
        {
            var icao     = state[0]?.GetValue<string>() ?? "";
            var callsign = state[1]?.GetValue<string>()?.Trim() ?? "";
            var lon      = state[5]?.GetValue<double>();
            var lat      = state[6]?.GetValue<double>();
            var speed    = state[9]?.GetValue<double>() ?? 0;
            var track    = state[10]?.GetValue<double>() ?? 0;
            var epoch    = state[3]?.GetValue<long>() ?? 0;

            if (lat is null || lon is null) return null; // on ground / no position

            var posTime = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;
            var coord   = new Coordinate(lat.Value, lon.Value);

            return new BasicAircraft(icao, callsign, posTime, coord, speed, track) {PosTime = posTime};
        }
        catch { return null; }
    }
    
}