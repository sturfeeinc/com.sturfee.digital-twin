using Sturfee.DigitalTwin.CMS;
using Sturfee.DigitalTwin.Tiles;
using Sturfee.XRCS.Config;
using SturfeeVPS.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sturfee.DigitalTwin.Demo
{
    public class SpaceItem : MonoBehaviour
    {        
        [SerializeField]
        private TextMeshProUGUI _title;
        [SerializeField]
        private TextMeshProUGUI _subtitle;
        [SerializeField]
        private XrSceneData _spaceData;
        [SerializeField]
        private Toggle _multiplayerToggle;
        [SerializeField]
        private Image _thumbnail;
        [SerializeField]
        private GameObject _playButtons;
        [SerializeField]
        private GameObject _downloadButton;

        [SerializeField]
        private Image _progress;

        private float _totalProgress;
        private float _tileDownloadProgress = 0;
        private float _cmsDownloadProgress = 0;
        private float _enhancementsDownloadProgress = 0;

        private int _maxUsernameLength = 12;

        public async void SetData(XrSceneData spaceData)
        {
            _spaceData = spaceData;
            _title.text = spaceData.Name;

            var username = _spaceData.User.Name.Length > _maxUsernameLength ? $"{_spaceData.User.Name.Substring(0, _maxUsernameLength)}..." : _spaceData.User.Name;
            _subtitle.text = $"{username} | {spaceData.CreatedDate.ToString("M")}";

            bool showPlay = await AvailableInCache();

            _downloadButton.SetActive(!showPlay);
            _playButtons.SetActive(showPlay);

            ThumbnailUtils.TryLoadThumbnail(_spaceData.Id, _spaceData.ProjectId, (image) =>
            {
                if (image != null)
                {
                    _thumbnail.sprite = ImageUtils.ConvertTextureToSprite(image as Texture2D);
                }
                else
                {
                    ResavePngToJpg();
                }
            }, ImageFileType.jpg);
        }

        public async void Download()
        {
            try
            {
                var downloadWatch = Stopwatch.StartNew();
                List<Task> downloadTasks = new List<Task>() { DownloadTiles(), DownloadCMS(), DownloadEnhancements() };
                await Task.WhenAll(downloadTasks);
                MyLogger.Log($" Timer :: SpaceItem ::  Download time : {downloadWatch.ElapsedMilliseconds} ms");

                _downloadButton.SetActive(false);
                _playButtons.SetActive(true);
            }
            catch (Exception ex)
            {
                MyLogger.LogException(ex);
                MobileToastManager.Instance.ShowToast("Error downloading Space", -1, true);
            }
        }

        public void OnPlayVR()
        {
            SpacesManager.Instance.StartSpaceMode = SpaceMode.VR;
            PlayAsync();
        } 

        public void OnPlayAR()
        {
            SpacesManager.Instance.StartSpaceMode = SpaceMode.AR;
            PlayAsync();
        }

        public async void PlayAsync()
        {
            SpacesManager.Instance.SetCurrentSpace(_spaceData);

            if(_multiplayerToggle.isOn)
            {
                LoadScreenManager.Instance.ShowLoadingScreen();
#if CLIENT
                SpacesManager.Instance.GameMode = GameMode.Multiplayer;
                await GameSessionManager.Instance.Join(_spaceData);
#endif
            }
            else
            {
                SpacesManager.Instance.GameMode = GameMode.Single;
                SceneManager.LoadScene("3.Game");
            }            
        }

        private async Task DownloadTiles()
        {
            try
            {
                await DtTileLoader.Instance.DownloadTilesAt(
                    _spaceData.Location.Latitude, 
                    _spaceData.Location.Longitude,
                    (progress, errorCount) =>
                    {
                        _tileDownloadProgress = progress;
                        UpdateProgress();
                    },
                    (errorCode, eror) =>
                    {

                    }
                );
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private async Task DownloadCMS()
        {
            await CMSLoader.Instance.DoawnloadAssets(
                _spaceData,
                (progress) =>
                {
                    _cmsDownloadProgress = progress;
                    UpdateProgress();
                }
            );
        }

        // TODO
        private async Task DownloadEnhancements()
        {            
            await Task.Yield();
            _enhancementsDownloadProgress = 1;
        }
        
        private void UpdateProgress()
        {
            _totalProgress = (_tileDownloadProgress + _cmsDownloadProgress + _enhancementsDownloadProgress) / 3;
            _progress.fillAmount = _totalProgress;
        }

        private async Task<bool> AvailableInCache()
        {
            bool tiles = DtTileLoader.Instance.AvailableInCache(_spaceData.Location.Latitude, _spaceData.Location.Longitude);
            bool cms = await CMSLoader.Instance.AvailableInCache(_spaceData);

            return tiles && cms;
        }

        private async void ResavePngToJpg()
        {
            var texture = await ThumbnailUtils.LoadThumbnail(_spaceData.Id, ImageFileType.png);
            if (texture == null)
            {
                texture = await ThumbnailUtils.LoadThumbnail(_spaceData.ProjectId, ImageFileType.png);
            }
            if (texture != null)
            {
                try
                {
                    var baseDirectory = $"{Application.persistentDataPath}/{XrConstants.LOCAL_THUMBNAILS_PATH}";
                    if (!Directory.Exists(baseDirectory)) { Directory.CreateDirectory(baseDirectory); }
                    var thumbFile = Path.Combine($"{baseDirectory}", $"{_spaceData.Id}.jpg");

                    MyLogger.Log($"Resaving image to JPG: {thumbFile}");

                    var bytes = texture.EncodeToJPG();
                    File.WriteAllBytes(thumbFile, bytes);

                    // resave as jpg
                    var thumbnailProvider = IOC.Resolve<IThumbnailProvider>();
                    await thumbnailProvider.SaveThumbnail(_spaceData.Id, ImageFileType.jpg);

                    _thumbnail.sprite = ImageUtils.ConvertTextureToSprite(texture as Texture2D);
                }
                catch (Exception ex)
                {
                    MyLogger.LogError(ex);
                }
            }
        }
    }
}