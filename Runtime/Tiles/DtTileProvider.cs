using Newtonsoft.Json;
using SturfeeVPS.Core;
using SturfeeVPS.Core.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Sturfee.DigitalTwin.Tiles
{
    public interface ITileProvider
    {
        Task<DigitalTwinTileItem> FetchTileUrl(string geohash);
        Task<string> DownloadTileLayers(string geohash, string url);
    }

    public class DtTileProvider : ITileProvider
    {
        public DtTileProvider()
        {
            ServicePointManager.DefaultConnectionLimit = 1000; 
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
        public async Task<DigitalTwinTileItem> FetchTileUrl(string geohash)
        {
            // get download URL
            string url = DtConstants.DTE_OUTDOOR_TILES_API + "/" + geohash;

            try
            {               
                var uwr = new UnityWebRequest(url);

                var dh = new DownloadHandlerBuffer();
                uwr.downloadHandler = dh;

                uwr.method = UnityWebRequest.kHttpVerbGET;
                await uwr.SendWebRequest();
                
                

                if (uwr.result == UnityWebRequest.Result.ConnectionError) //uwr.isNetworkError || uwr.isHttpError)
                {
                }
                else
                {
                    var item = JsonConvert.DeserializeObject<DigitalTwinTileItem>(uwr.downloadHandler.text);
                    return item;
                }

            }
            catch (Exception e)
            {           
            }

            return null;
        }

        public async Task<string> DownloadTileLayers(string geohash, string url)
        {
            MyLogger.Log($"DtTileProvider :: Downloading Tile Layers for {geohash}");
            var dtTileCache = IOC.Resolve<ICacheProvider<CachedDtTile>>();

            var tileFolder = Path.Combine(dtTileCache.CacheDir, $"{geohash}");
            var tileZip = Path.Combine(tileFolder, $"{geohash}.zip");
            if (!Directory.Exists(tileFolder)) { Directory.CreateDirectory(tileFolder); }

            try
            {               
                var uwr = new UnityWebRequest(url);

                uwr.method = UnityWebRequest.kHttpVerbGET;
                var dh = new DownloadHandlerFile($"{tileZip}");
                dh.removeFileOnAbort = true;
                uwr.downloadHandler = dh;
                await uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success) //(uwr.result == UnityWebRequest.Result.ConnectionError) //uwr.isNetworkError || uwr.isHttpError)
                {
                    MyLogger.LogError(uwr.error);
                    MyLogger.LogError($"DtTileProvider :: ERROR Downloading Tile {geohash}");
                }
                else
                {
                    MyLogger.Log("DtTileProvider :: Download saved to: " + tileZip.Replace("/", "\\") + "\r\n" + uwr.error);

                    // extract zip file
                    ZipFile.ExtractToDirectory(tileZip, tileFolder);
                    // delete zip file
                    File.Delete(tileZip);
                }

            }
            catch (Exception e)
            {               
                MyLogger.LogError($"ERROR :: LoadTileLayer.DownloadFileTaskAsync => {e.Message}\n{e.StackTrace}");
                throw e;                
            }

            return tileZip;
        }
    }
}
