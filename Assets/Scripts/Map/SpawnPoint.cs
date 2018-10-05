using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpawnPoint : MonoBehaviour
{
    private bool _isDirty;

    private int _numberOfTanksInZone;

    [SerializeField] protected Transform m_SpawnPointTransform;

    public Transform SpawnPointTransform
    {
        get
        {
            if (m_SpawnPointTransform == null) m_SpawnPointTransform = transform;
            return m_SpawnPointTransform;
        }
    }

    public bool IsEmptyZone => !_isDirty && _numberOfTanksInZone == 0;

    private void OnTriggerEnter(Collider c)
    {
        var tankHealth = c.GetComponentInParent<TankHealth>();

        if (tankHealth == null)
        {
            return;
        }

        _numberOfTanksInZone++;
        tankHealth.currentSpawnPoint = this;
    }

    private void OnTriggerExit(Collider c)
    {
        var tankHealth = c.GetComponentInParent<TankHealth>();

        if (tankHealth == null)
        {
            return;
        }

        Decrement();
        tankHealth.NullifySpawnPoint(this);
    }

    public void Decrement()
    {
        _numberOfTanksInZone--;
        if (_numberOfTanksInZone < 0) _numberOfTanksInZone = 0;

        _isDirty = false;
    }

    public void SetDirty()
    {
        _isDirty = true;
    }

    public void Cleanup()
    {
        _isDirty = false;
        _numberOfTanksInZone = 0;
    }
}