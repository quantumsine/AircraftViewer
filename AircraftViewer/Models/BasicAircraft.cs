using System;

namespace AircraftViewer.Models;

public class BasicAircraft
{
    public string Icao { get; set; }
    public string Callsign { get; set; }
    public required DateTime PosTime { get; set; } 
    public Coordinate Coordinate { get; set; }
    
    public double Latitude => Coordinate.Latitude;
    public double Longitude => Coordinate.Longitude;
    public double Speed { get;  set; }
    public double Trak { get; set; }
    
    public BasicAircraft(string icao, string callsign, DateTime posTime, Coordinate coordinate, double speed,
        double trak)
    {
        Icao = icao;
        Callsign = callsign;
        PosTime = posTime;
        Coordinate = coordinate;
        Speed = speed;
        Trak = trak;
    }
    
    
    public override string ToString() => 
        $"BasicAircraft [icao={Icao}, callsign={Callsign}, posTime={PosTime}, coordinate={Coordinate}, speed={Speed}, trak={Trak}]";
}