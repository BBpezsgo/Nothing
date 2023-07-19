using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Networking
{
    public class DownloadManager : MonoBehaviour
    {
        readonly struct HttpResponse
        {
            readonly Action<UnityWebRequest> callback;

            public HttpResponse(Action<UnityWebRequest> callback)
            {
                this.callback = callback;
            }

            internal void Invoke(UnityWebRequest result) => callback?.Invoke(result);
        }

        static DownloadManager Instance;
        readonly Queue<HttpResponse> requests = new();

        void Start()
        {
            if (Instance == null) Instance = this;
        }

        void StartHttpGet(string url, Action<UnityWebRequest> callback) => StartCoroutine(StartHttpGetAsync(url, callback));
        IEnumerator StartHttpGetAsync(string url, Action<UnityWebRequest> callback)
        {
            requests.Enqueue(new HttpResponse(callback));
            UnityWebRequest req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            requests.Dequeue().Invoke(req);
        }

        void StartHttpPut(string url, byte[] data, Action<UnityWebRequest> callback) => StartCoroutine(StartHttpPutAsync(url, data, callback));
        IEnumerator StartHttpPutAsync(string url, byte[] data, Action<UnityWebRequest> callback)
        {
            requests.Enqueue(new HttpResponse(callback));
            UnityWebRequest req = UnityWebRequest.Put(url, data);
            yield return req.SendWebRequest();
            requests.Dequeue().Invoke(req);
        }

        void StartHttpPut(string url, string data, Action<UnityWebRequest> callback) => StartCoroutine(StartHttpPutAsync(url, data, callback));
        IEnumerator StartHttpPutAsync(string url, string data, Action<UnityWebRequest> callback)
        {
            requests.Enqueue(new HttpResponse(callback));
            UnityWebRequest req = UnityWebRequest.Put(url, data);
            yield return req.SendWebRequest();
            requests.Dequeue().Invoke(req);
        }

        /// <exception cref="AssetManager.SingletonNotExistException{T}"></exception>
        public static void Get(string url, Action<UnityWebRequest> callback)
        {
            if (Instance == null) throw new AssetManager.SingletonNotExistException<DownloadManager>();
            Instance.StartHttpGet(url, callback);
        }

        /// <exception cref="AssetManager.SingletonNotExistException{T}"></exception>
        public static void Put(string url, byte[] data, Action<UnityWebRequest> callback)
        {
            if (Instance == null) throw new AssetManager.SingletonNotExistException<DownloadManager>();
            Instance.StartHttpPut(url, data, callback);
        }

        /// <exception cref="AssetManager.SingletonNotExistException{T}"></exception>
        public static void Put(string url, string data, Action<UnityWebRequest> callback)
        {
            if (Instance == null) throw new AssetManager.SingletonNotExistException<DownloadManager>();
            Instance.StartHttpPut(url, data, callback);
        }
    }
}
