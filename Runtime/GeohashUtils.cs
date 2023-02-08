using SturfeeVPS.Core;
using NGeoHash;

namespace Sturfee.DigitalTwin
{
    public class GeohashUtils
    {
        public static string EncodeToGeohash(double latitude, double longitude, int length = 7)
        {
            return GeoHash.Encode(latitude, longitude, length);
        }

        public static GeoLocation DecodeFromGeohash()
        {
            
            var location = GeoHash.Decode(geoHash);
            return new GeoLocation
            {
                Latitude = location.Coordinates.Lat,
                Longitude = location.Coordinates.Lon,
                Altitude = 0
            };
        }
    }
}