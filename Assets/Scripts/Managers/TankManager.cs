using UnityEngine;

public class TankManager : MonoBehaviour
{
    private TankDisplay _display;

    private bool _isServer;
    private TankMovement _movement;
    private TankShooting _shooting;

    [SerializeField] private GameObject _tankPrefab;

    internal TankHealth Health;

    public bool Respawned;

    public void Init(bool isServer, Material material)
    {
        _isServer = isServer;

        var tankDisplay = Instantiate(_tankPrefab, transform.position, transform.rotation);
        tankDisplay.transform.SetParent(transform, true);

        _display = tankDisplay.GetComponent<TankDisplay>();
        _movement = GetComponent<TankMovement>();
        _shooting = GetComponent<TankShooting>();
        Health = GetComponent<TankHealth>();

        _movement.Init(isServer);
        _shooting.Init(_display);
        Health.Init(this, _display);
        _display.Init(_movement, material);
    }

    public void MoveToSpawnLocation(Transform spawnPoint)
    {
        _movement.Rigidbody.position = spawnPoint.position;
        _movement.transform.position = spawnPoint.position;

        _movement.Rigidbody.rotation = spawnPoint.rotation;
        _movement.transform.rotation = spawnPoint.rotation;
    }

    public void MoveToLocation(Vector3 position)
    {
        _movement.Rigidbody.position = position;
        _movement.transform.position = position;
    }

    public void Follow()
    {
        CameraFollow.Instance.SetTankToFollow(transform, _movement);
    }

    public void DisableControl()
    {
        _movement.DisableMovement();
        _shooting.enabled = false;
        _shooting.canShoot = false;
    }

    public void EnableControl()
    {
        _movement.EnableMovement();
        _shooting.enabled = true;
        _shooting.canShoot = true;
        _display.ReEnableTrackParticles();
    }

    public void Respawn()
    {
        if (!_isServer) return;

        Respawned = true;

        var spawnPointIndex = SpawnManager.Instance.GetRandomEmptySpawnPointIndex();
        var spawnPoint = SpawnManager.Instance.GetSpawnPointTransformByIndex(spawnPointIndex);

        Respawn(spawnPoint.position, spawnPoint.rotation.eulerAngles);
    }

    public void Respawn(Vector3 position, Vector3 rotation)
    {
        Health.SetDefaults();

        MoveToLocation(position);
        transform.rotation = Quaternion.Euler(rotation);

        _movement.SetDefaults();
        _movement.SetAudioSourceActive(true);
        _shooting.SetDefaults();
    }

    public void SetDesiredMovementDirection(Vector2 moveDir)
    {
        _movement.SetDesiredMovementDirection(moveDir);
    }

    public void SetDesiredMovementPosition(Vector2 movePos)
    {
        _movement.SetDesiredMovementPosition(movePos);
    }

    public void SetDesiredFirePosition(Vector3 target)
    {
        _shooting.SetDesiredFirePosition(target);
    }

    public Vector3 GetDesiredFirePosition()
    {
        return _shooting.GetDesiredFirePosition();
    }

    public void SetFireIsHeld(bool fireHeld)
    {
        _shooting.SetFireIsHeld(fireHeld);
    }

    public bool GetFireIsHeld()
    {
        return _shooting.GetFireIsHeld();
    }

    public float GetRotation()
    {
        return transform.rotation.eulerAngles.y;
    }
}