using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RESTFULInterface
{
    private static RESTFULInterface instance;
    public static RESTFULInterface Instance
    {
        get {
            if (instance == null)
                instance = new RESTFULInterface();
            return instance;
        }
    }

    public IEnumerator GetRequest(string uri, Action<string> responseCallback)
    {
        //Debug.Log("sending request to: " + uri);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    responseCallback("Error :" + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    responseCallback("Error :" + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    responseCallback(webRequest.downloadHandler.text);
                    break;
            }
        }
    }

    public IEnumerator GetRequest(string uri, Action<Texture2D> responseCallback)
    {
        //Debug.Log("sending request to: " + uri);

        using (var webRequest = UnityWebRequestTexture.GetTexture(uri))
        {
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    responseCallback(null);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    responseCallback(null);
                    break;
                case UnityWebRequest.Result.Success:
                   var texture = DownloadHandlerTexture.GetContent(webRequest);
                    responseCallback(texture);
                    break;
            }
        }
    }
}
