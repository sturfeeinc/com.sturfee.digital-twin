using Sturfee.DigitalTwin.Demo;
using SturfeeVPS.Core;
using SturfeeVPS.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class CreateSpace : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField _spaceNameInput;
	[SerializeField]
	private TMP_InputField _locationInput;
	[SerializeField]
	private TMP_InputField _locationInputReadOnly;
	[SerializeField]
	private GameObject _createSpaceDialog;

	private XrSceneData _newSpace;

	public async void Create()
    {
        if (string.IsNullOrEmpty(_locationInput.text))
        {
			MyLogger.LogError("Location is empty");
			return;
        }

		try
		{
			GeoLocation location = new GeoLocation();
			try
			{
				location = StringToLocation(_locationInput.text);
			}
			catch (Exception ex)
            {
				MobileToastManager.Instance.ShowToast("Error creating Space : Location Invalid", -1, true);
				return;
			}
			_newSpace = await SpacesManager.Instance.CreateSpace(location.Latitude, location.Longitude);
			_createSpaceDialog.SetActive(true);
			_locationInputReadOnly.text = _locationInput.text;

		}
		catch (Exception ex)
        {
			MobileToastManager.Instance.ShowToast("Error creating Space", -1, true);
		}
	}

	public void OnEndEditLocation()
    {
		if (_newSpace == null)
		{
			MyLogger.LogError(" New space is NULL");
			return;
		}

		Create();
    }

	public async void Publish()
    {
		if(_newSpace == null)
        {
			MyLogger.LogError(" New space is NULL");
			return;
        }

		_newSpace.Name = _spaceNameInput.text;
		await SpacesManager.Instance.PublishSpace(_newSpace);

		MobileToastManager.Instance.ShowToast(" Space Created");
    }

    private GeoLocation StringToLocation(string s)
    {
		var latLonSplit = s.Split(',');
		if (latLonSplit.Length != 2)
		{
			throw new ArgumentException("Wrong number of arguments");
		}

		double latitude = 0;
		double longitude = 0;

		if (!double.TryParse(latLonSplit[0], NumberStyles.Any, NumberFormatInfo.InvariantInfo, out latitude))
		{
			throw new Exception(string.Format("Could not convert latitude to double: {0}", latLonSplit[0]));
		}

		if (!double.TryParse(latLonSplit[1], NumberStyles.Any, NumberFormatInfo.InvariantInfo, out longitude))
		{
			throw new Exception(string.Format("Could not convert longitude to double: {0}", latLonSplit[0]));
		}

		return new GeoLocation { Latitude = latitude, Longitude = longitude };
	}
}
