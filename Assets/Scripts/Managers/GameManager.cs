using System;
using System.Collections.Generic;
using System.Linq;
using NKN.Client;
using NKN.Wallet;
using Tanks.Utilities;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private readonly IList<string> _publicKeys = new List<string>();

    private readonly IDictionary<string, TankManager> _tanks = new Dictionary<string, TankManager>();
    private readonly IList<TankManager> _tanksList = new List<TankManager>();
    private Client _client;

    [SerializeField] private ExplosionManager _explosionManager;
    [SerializeField] private Material[] _materials;

    private bool _isServer;

    private string _publicKey;

    [SerializeField] private GameObject _tankPrefab;

    private TankManager LocalTank { get; set; }

    private TankManager AddTank(string playerId = null)
    {
        var tankObject = Instantiate(_tankPrefab);
        var tank = tankObject.GetComponent<TankManager>();
        var material = _materials[_tanksList.Count];
        tank.Init(_isServer, material);

        if (_isServer)
        {
            var spawnPointIndex = SpawnManager.Instance.GetRandomEmptySpawnPointIndex();
            var spawnPoint = SpawnManager.Instance.GetSpawnPointTransformByIndex(spawnPointIndex);
            tank.MoveToSpawnLocation(spawnPoint);

            _tanks.Add(playerId, tank);

        }

        _publicKeys.Add(playerId);

        _tanksList.Add(tank);

        return tank;
    }

    private void Start()
    {
        Application.targetFrameRate = 30;
        if (Application.platform == RuntimePlatform.Android)
        {
            gameObject.AddComponent<TankTouchInput>();
        }
        else
        {
            gameObject.AddComponent<TankKeyboardInput>();
        }
        _publicKey = Env.PublicKey;
        Debug.Log(_publicKey);

        _client = new Client(Env.Address, _publicKey);

        UIChat.MessageSender = m =>
        {
            if (_isServer && _publicKeys.Count > 1)
            {
                Send(_publicKeys[1], new ChatMessage(m));
            }
            else if (!_isServer)
            {
                Send(_publicKeys[0], new ChatMessage(m));
            }
        };

        Instantiate(_explosionManager);

        if (Env.ServerName == null)
            _client.OnConnect += () => UnityMainThreadDispatcher.Instance().Enqueue(() => StartServer());
        else
            _client.OnConnect += () => UnityMainThreadDispatcher.Instance().Enqueue(() => StartClient());
    }

    private void SendStates(bool initial)
    {
        var type = initial ? MessageType.InitialTankStates : MessageType.TankStates;
        var tankStates = new TankStates
        {
            Type = type,
            States = new TankStates.TankState[_tanks.Count]
        };

        var i = 0;
        foreach (var tank in _tanksList)
        {
            var position = tank.transform.position;
            var firePosition = tank.GetDesiredFirePosition();
            var fire = tank.GetFireIsHeld();
            var state = new TankStates.TankState
            {
                moveX = position.x,
                moveY = position.z,
                fire = fire,
                fireX = firePosition.x,
                fireY = firePosition.z,
                respawned = initial || tank.Respawned
            };
            if (state.respawned) state.rotation = tank.GetRotation();

            tank.Respawned = false;
            if (initial) state.publicKey = _publicKeys[i];
            tankStates.States[i] = state;
            i++;
        }

        var sendTo = _publicKeys.Where(p => !p.Equals(_publicKey)).ToList();
        sendTo.ForEach(a => Send(a, tankStates));
    }

    private void Update()
    {
        if (!_isServer) return;

        SendStates(false);
    }

    private void StartServer()
    {
        _isServer = true;

        LocalTank = AddTank(_publicKey);

        LocalTank.Follow();

        _client.OnMessage += (src, data) => UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            switch (GetMessageType(data))
            {
                case MessageType.JoinEvent:
                    AddTank(src);
                    SendStates(true);
                    break;
                case MessageType.MovementState:
                    var tank = _tanks[src];
                    var state = MovementState.FromByteArray(data);

                    var cameraDirection = new Vector3(state.moveX, state.moveY, 0);

                    if (cameraDirection.sqrMagnitude > 0.01f)
                    {
                        var worldUp = Camera.main.transform.TransformDirection(Vector3.up);
                        worldUp.y = 0;
                        worldUp.Normalize();
                        var worldRight = Camera.main.transform.TransformDirection(Vector3.right);
                        worldRight.y = 0;
                        worldRight.Normalize();

                        var worldDirection = worldUp * state.moveY + worldRight * state.moveX;
                        var desiredDir = new Vector2(worldDirection.x, worldDirection.z);
                        if (desiredDir.magnitude > 1) desiredDir.Normalize();
                        tank.SetDesiredMovementDirection(new Vector2(state.moveX, state.moveY));
                    }
                    else
                    {
                        tank.SetDesiredMovementDirection(Vector2.zero);
                    }

                    tank.SetDesiredFirePosition(new Vector3(state.fireX, 0, state.fireY));

                    tank.SetFireIsHeld(state.fire);
                    break;
                case MessageType.ChatMessage:
                    var message = ChatMessage.FromByteArray(data);
                    UIChat.ShowMessage(message.Message);
                    break;
                case MessageType.InitialTankStates:
                    throw new ArgumentOutOfRangeException();
                case MessageType.TankStates:
                    throw new ArgumentOutOfRangeException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });

        TankInputModule.Instance.Init(LocalTank);
        HUDController.Instance.InitHudPlayer(LocalTank.Health);
    }

    private void StartClient()
    {
        TankInputModule.Instance.Init(state => Send(Env.ServerName, state));

        for (var i = 0; i < 15; i++)
        {
            Send(i + "." + Env.ServerName, new JoinEvent());
        }

        _client.OnMessage += (src, data) => UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Env.ServerName = src;
            switch (GetMessageType(data))
            {
                case MessageType.InitialTankStates:
                    Debug.Log(src);
                    Debug.Log(_tanks);
                    goto case MessageType.TankStates;
                case MessageType.TankStates:
                    var tanks = TankStates.FromByteArray(data);
                    for (var i = 0; i < tanks.States.Length; i++)
                    {
                        var state = tanks.States[i];
                        TankManager tank;
                        if (tanks.Type == MessageType.InitialTankStates)
                        {
                            tank = AddTank(state.publicKey);
                            if (state.publicKey.Equals(_publicKey))
                            {
                                HUDController.Instance.InitHudPlayer(tank.Health);
                                tank.Follow();
                            }
                        }
                        else
                        {
                            tank = _tanksList[i];
                        }

                        if (state.respawned)
                            tank.Respawn(
                                new Vector3(state.moveX, 0, state.moveY),
                                new Vector3(0, state.rotation.Value, 0)
                            );
                        else
                            tank.SetDesiredMovementPosition(new Vector2(state.moveX, state.moveY));
                        tank.SetDesiredFirePosition(new Vector3(state.fireX, 0, state.fireY));
                        tank.SetFireIsHeld(state.fire);
                    }

                    break;
                case MessageType.ChatMessage:
                    var message = ChatMessage.FromByteArray(data);
                    UIChat.ShowMessage(message.Message);
                    break;
                case MessageType.JoinEvent:
                    throw new ArgumentOutOfRangeException();
                case MessageType.MovementState:
                    throw new ArgumentOutOfRangeException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });
    }

    private void Send(string serverAddress, IMessage message)
    {
        _client.Send(serverAddress, message.ToByteArray());
    }

    private static MessageType GetMessageType(IReadOnlyList<byte> data)
    {
        return (MessageType) data[0];
    }
}