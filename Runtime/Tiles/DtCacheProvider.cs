using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class CachedDtTile
{
    public string Geohash;
    public FeatureLayer Layer;
    public string Path;
    public string Format;
    public bool HasLods;
    public byte[] Data;
}

public class DtCacheProvider : ICacheProvider<CachedDtTile>
{
    private string _localCachePath = $"{Application.persistentDataPath}/DT/Cache/";

    public string CacheDir => _cacheDir;
    private string _cacheDir { get { return Path.Combine(_localCachePath, $"DtTiles"); } }

    private float _expirationDays = 7.0f;

    public DtCacheProvider()
    {
        if (!Directory.Exists(_cacheDir)) { Directory.CreateDirectory(_cacheDir); }
    }

    public void SaveToCache(string key, CachedDtTile tile)
    {
        if (!Directory.Exists(_cacheDir)) { Directory.CreateDirectory(_cacheDir); }

        var filepath = Path.Combine(_cacheDir, $"{key}.glb");
        File.WriteAllBytes(filepath, tile.Data);
    }

    public CachedDtTile GetFromCache(string key)
    {
        if (!Directory.Exists(_cacheDir)) { return null; }

        var filepath = Path.Combine(_cacheDir, $"{key}.glb");
        var fileType = "GLB";

        if (!File.Exists(filepath))
        {
            filepath = Path.Combine(_cacheDir, $"{key}.gltf");
            fileType = "GLTF";
        }

        byte[] fileData;
        if (File.Exists(filepath))
        {
            // check if cached tile is expired
            var createdDate = File.GetCreationTime(filepath);
            //MyLogger.LogWarning($"DtCacheProvider :: Cached DT Tile Expired (total days={(createdDate.AddDays(_expirationDays) - DateTime.Now).TotalDays}): {filepath}");
            if ((createdDate.AddDays(_expirationDays) - DateTime.Now).TotalDays > _expirationDays) // DateTime.Now > createdDate.AddDays(_expirationDays))
            {
                MyLogger.LogWarning($"DtCacheProvider :: Cached DT Tile Expired (created={createdDate}): {filepath}");
                return null;
            }

            //fileData = File.ReadAllBytes(filepath);

            var hasLods = fileType == "GLTF" ? Directory.Exists(Path.Combine(_cacheDir, $"{key}", "lods")) : false;
            Debug.Log($"DtCacheProvider :: Loading data for {key}.{fileType} => {filepath} | LODs={hasLods}");

            var result = new CachedDtTile
            {
                Geohash = key,
                Path = filepath,
                Format = fileType,
                HasLods = hasLods
                //Data = fileData
            };

            var feature = key.Split(Path.DirectorySeparatorChar).Last();
            FeatureLayer featureLayer;
            if (Enum.TryParse<FeatureLayer>(feature, out featureLayer))
            {
                result.Layer = featureLayer;
            }

            return result;
        }
        else
        {
            return null;
        }
    }
}
