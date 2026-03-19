namespace AircraftViewer.Models;

public class Coordinate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Coordinate(double latitude, double longitude)
    {
        Longitude  = longitude;
        Latitude = latitude;
    }

    public override string ToString()
    {
        return $"{Latitude},{Longitude}";
    }
}