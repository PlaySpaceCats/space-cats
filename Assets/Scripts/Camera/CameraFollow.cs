using Tanks.Utilities;
using UnityEngine;

public class CameraFollow : Singleton<CameraFollow>
{
    [SerializeField] protected float m_ForwardThreshold = 5f, m_DampTime = 0.2f;

    private Vector3 m_MoveVelocity;
    private TankMovement m_TankToFollowMovement;
    private Transform m_TankToFollowTransform;

    private void Update()
    {
        FollowTank();
    }

    public void SetTankToFollow(Transform tankTransform, TankMovement movement)
    {
        m_TankToFollowTransform = tankTransform;
        m_TankToFollowMovement = movement;
    }

    private void FollowTank()
    {
        if (m_TankToFollowTransform == null || m_TankToFollowMovement == null) return;

        var tankPosition = m_TankToFollowTransform.position;
        var targetPosition = new Vector3(tankPosition.x, transform.position.y, tankPosition.z);
        targetPosition = targetPosition + m_ForwardThreshold * m_TankToFollowTransform.forward *
                         (float) m_TankToFollowMovement.currentMovementMode;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref m_MoveVelocity, m_DampTime,
            float.PositiveInfinity, Time.unscaledDeltaTime);
    }
}