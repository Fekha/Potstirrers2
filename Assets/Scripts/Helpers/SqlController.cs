using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SqlController
{
    string apiUrl;
    public SqlController()
    {
        apiUrl = "https://potstirrersapi.azurewebsites.net/api/";
#if UNITY_EDITOR
        apiUrl = "http://localhost:7001/api/";
#endif
    }
    public IEnumerator RequestRoutine(string url, Action<string> callback = null, bool allowGuest = false)
    {
        if (allowGuest || !Global.LoggedInPlayer.IsGuest)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(apiUrl + url))
            {
                request.SetRequestHeader("Access-Control-Allow-Origin", "*");
                yield return request.SendWebRequest();

                var data = request.downloadHandler.text;

                if (callback != null)
                    callback(data);
            }
        }
    }

    public IEnumerator PostRoutine(string url, dynamic ToPost, Action<string> callback = null)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(ToPost);
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
        using (UnityWebRequest request = UnityWebRequest.Put(apiUrl + url, postData))
        {
            //request.SetRequestHeader("Access-Control-Allow-Origin", "*");
            request.SetRequestHeader("Content-Type", "application/json");
            request.method = "POST";
            //request.chunkedTransfer = false;
            yield return request.SendWebRequest();

            var data = request.downloadHandler.text;

            if (callback != null)
                callback(data);
        }
    }
    public T jsonConvert<T>(string json)
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
    }
}