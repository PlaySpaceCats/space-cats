using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace NKN.Client
{
    public static class RPC
    {
        public interface IParams
        {
        }

        [Serializable]
        public class GetWSAddrParams : IParams
        {
            public string address;

            public GetWSAddrParams(string address)
            {
                this.address = address;
            }
        }

        [Serializable]
        public class SendRawTxParams : IParams
        {
            public string tx;

            public SendRawTxParams(string tx)
            {
                this.tx = tx;
            }
        }

        [Serializable]
        public class GetTxParams : IParams
        {
            public string hash;

            public GetTxParams(string hash)
            {
                this.hash = hash;
            }
        }

        public class Request<T> where T : IParams
        {
            public string jsonrpc = "2.0";
            public string method;
            public T parameters;

            public Request(string method, T parameters)
            {
                this.method = method;
                this.parameters = parameters;
            }
        }

        [Serializable]
        public class Response
        {
            [Serializable]
            public class Error
            {
                public int code;
                public string message;
            }

            public string result;
            public Error error;
        }

        private static readonly HttpClient Client = new HttpClient();

        public static async Task<Response> Call<T>(string addr, string method, T parameters) where T : IParams {
            var request = new Request<T>(method, parameters);
            var requestString = JsonUtility.ToJson(request).Replace("parameters", "params");
            Debug.Log(requestString);
            var content = new StringContent(requestString);
            var response = await Client.PostAsync(addr, content);
            var responseString = await response.Content.ReadAsStringAsync();
            Debug.Log(responseString);
            return JsonUtility.FromJson<Response>(responseString);
        }
    }
}