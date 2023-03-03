//using GLTFast;
using Newtonsoft.Json;
using NGeoHash;
using Sturfee.XRCS.Config;
using Sturfee.XRCS.Utils;
using SturfeeVPS.Core;
using SturfeeVPS.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityGLTF;
using UnityGLTF.Loader;
using DebugWatch = System.Diagnostics.Stopwatch;

namespace Sturfee.DigitalTwin.Tiles
{
    public enum DtTileErrorCode
    {
        NotFound,
        DownloadError,
        ImportError,
        Other
    }

    public delegate void DtTileLoadEvent(float progress, int _errorCount);
    public delegate void DtTileLoadError(DtTileErrorCode code, string errorMessage);

    [Serializable]
    public class LoadedTile
    {
        public GameObject Tile;
        public string Geohash;
    }

    [Serializable]
    public class TileInfo
    {
        public string Path;
        public bool HasLods;
    }

    [Serializable]
    public class CacheInfo
    {
        public GeoLocation Location;
        public List<TileInfo> CachedTilesPath;
        public List<String> NonCachedGeohashes;
    }

    public class DtTileLoader : SimpleSingleton<DtTileLoader>
    {
        public List<string> unsmoothList = new List<string> {
            $"{FeatureLayer.Concrete}",
            $"{FeatureLayer.Freeway}",
            $"{FeatureLayer.Greenspace}",
            $"{FeatureLayer.TrainTrack}",
            $"{FeatureLayer.Road}",
            $"{FeatureLayer.Sidewalk}",
            $"{FeatureLayer.RoadMarking}",
            //$"{FeatureLayer.Building}",
            //$"{FeatureLayer.Terrain}"
        };

        public List<string> GroundLayerList = new List<string> {
            $"{FeatureLayer.Concrete}",
            $"{FeatureLayer.Freeway}",
            $"{FeatureLayer.Greenspace}",
            $"{FeatureLayer.TrainTrack}",
            $"{FeatureLayer.Road}",
            $"{FeatureLayer.Sidewalk}",
            $"{FeatureLayer.Terrain}"
        };

        public int CurrentDetailLevel = 0;
        private int _lastDetailLevel = -1;

        public bool GenerateMipMapsForTextures = true;

        private GameObject _parent;

        private int _geohashLength = 7;
        private List<LoadedTile> _loadedTiles;

        public DtTileLoader()
        {

        }

        /// <summary>
        /// Checks if all the tiles (including neighbors) of this geocache are already downloaded and saved in cache
        /// </summary>
        /// <param name="geoHash"></param>
        /// <returns></returns>
        public bool AvailableInCache(string geoHash, double radius = 150)
        {
            var location = GeoHash.Decode(geoHash);
            return AvailableInCache(location.Coordinates.Lat, location.Coordinates.Lon, radius);
        }

        /// <summary>
        /// Checks if all the tiles needed for this location(3x3) are already downloaded and saved in cache
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public bool AvailableInCache(double latitude, double longitude, double radius = 150)
        {
            var cacheInfo = GetCacheInfo(latitude, longitude, radius);

            if (cacheInfo.NonCachedGeohashes.Any())
            {
                MyLogger.Log($" DtTileLoader :: Geohashes {JsonConvert.SerializeObject(cacheInfo.NonCachedGeohashes)} not available in cache");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Download all the tiles for this geohash( including neighbors)
        /// </summary>
        /// <param name="geoHash"></param>
        /// <param name="progress"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public async Task<List<string>> DownloadTilesAt(string geoHash, double radius = 150, DtTileLoadEvent progress = null, DtTileLoadError onError = null)
        {
            var location = GeoHash.Decode(geoHash);
            return await DownloadTilesAt(location.Coordinates.Lat, location.Coordinates.Lon, radius, progress, onError);
        }

        /// <summary>
        /// Download all the tiles needed for this locatio (3x3)
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="progress"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public async Task<List<string>> DownloadTilesAt(double latitude, double longitude, DtTileLoadEvent progress = null, DtTileLoadError onError = null)
        {
            return await DownloadTilesAt(latitude, longitude, 150, progress, onError);
        }

        /// <summary>
        /// Download all the tiles needed for this locatio (3x3)
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="progress"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public async Task<List<string>> DownloadTilesAt(double latitude, double longitude, double radius, DtTileLoadEvent progress = null, DtTileLoadError onError = null)
        {
            MyLogger.Log($"DtTileLoader :: Downloading tiles at {latitude}, {longitude}, radius={radius}");

            var filePaths = new List<string>();
            var geoHash = GeoHash.Encode(latitude, longitude, _geohashLength);
            var cacheInfo = GetCacheInfo(latitude, longitude, radius);

            var nonCachedGeoHashes = cacheInfo.NonCachedGeohashes;
            if (!nonCachedGeoHashes.Any())
            {
                MyLogger.Log(" DtTileLoader :: No tile to download");
                progress?.Invoke(1, 0);
                return filePaths;
            }

            var tileProvider = IOC.Resolve<ITileProvider>();

            ServicePointManager.DefaultConnectionLimit = 10000;

            var downloadTasks = new List<Task<string>>();

            foreach (var _geohash in cacheInfo.NonCachedGeohashes)
            {
                var item = await tileProvider.FetchTileUrl(_geohash);
                if (item != null)
                {
                    downloadTasks.Add(tileProvider.DownloadTileLayers(_geohash, item.fileUrl));
                }
            }
            MyLogger.Log($"DtTileLoader :: Waiting for tiles to finish download...");
            DebugWatch downloadWatch = DebugWatch.StartNew();
            try
            {
                await DownloadAllTasks(downloadTasks, filePaths, progress);
                MyLogger.Log($" Timer :: DtTileLoader :: Download tiles time : {downloadWatch.ElapsedMilliseconds} ms");
            }
            catch (Exception e)
            {
                onError?.Invoke(DtTileErrorCode.DownloadError, e.Message);
                MyLogger.LogError($"DtTileLoader :: ERROR => {e.Message}");
                throw;
            }

            MyLogger.LogError($"DtTileLoader :: filepaths = {JsonConvert.SerializeObject(filePaths)}");
            return filePaths;
        }

        /// <summary>
        /// Load all the tiles needed for this location from cache into scene
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="progress"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public async Task<GameObject> LoadTilesAt(double latitude, double longitude, double radius = 150, DtTileLoadEvent progress = null, DtTileLoadError onError = null)
        {
            MyLogger.Log($"DigitalTwinTileLoader :: Trying to load DT Tiles for loc={latitude},{longitude}");

            var tileGeohash = GeoHash.Encode(latitude, longitude, _geohashLength);
            await LoadTilesAt(tileGeohash, radius, progress, onError);

            return _parent;
            //await Task.Delay(2000);
        }

        /// <summary>
        /// Load all the tiles for this geohash (including neighbors) from cache into scene
        /// </summary>
        /// <param name="tileGeohash"></param>
        /// <param name="progress"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public async Task<GameObject> LoadTilesAt(string tileGeohash, double radius = 150, DtTileLoadEvent progress = null, DtTileLoadError onError = null)
        {         
            var location = GeoHash.Decode(tileGeohash);

            _parent = new GameObject("DigitalTwinTiles"); 
            _parent.transform.position = Vector3.zero;
            _loadedTiles = new List<LoadedTile>();

            var cacheInfo = GetCacheInfo(location.Coordinates.Lat, location.Coordinates.Lon, radius);
            var cachedTiles = cacheInfo.CachedTilesPath;
            var tilesToLoad = new List<TileInfo>(cachedTiles);

            // check if anything needed to be downloaded
            var nonCachedGeohashes = cacheInfo.NonCachedGeohashes;
            if (nonCachedGeohashes.Any())
            {
                var downloadedTiles = await DownloadTilesAt(
                    tileGeohash,
                    radius,
                    (downloadProgress, error) => 
                    {
                        // Split total progess into 2 parts => downloadProgress, importProgress
                        progress?.Invoke(downloadProgress / 2, error);
                    }, 
                    (code, errorMsg) => 
                    {
                        onError?.Invoke(DtTileErrorCode.DownloadError, errorMsg);
                    }
                );

                //tilesToLoad.AddRange(downloadedTiles);

                // set up the cached tiles again
                cacheInfo = GetCacheInfo(location.Coordinates.Lat, location.Coordinates.Lon, radius);
                cachedTiles = cacheInfo.CachedTilesPath;
                tilesToLoad = new List<TileInfo>(cachedTiles);
            }            

            // Import
            float currentCount = 0;
            int errorCount = 0;
            float totalCount = tilesToLoad.Count;
            foreach (var tileToLoad in tilesToLoad)
            {
                try
                {
                    // set LOD textures -> overwrite referenced texture files using lods folder
                    if (tileToLoad.HasLods && CurrentDetailLevel != _lastDetailLevel)
                    {
                        var basePath = Path.Combine(Path.GetDirectoryName(tileToLoad.Path), Path.GetFileNameWithoutExtension(tileToLoad.Path));
                        if (!Directory.Exists(basePath))
                        {
                            MyLogger.LogError($"Directory does not exist: {basePath}");
                            continue;
                        }
                        var lodPath = Path.Combine(basePath, "lods", $"LOD{CurrentDetailLevel}");
                        if (!Directory.Exists(lodPath))
                        {
                            MyLogger.LogError($"Directory does not exist: {lodPath}");
                            continue;
                        }

                        MyLogger.Log($"DtTileLoader :: Loading LOD textures for level {CurrentDetailLevel}...");                        
                        var lodTextures = Directory.GetFiles(lodPath);
                        foreach (var texture in lodTextures)
                        {
                            var texturePath = Path.Combine(basePath, texture);
                            if (File.Exists(texturePath))
                            {
                                var copyToFile = Path.Combine(basePath, Path.GetFileName(texturePath));
                                MyLogger.Log($"Found LOD texture: {texture}\n COPY: {texturePath} => {copyToFile}");                                
                                File.Copy(texturePath, copyToFile, true);
                            }
                            else
                            {
                                MyLogger.LogError($"Texture LOD does not exist: {texturePath}");
                            }
                        }
                    }

                    await ImportTileLayer(tileToLoad.Path, (go, err) =>
                    {
                        if (go != null)
                        {
                            currentCount++;
                            progress?.Invoke( 0.5f + (currentCount / totalCount)/2, errorCount);
                        }
                        else
                        {
                            errorCount ++;
                            onError?.Invoke(DtTileErrorCode.ImportError, err.SourceException?.Message);
                        }
                    });
                    
                }
                catch (Exception ex)
                {
                    errorCount++;
                    onError.Invoke(DtTileErrorCode.ImportError, ex.Message);
                }
            }

            MyLogger.Log($"DtTileLoader :: DONE! Loaded ({currentCount}) DT Tiles for hash {tileGeohash} OR loc={location.Coordinates.Lat},{location.Coordinates.Lon}\nERROR COUNT = {errorCount}");

            // Arrange tiles in scene
            foreach (var loadedTile in _loadedTiles)
            {
                var tileRef = GeoHash.Decode(loadedTile.Geohash);
                var gps = new GeoLocation { Latitude = tileRef.Coordinates.Lat, Longitude = tileRef.Coordinates.Lon };
                loadedTile.Tile.transform.position = LocManager.Instance.GetObjectPosition(gps);
                loadedTile.Tile.transform.SetParent(_parent.transform);
            }

            progress?.Invoke(1,errorCount);

            MyLogger.Log($"DtTileLoader :: DONE! Loaded ({_loadedTiles.Count}) DT Tiles for loc={location.Coordinates.Lat},{location.Coordinates.Lon}");
            return _parent;
        }

        private async Task ImportTileLayer(string filePath, Action<GameObject, ExceptionDispatchInfo> onComplete = null)
        {
            var _importOptions = new ImportOptions
            {
                DataLoader = new FileLoader(Path.GetDirectoryName(filePath)),
                AsyncCoroutineHelper = gameObject.AddOrGetComponent<AsyncCoroutineHelper>(),
            };

            MyLogger.Log($"DtTileLoader :: Khronos :: Loading file = {filePath}");

            try
            {
                if (!File.Exists(filePath))
                {
                    MyLogger.LogError($"{filePath} DOES NOT EXIST!");
                    return;
                }

                var _importer = new GLTFSceneImporter(filePath, _importOptions);

                // optimization techniques
                _importer.GenerateMipMapsForTextures = GenerateMipMapsForTextures;
                //_importer.KeepCPUCopyOfTexture = false;

                _importer.DefaultTextureFormat = TextureFormat.RGBA32;

                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    if (SystemInfo.SupportsTextureFormat(TextureFormat.ETC2_RGBA8Crunched))
                    {
                        _importer.DefaultTextureFormat = TextureFormat.ETC2_RGBA8Crunched;
                    }
                    if (SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_4x4))
                    {
                        _importer.DefaultTextureFormat = TextureFormat.ASTC_4x4;
                    }                    
                }
                else if (Application.platform == RuntimePlatform.Android)
                {
                    if (SystemInfo.SupportsTextureFormat(TextureFormat.ETC2_RGBA1))
                    {
                        _importer.DefaultTextureFormat = TextureFormat.ETC2_RGBA1;
                    }
                    if (SystemInfo.SupportsTextureFormat(TextureFormat.ETC2_RGBA8Crunched))
                    {
                        _importer.DefaultTextureFormat = TextureFormat.ETC2_RGBA8Crunched;
                    }
                    if (SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_4x4))
                    {
                        _importer.DefaultTextureFormat = TextureFormat.ASTC_4x4;
                    }
                }
                else
                {
                    //_importer.DefaultTextureFormat = TextureFormat.DXT1;
                    _importer.DefaultTextureFormat = TextureFormat.RGBA32;
                }

                _importer.Collider = GLTFSceneImporter.ColliderType.Mesh;
                _importer.SceneParent = _parent.transform;

                await _importer.LoadSceneAsync(
                    -1,
                    true, 
                    (go, err) => 
                    {
                        onComplete?.Invoke(go, err);
                        OnFinishAsync(filePath, go, err);
                    }
                );
            }
            catch (Exception ex)
            {
                MyLogger.LogError(" DtTileLoader :: Importer error" );
                MyLogger.LogException(ex);
                throw;
            }
        }

        private void OnFinishAsync(string filePath, GameObject result, ExceptionDispatchInfo info)
        {
            if (result == null)
            {
                MyLogger.LogError($"DtTileLoader :: ERROR loading GLTF => {filePath}\nERR: {info.SourceException}");
                return;
            }

            MyLogger.Log($"DtTileLoader :: loaded tile ({filePath})");

            var separators = new char[] {
              Path.DirectorySeparatorChar,
              Path.AltDirectorySeparatorChar
            };
            var parts = filePath.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var tileId = parts[parts.Length - 2];
            MyLogger.Log($"DtTileLoader :: loaded tile ID = {tileId}");

            var filename = Path.GetFileNameWithoutExtension(filePath);
            result.name = filename;
            //var obj = new GameObject($"{filename}");

            result.transform.localScale = Vector3.one;
            result.transform.Rotate(-90, 180, 0);

            //result.transform.SetParent(_parent.transform);

            var tileObj = GameObject.Find(tileId);
            if (tileObj == null)
            {
                tileObj = new GameObject(tileId);
                _loadedTiles.Add(new LoadedTile
                {
                    Tile = tileObj,
                    Geohash = tileId
                });
            }
            //tileObj.transform.SetParent(_parent.transform);
            result.transform.SetParent(tileObj.transform);

            foreach (MeshRenderer mr in result.transform.GetComponentsInChildren<MeshRenderer>())
            {
                // force white base color and non-metallic
                if (mr.material.mainTexture != null)
                {
                    mr.material.color = Color.white;
                }
                if (mr.material.HasProperty("_Metallic"))
                {
                    mr.material.SetFloat("_Metallic", 0);
                }

                var unsmooth = false;
                if (unsmoothList.Contains(Path.GetFileNameWithoutExtension(filename)))
                {
                    //MyLogger.Log($"DigitalTwinTileLoader :: Smoothing Setup for {filename}");
                    unsmooth = true;
                }
                ProcessMeshTriangles(mr.GetComponent<MeshFilter>(), unsmooth);
            }

            if (GroundLayerList.Contains(Path.GetFileNameWithoutExtension(filename)))
            {
                LayerUtils.SetLayerRecursive(result, LayerMask.NameToLayer($"{XrLayers.Terrain}"));
            }
            else
            {
                LayerUtils.SetLayerRecursive(result, LayerMask.NameToLayer($"{XrLayers.DigitalTwin}"));
            }

            if (Path.GetFileNameWithoutExtension(filename) == $"{FeatureLayer.RoadMarking}")
            {
                foreach (var mr in result.transform.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                foreach (var c in result.transform.GetComponentsInChildren<Collider>())
                {
                    Destroy(c);
                }
            }
        }

        private CacheInfo GetCacheInfo(double latitude, double longitude, double radius = 150)
        {
            MyLogger.Log($"DtTileLoader :: Getting cache info for tiles at {latitude}, {longitude}");
            var geohashes = GeoHashUtils.EncodeWithRadius(latitude, longitude, radius, _geohashLength);
            MyLogger.Log($"DtTileLoader :: Looking for ({geohashes.Length}) geohashes = {JsonConvert.SerializeObject(geohashes)} in cache");

            var nonCachedGeohashes = new List<string>(geohashes);

            // check local cache
            var dtTileCache = IOC.Resolve<ICacheProvider<CachedDtTile>>();
            var cachedTiles = new List<TileInfo>();
            foreach (var geohash in geohashes)
            {
                foreach (var layer in Enum.GetValues(typeof(FeatureLayer)))
                {
                    var cachedTile = dtTileCache.GetFromCache(Path.Combine(geohash, $"{layer}"));
                    if (cachedTile != null)
                    {
                        cachedTiles.Add(new TileInfo
                        {
                            Path = cachedTile.Path,
                            HasLods = cachedTile.HasLods
                        });
                    }
                }

                if (Directory.Exists(Path.Combine(dtTileCache.CacheDir, geohash)))
                {
                    nonCachedGeohashes.Remove(geohash);
                }
            }

            CacheInfo cacheInfo = new CacheInfo
            {
                Location = new GeoLocation { Latitude = latitude, Longitude = longitude },
                CachedTilesPath = cachedTiles,
                NonCachedGeohashes = nonCachedGeohashes,
            };

            return cacheInfo;
        }

        private async Task DownloadAllTasks(List<Task<string>> downloadTasks, List<string> filePaths, DtTileLoadEvent progress = null)
        {
            var tasks = Task.WhenAll(downloadTasks);
            var startTime = DateTime.Now;
            float downloadTargetTime = 5.0f;       // 5 seconds
            try
            {
                while (!tasks.IsCompleted)
                {
                    var timer = Task.Delay(250);

                    // Wait until either all tasks have completed OR 250ms passed
                    await Task.WhenAny(tasks, timer);
                    if (tasks.IsCompleted)
                    {
                        var paths = tasks.Result;
                        filePaths.AddRange(paths.Where(x => !string.IsNullOrEmpty(x)).ToList());
                        MyLogger.Log($"DtTileLoader :: Done downloading tiles ({paths.Length})");

                        progress?.Invoke(1, 0);
                    }
                    else
                    {
                        var currentTime = DateTime.Now;
                        double currentProgress = (currentTime - startTime).TotalSeconds / downloadTargetTime;
                        MyLogger.Log($"Download progress : {currentProgress}");
                        progress?.Invoke((float)currentProgress, 0); 
                    }
                }
            }
            catch (Exception e)
            {
                if (tasks.Exception != null)
                {
                    MyLogger.LogError($"DtTileLoader :: ERROR => {tasks.Exception.Message}\n{JsonUtility.ToJson(tasks.Exception)}");
                    throw tasks.Exception;
                }

                MyLogger.LogError($"DtTileLoader :: ERROR => {e.Message}");
                throw;
            }
        }

        private void ProcessMeshTriangles(MeshFilter mf, bool unsmooth = false)//, int layer)
        {
            var mesh = mf.mesh;

            if (unsmooth)
            {
                //Process the triangles
                Vector3[] oldVerts = mesh.vertices;
                int[] triangles = mesh.triangles;
                Vector3[] vertices = new Vector3[triangles.Length];
                for (int i = 0; i < triangles.Length; i++)
                {
                    vertices[i] = oldVerts[triangles[i]];
                    triangles[i] = i;
                }
                mesh.vertices = vertices;
                mesh.triangles = triangles;
            }

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            mf.gameObject.AddComponent<MeshCollider>();

            //DupeFixNormals(mf, layer);


            //setup DT features
           var dtFeature = mf.gameObject.AddComponent<DigitalTwinFeature>();
            dtFeature.FeatureId = mf.gameObject.name;
        }
        
        private void DupeFixNormals(MeshFilter mf, int layer)
        {
            // TODO: look into using combine meshes instead of using a separate GO

            var dupe = Instantiate(mf.gameObject, mf.gameObject.transform);
            dupe.name = $"{mf.gameObject.name} (flipped)";
            dupe.layer = layer;

            var meshFilter = dupe.GetComponent<MeshFilter>();
            var mesh = meshFilter.mesh;

            // duplicate the mesh and flip all faces (including normals for lighting)
            var indices = mesh.triangles;
            var triangleCount = indices.Length / 3;
            for (var i = 0; i < triangleCount; i++)
            {
                var tmp = indices[i * 3];
                indices[i * 3] = indices[i * 3 + 1];
                indices[i * 3 + 1] = tmp;
            }
            mesh.triangles = indices;
            // additionally flip the vertex normals to get the correct lighting
            var normals = mesh.normals;
            for (var n = 0; n < normals.Length; n++)
            {
                normals[n] = -normals[n];
            }
            mesh.normals = normals;

            dupe.GetComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}
