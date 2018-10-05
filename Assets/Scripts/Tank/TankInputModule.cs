using System;
using Tanks.Utilities;
using UnityEngine;

public abstract class TankInputModule : Singleton<TankInputModule>
{
    private bool _initialized;

    private Action<MovementState> _send;

    private MovementState _state;
    private TankManager _tank;
    protected Plane FloorPlane;

    protected int GroundLayerMask;

    private static TankInputModule CurrentInputModule { get; set; }

    protected bool IsActiveModule => CurrentInputModule == this;

    protected override void Awake()
    {
        base.Awake();

        OnBecomesInactive();
        FloorPlane = new Plane(Vector3.up, 0);
        GroundLayerMask = LayerMask.GetMask("Ground");
    }

    public void Init(TankManager tank)
    {
        _initialized = true;

        _tank = tank;
    }

    public void Init(Action<MovementState> send)
    {
        _initialized = true;

        _send = send;
    }

    protected virtual void Update()
    {
        if (!_initialized) return;

        _state = new MovementState();

        var isActive = DoMovementInput();
        isActive |= DoFiringInput();

        _send?.Invoke(_state);

        if (!isActive || IsActiveModule) return;

        if (CurrentInputModule != null) CurrentInputModule.OnBecomesInactive();
        CurrentInputModule = this;
        OnBecomesActive();
    }

    protected virtual void OnBecomesActive()
    {
    }

    protected virtual void OnBecomesInactive()
    {
    }

    protected abstract bool DoMovementInput();

    protected abstract bool DoFiringInput();

    protected void SetDesiredMovementDirection(Vector2 moveDir)
    {
        if (_tank != null)
        {
            _tank.SetDesiredMovementDirection(moveDir);
        }
        else
        {
            _state.moveX = moveDir.x;
            _state.moveY = moveDir.y;
        }
    }

    protected void SetDesiredFirePosition(Vector3 target)
    {
        if (_tank != null)
        {
            _tank.SetDesiredFirePosition(target);
        }
        else
        {
            _state.fireX = target.x;
            _state.fireY = target.z;
        }
    }

    protected void SetFireIsHeld(bool fireHeld)
    {
        if (_tank != null)
            _tank.SetFireIsHeld(fireHeld);
        else
            _state.fire = fireHeld;
    }

    protected void OnDisable()
    {
        SetDesiredMovementDirection(Vector2.zero);
        SetFireIsHeld(false);
    }
}