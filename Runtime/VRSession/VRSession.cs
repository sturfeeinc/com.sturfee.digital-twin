using Sturfee.DigitalTwin.CMS;
using Sturfee.DigitalTwin.Tiles;
using Sturfee.XRCS;
using Sturfee.XRCS.Config;
using SturfeeVPS.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

public class SpawnPoint
{
    public Vector3 Position;
    public Quaternion Rotation;
}

public enum VRSessionStatus
{
    NotReady,
    Initializing,
    Loading,
    Ready
}

[Serializable]
public class SpaceLoadOptions
{
    public bool Tiles = true;
    public bool CMS;
    public bool Enhancements;
}

public class VRSession : SceneSingleton<VRSession>
{
    public VRSessionStatus Status => _status;
    [SerializeField]
    private VRSessionStatus _status = VRSessionStatus.NotReady;

    public XrSceneData Space => _space;
    [SerializeField]
    private XrSceneData _space;

    public SpaceLoadOptions Options;

    public float SpawnHeightAboveGround = 15;

    public float LoadProgress { get; private set; }

    private float _tileLoadProgress = 0;
    private float _dtEnhancementLoadProgress = 0;
    private float _xrAssetsLoadProgress = 0;
    private float _xrSceneLoadProgress = 0;

    private bool _dtTilesLoaded = false;

    TaskCompletionSource<bool> AreTilesLoading = new TaskCompletionSource<bool>();
    TaskCompletionSource<bool> AreAssetsLoading = new TaskCompletionSource<bool>();

    public void CreateSession(XrSceneData space, SpaceLoadOptions spaceLoadOptions)
    {
        Options = spaceLoadOptions;
        CreateSession(space);
    }

    public async void CreateSession(XrSceneData space)
    {
        ServicePointManager.DefaultConnectionLimit = 1000;

        try
        {
            MyLogger.Log(" Creating VR session");
            _status = VRSessionStatus.Initializing;

            List<Task> loadTasks = new List<Task>();

            loadTasks.Add(LoadTiles(space));
            loadTasks.Add(LoadCMS(space));
            loadTasks.Add(LoadEnhancements(space));

            await Task.WhenAll(loadTasks);

        }
        catch (Exception e)
        {
            _status = VRSessionStatus.NotReady;

            MyLogger.LogException(e);
            throw;
        }

        await AreTilesLoading.Task;
        await AreAssetsLoading.Task;

        await Task.Delay(2000);

        var spawnPoint = GetSpawnLocation();
        spawnPoint.Position.y = GetElevation() + SpawnHeightAboveGround;

        MyLogger.Log(" VR Camera start position : " + JsonUtility.ToJson(spawnPoint));

        VRCamera.CurrentInstance.SetPosition(spawnPoint.Position);
        VRCamera.CurrentInstance.SetRotation(spawnPoint.Rotation);
        VRCamera.CurrentInstance.Activate();


        _space = space;
        _status = VRSessionStatus.Ready;
    }

    private async Task LoadTiles(XrSceneData space)
    {
        if (!Options.Tiles)
        {
            _tileLoadProgress = 1;
            _dtTilesLoaded = true;
            AreTilesLoading.SetResult(true);
            return;
        }

        MyLogger.Log($" Loading Tiles");

        int _dtTilesFailedCount = 0;
        Stopwatch tileWatch = Stopwatch.StartNew();
        await DtTileLoader.Instance.LoadTilesAt(
            space.Location.Latitude,
            space.Location.Longitude,
            (progress, errorCount) =>
            {
                _tileLoadProgress = progress;

                if (progress >= 1 && !_dtTilesLoaded)
                {
                    MyLogger.Log($"VRSession :: DT Tiles Loaded!!! erros = {errorCount}");
                    _dtTilesLoaded = true;
                    MyLogger.Log($" Timer :: VRSession ::  Tile load time : {tileWatch.ElapsedMilliseconds} ms");
                    tileWatch.Stop();

                    var dtTileParent = GameObject.Find("DigitalTwinTiles");
                    if (dtTileParent != null)
                    {
                        dtTileParent.transform.SetParent(transform.parent);
                    }

                    AreTilesLoading.SetResult(_dtTilesLoaded);
                }

                UpdateProgress();
            },
            (code, error ) =>
            {
                _dtTilesFailedCount++;

                if (_dtTilesFailedCount == 9)
                {
                    if (code == DtTileErrorCode.NotFound)
                    {
                        // load the SturG instead
                        MyLogger.LogError($"VRSSession :: Error finding some DT Tiles...");
                    }
                    else
                    {
                        MyLogger.LogError($"VRSSession :: Error loading some DT Tiles... {error}");
                    }

                    _status = VRSessionStatus.NotReady;
                }
            }
        );
    }

    private async Task LoadCMS(XrSceneData space)
    {        
        if (!Options.CMS)
        {
            _xrAssetsLoadProgress = 1;
            _xrSceneLoadProgress = 1;

            AreAssetsLoading.SetResult(true);
            return;
        }

        MyLogger.Log($" Loading CMS");

        Stopwatch cmsWatch = Stopwatch.StartNew();

        await CMSLoader.Instance.LoadAssets(
            space,
            (assetProgress, errorCount) =>
            {
                _xrAssetsLoadProgress = assetProgress;
                if (assetProgress >= 1)
                {
                    MyLogger.Log($"VRSession :: XR Assets Loaded!!! erros = {errorCount}");
                }

                UpdateProgress();
            },
            (sceneProgress, errorCount) =>
            {
                _xrSceneLoadProgress = sceneProgress;
                if (sceneProgress == 1)
                {
                    MyLogger.Log($"VRSession :: XR Scene Loaded!!! erros = {errorCount}");
                    MyLogger.Log($" Timer :: VRSession ::  CMS load time : {cmsWatch.ElapsedMilliseconds} ms");
                    cmsWatch.Stop();
                    AreAssetsLoading.SetResult(true);
                }

                UpdateProgress();
            },
            (error) =>
            {
                _status = VRSessionStatus.NotReady;
            }
        );
    }

    private async Task LoadEnhancements(XrSceneData space)
    {
        if(!Options.Enhancements)
        {
            _dtEnhancementLoadProgress = 1;
            return;
        } 

        await Task.Yield();
        _dtEnhancementLoadProgress = 1;
    }


    private void UpdateProgress()
    {
        LoadProgress = (_tileLoadProgress + _xrAssetsLoadProgress + _xrSceneLoadProgress) / 3;
    }

    private SpawnPoint GetSpawnLocation()
    {
        // try to use spawn point
        var spawnLocations = FindObjectsOfType<SpawnPointTemplateAsset>();
        if (spawnLocations.Any())
        {
            //foreach (var spawnPoint in spawnLocations)
            //{
            //    spawnPoint.ShowUi = false;
            //}

            MyLogger.Log($"VRSession :: {spawnLocations.Length} spawn points found in the Space!");
            var randpoint = UnityEngine.Random.Range(0, spawnLocations.Length);

            return new SpawnPoint
            {
                Position = spawnLocations[randpoint].transform.position,
                Rotation = spawnLocations[randpoint].transform.rotation
            };
        }

        MyLogger.Log($"VRSession :: no spawn points in the Space. using random start...");

        // otherwise use random position
        int SpawnXMin = -3;
        int SpawnXMax = 3;

        int SpawnZMin = -3;
        int SpawnZMax = 3;

        var randX = UnityEngine.Random.Range(SpawnXMin, SpawnXMax);
        var randZ = UnityEngine.Random.Range(SpawnZMin, SpawnZMax);

        Vector3 randomSpawnPos = new Vector3(randX, 0, randZ);
        MyLogger.Log($"Spawn Pos : {randomSpawnPos}");

        return new SpawnPoint
        {
            Position = randomSpawnPos,
            Rotation = Quaternion.identity
        };
    }

    private float GetElevation()
    {
        RaycastHit hit;

        Vector3 unityPos = new Vector3(0, 15000, 0);

        Ray ray = new Ray(unityPos, Vector3.down);
        //MyLogger.DrawRay(ray.origin, ray.direction * 30000, Color.green, 2000);

        var layermask = LayerMask.GetMask($"{XrLayers.Terrain}", $"{XrLayers.DigitalTwin}", $"{XrLayers.DtAssets}");
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layermask))
        {
            float elevation = hit.point.y;
            MyLogger.Log("terrain elevation : " + elevation);
            return elevation;
        }

        return 0;
    }
}
