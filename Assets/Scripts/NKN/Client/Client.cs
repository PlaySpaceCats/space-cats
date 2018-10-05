using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using NKN.Client.Protocol;
using UnityEngine;
using WebSocketSharp;
using Ping = System.Net.NetworkInformation.Ping;

namespace NKN.Client
{
    public class Client
    {
        public class Options
        {
            public int ReconnectIntervalMin = Const.ReconnectIntervalMin;
            public int ReconnectIntervalMax = Const.ReconnectIntervalMax;
            public string RpcServerAddr = Const.SeedRpcServerAddr;
        }

        private class WebSocketRequest
        {
            public string Action;
            public string Addr;

            public WebSocketRequest(string action, string addr)
            {
                Action = action;
                Addr = addr;
            }
        }

        private class WebSocketResponse
        {
            public string Action;
            public ErrCodes Error;
        }

        private string address;
        private string publicKey;
        private Options options;
        private bool shouldReconnect;
        private int reconnectInterval;
        private WebSocket ws;

        public delegate void OnConnectHandler();
        public event OnConnectHandler OnConnect;

        public delegate void OnMessageHandler(string src, byte[] data);
        public event OnMessageHandler OnMessage;

        public Client(string address, string publicKey, Options options = null)
        {
            this.address = address;
            this.publicKey = publicKey;
            this.options = options ?? new Options();
            reconnectInterval = this.options.ReconnectIntervalMin;

            Connect(address);
        }

        private static long Ping(string address)
        {
            using (var ping = new Ping())
            {
                try
                {
                    var reply = ping.Send(address);
                    return reply.RoundtripTime;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return long.MaxValue;
                }
            }
        }

        public static async Task<KeyValuePair<string, string>> SelectAddress(string publicKey)
        {
            var responses = new Dictionary<string, Task<RPC.Response>>();
            for (var i = 0; i < 15; i++)
            {
                var name = i + "." + publicKey;
                responses.Add(name, RPC.Call(Const.SeedRpcServerAddr, "getwsaddr", new RPC.GetWSAddrParams(name)));
            }

            await Task.WhenAll(responses.Values);

            var nodes = new Dictionary<string, string>();
            string fastestNode = null;
            var minLatency = long.MaxValue;

            foreach (var response in responses)
            {
                var address = response.Value.Result.result;
                if (address == null || nodes.ContainsKey(address))
                {
                    continue;
                }
                var ping = Ping(address.Split(':')[0]);
                if (ping < minLatency)
                {
                    minLatency = ping;
                    fastestNode = address;
                }

                var name = response.Key;
                nodes.Add(address, name);
            }

            var fastestNodeName = nodes[fastestNode];
            return new KeyValuePair<string, string>(fastestNode, fastestNodeName);
        }

        public void Connect(string address)
        {
            try
            {
                var url = "ws://" + address;
                Debug.Log(url);
                ws = new WebSocket(url);

                ws.OnOpen += (sender, e) =>
                {
                    Debug.Log("OnOpen");
                    var setClientRequest = JsonUtility.ToJson(new WebSocketRequest("setClient", publicKey));
                    Debug.Log(setClientRequest);
                    ws.Send(setClientRequest);
                    shouldReconnect = true;
                    reconnectInterval = options.ReconnectIntervalMin;
                };

                ws.OnMessage += (sender, e) =>
                {
                    try
                    {
                        if (e.IsBinary)
                        {
                            HandleMsg(e.RawData);
                            return;
                        }

                        var msg = JsonUtility.FromJson<WebSocketResponse>(e.Data);
                        if (msg.Error != ErrCodes.Success)
                        {
                            Debug.LogError(msg.Error);
                            if (msg.Action.Equals("setClient"))
                            {
                                ws.Close();
                            }

                            return;
                        }

                        switch (msg.Action)
                        {
                            case "setClient":
                                OnConnect?.Invoke();
                                break;
                            case "updateSigChainBlockHash":
                                break;
                            default:
                                Debug.LogError("Unknown msg type: " + msg.Action);
                                break;
                        }
                    }
                    catch (Exception t)
                    {
                        Debug.LogError(t);
                    }
                };

                ws.OnClose += (sender, e) =>
                {
                    Debug.LogError("WebSocket unexpectedly closed: (" + e.Code + ") " + e.Reason);
                    if (!shouldReconnect)
                    {
                        return;
                    }
                    Reconnect();
                };

                ws.OnError += (sender, e) =>
                {
                    Debug.LogError(e.Message);
                };

                ws.Connect();
                Debug.Log("Connected");
            }
            catch (Exception e)
            {
                Debug.LogError("Create WebSocket failed: " + e.Message);
                if (!shouldReconnect)
                {
                    return;
                }
                Reconnect();
            }
        }

        private void Reconnect()
        {
            Task.Run(async () =>
            {
                Console.WriteLine("Reconnecting in " + reconnectInterval / 1000 + "s...");
                await Task.Delay(reconnectInterval * 60000, CancellationToken.None);
                reconnectInterval *= 2;
                if (reconnectInterval > options.ReconnectIntervalMax)
                {
                    reconnectInterval = options.ReconnectIntervalMax;
                }
                Connect(address);
            }, CancellationToken.None).Wait(CancellationToken.None);
        }

        public void Close()
        {
            shouldReconnect = false;
            ws.Close();
        }

        public void Send(string dest, byte[] data)
        {
            var msg = new OutboundMessage
            {
                Dest = dest,
                Payload = ByteString.CopyFrom(data)
            };
            ws.SendAsync(msg.ToByteArray(), _ => {});
        }
        
        private void HandleMsg(byte[] rawMsg)
        {
            var msg = InboundMessage.Parser.ParseFrom(rawMsg);
            var data = msg.Payload.ToByteArray();

            OnMessage?.Invoke(msg.Src, data);
        }
    }
}