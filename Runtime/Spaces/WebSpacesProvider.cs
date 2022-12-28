using Newtonsoft.Json;
using Sturfee.XRCS;
//using Sturfee.XRCS.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Sturfee.DigitalTwin.Spaces
{
    public interface ISpacesProvider
    {
        /// <summary>
        /// Find all spaces matching the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="token"></param>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        Task<List<XrSceneData>> FindSpaces(FindSpacesFilter filter, CancellationToken? token, int page = 1, int count = 10);
        
        /// <summary>
        /// Create a space locally. 
        /// Note : Created will only be saved on server when SaveSpace is called
        /// </summary>
        /// <param name="user"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        Task<XrSceneData> CreateSpace(XrcsUserData user, double latitude, double longitude);

        /// <summary>
        /// Saves/Publish this space. If successful, this space can be found uding FindSpaces
        /// </summary>
        /// <param name="space"></param>
        /// <returns></returns>
        Task SaveSpace(XrSceneData space);
        Task DeleteSpace(XrSceneData space);
    }

    public class WebSpacesProvider : ISpacesProvider
    {
        public async Task<XrSceneData> CreateSpace(XrcsUserData user, double latitude, double longitude)
        {
            var newProject = new XrProjectData
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                AccountId = user.Id,
                Privacy = ProjectPrivacy.Public,
                IsPublic = true,
                IsPublished = true,
                CreatedDate = DateTime.UtcNow,
                Location = new XrGeoLocationData
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Altitude = 0
                }
            };

            // create new scene (local)
            var newScene = new XrSceneData
            {
                Id = Guid.NewGuid(),
                ProjectId = newProject.Id,
                Project = newProject,
                UserId = user.Id,
                IsPublic = true,
                IsPublished = true,
                CreatedDate = DateTime.UtcNow,
                Location = new XrGeoLocationData
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Altitude = 0
                },

            };

            await Task.Yield();

            return newScene;
        }

        public async Task DeleteSpace(XrSceneData space)
        {
            var projectProvider = IOC.Resolve<IProjectProvider>();
            await projectProvider.DeleteXrScene(space);
        }

        public async Task<List<XrSceneData>> FindSpaces(FindSpacesFilter filter, CancellationToken? token, int page = 1, int count = 10)
        {
            MyLogger.Log($"WebSpacesProvider :: Finding Spaces... {DtConstants.SPACES_API}/sharedspaces/find?page={page}&count={count} \n{JsonConvert.SerializeObject(filter)}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{DtConstants.SPACES_API}/sharedspaces/find?page={page}&count={count}");
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";


            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(filter);
                streamWriter.Write(json);
                streamWriter.Flush();
            }

            try
            {
                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    token?.ThrowIfCancellationRequested();
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        token?.ThrowIfCancellationRequested();

                        string jsonResponse = reader.ReadToEnd();
                        Debug.Log(jsonResponse);
                        return JsonConvert.DeserializeObject<List<XrSceneData>>(jsonResponse);
                    }
                }
            }
            catch (OperationCanceledException cancelledEx)
            {
                Debug.Log(cancelledEx.Message);
                return null;
            }
            catch (WebException ex)
            {
                using (WebResponse res = ex.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)res;
                    Debug.LogError(ex.Message);
                    Debug.LogError($"WebSpacesProvider :: ERROR:: API => {httpResponse.StatusCode} - {httpResponse.StatusDescription} \n{JsonConvert.SerializeObject(filter)}");
                    return null;
                }

            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                throw;
            }

        }

        public async Task SaveSpace(XrSceneData space)
        {
            var projectProvider = IOC.Resolve<IProjectProvider>();
            var project = space.Project;

            // Save Project
            if (project != null)
            {
                project.Name = space.Name;
                project.ModifiedDate = DateTime.UtcNow;
                project.PublishedDate = DateTime.UtcNow;
                MyLogger.Log($"SpacesProvider :: Saving Layer (Project):\n{JsonConvert.SerializeObject(project)}");
                await projectProvider.SaveXrProject(project);
            }

            // save scene
            space.Project = null;
            space.ModifiedDate = DateTime.UtcNow;
            space.PublishedDate = DateTime.UtcNow;
            await projectProvider.SaveXrScene(space);
        }
    }
}