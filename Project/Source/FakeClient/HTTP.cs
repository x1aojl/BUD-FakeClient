using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Net;
using System.Net.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public enum HTTP_METHOD
{
    GET = 0,
    POST = 1
}

public class HttpPack
{
    public int StatusCode;
    public string ResponeData;
}

public static class Http
{
    // Http请求超时时长（秒）
    private const int HTTP_TIME_OUT = 10;

    public static void Init()
    {
        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Ssl3;
    }

    public static void MakeHttpRequest(string url, Dictionary<string, object> parameters, Dictionary<string, object> headers, Action<string> onReceive, Action<string> onFail)
    {
        List<string> keys;
        keys = parameters.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
            url += string.Format("{0}{1}={2}", i == 0 ? "?" : "&", keys[i], parameters[keys[i]]);

        WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
        keys = headers.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
            webHeaderCollection.Add(keys[i], headers[keys[i]].ToString());

        SendRequest(url, webHeaderCollection, (HttpPack httpPack) => {
            if (httpPack.StatusCode < 200)
            {
                onFail?.Invoke(string.Format("MakeAsyncRequest failed"));
            }
            else if (httpPack.StatusCode != 200)
            {
                onFail?.Invoke(string.Format("MakeAsyncRequest error. StatusCode = {0}", httpPack.StatusCode));
            }
            else if (string.IsNullOrEmpty(httpPack.ResponeData))
            {
                onFail?.Invoke(string.Format("MakeAsyncRequest ResponeData is null. StatusCode = {0}", httpPack.StatusCode));
            }
            else
            {
                var responseDataRaw = JsonConvert.DeserializeObject<Dictionary<string, object>>(httpPack.ResponeData);
                var result = responseDataRaw["result"].ToString();
                if (result != "0")
                {
                    onFail?.Invoke(httpPack.ResponeData);
                }
                else
                {
                    var data = responseDataRaw["data"].ToString();
                    onReceive?.Invoke(data);
                }
            }
        });
    }

    private static void SendRequest(string url, WebHeaderCollection headers, Action<HttpPack> callback)
    {
        HttpWebRequest webRequest;
        webRequest = WebRequest.Create(url) as HttpWebRequest;
        webRequest.ProtocolVersion = HttpVersion.Version10;

        webRequest.Headers = headers;

        webRequest.Accept = "application/json";
        webRequest.Timeout = HTTP_TIME_OUT * 1000;
        webRequest.UseDefaultCredentials = false;

        webRequest.Method = "GET";
        webRequest.ContentType = "application/json";

        string responseContent = "";
        HttpPack pack = new HttpPack();
        try
        {
            using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
            {
                pack.StatusCode = (int)response.StatusCode;
                using (Stream resStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(resStream, Encoding.UTF8))
                    {
                        responseContent = reader.ReadToEnd().ToString();
                    }
                }
            }
        }
        catch (WebException ex)
        {
            pack.StatusCode = (int)ex.Status;
        }

        pack.ResponeData = responseContent;
        callback.Invoke(pack);
        webRequest.Abort();
    }

    private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
    {
        return true;
    }
}