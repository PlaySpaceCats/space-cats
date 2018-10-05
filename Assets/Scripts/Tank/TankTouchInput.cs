using Tanks.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

public class TankTouchInput : TankInputModule
{
    private Touch? m_FiringTouch;
    private int m_FiringTouchId;

    private const float m_HorizontalArea = 0.24f;

	[SerializeField] protected GameObject m_MovementIndicator;

    private Touch? m_MovementTouch;

    private int m_MovementTouchId;

    private Vector2 m_PadCenter;
    private bool m_StoppedFiring;

    private bool m_StoppedMoving;

	[SerializeField] protected float m_TouchDistCenterPull = 0.1f;

	[SerializeField] protected float m_TouchDistMaxSensitivity = 0.8f;

	[SerializeField] protected float m_TouchDistMinSensitivity = 0.05f;

    private const float m_VerticalArea = 0.32f;

    protected override void Awake()
    {
        base.Awake();
        Input.simulateMouseWithTouches = false;
    }

    public void Start()
    {
        OnBecomesActive();
    }

    public void ForceUpdateUi()
    {
        UpdateUi();
    }

    protected override void Update()
    {
        ProcessTouches();

        base.Update();

        UpdateUi();
    }

    protected override void OnBecomesActive()
    {
        if (HUDController.Instance != null) HUDController.Instance.ShowVPad(m_HorizontalArea, m_VerticalArea);

        if (m_MovementIndicator != null) m_MovementIndicator.SetActive(true);
    }

    protected override void OnBecomesInactive()
    {
        if (HUDController.Instance != null) HUDController.Instance.HideVPad();

        if (m_MovementIndicator != null) m_MovementIndicator.SetActive(false);

        m_MovementTouchId = -1;
        m_FiringTouchId = -1;
    }

	protected virtual void UpdateUi()
    {
        var hud = HUDController.Instance;
        if (hud == null)
        {
            return;
        }
        if (m_MovementTouch != null)
        {
            hud.UpdateVPad(m_PadCenter, m_MovementTouch.Value.position, true);
            hud.SetVPadHeld();
        }
        else
        {
            hud.UpdateVPad(Vector2.zero, Vector2.zero, false);
            hud.SetVPadReleased();
        }
    }

	protected virtual void ProcessTouches()
    {
        m_MovementTouch = null;
        m_FiringTouch = null;
        m_StoppedMoving = false;
        m_StoppedFiring = false;

        for (var i = 0; i < Input.touchCount; ++i)
        {
            var touch = Input.GetTouch(i);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (IsValidMovementTouch(touch))
                    {
                        m_MovementTouchId = touch.fingerId;
                        m_MovementTouch = touch;
                        m_PadCenter = touch.position;
                    }
                    else if (IsValidFireTouch(touch) && touch.fingerId != m_MovementTouchId)
                    {
                        m_FiringTouchId = touch.fingerId;
                        m_FiringTouch = touch;
                    }

                    break;

                case TouchPhase.Canceled:
                case TouchPhase.Ended:
                    if (touch.fingerId == m_FiringTouchId)
                    {
                        m_FiringTouchId = -1;
                        m_FiringTouch = null;
                        m_StoppedFiring = true;
                    }

                    if (touch.fingerId == m_MovementTouchId)
                    {
                        m_MovementTouchId = -1;
                        m_MovementTouch = null;
                        m_StoppedMoving = true;
                    }

                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (touch.fingerId == m_FiringTouchId) m_FiringTouch = touch;

                    if (touch.fingerId == m_MovementTouchId) m_MovementTouch = touch;
                    break;
            }
        }
    }

	protected override bool DoFiringInput()
    {
        if (m_StoppedFiring)
        {
            SetFireIsHeld(false);
        }
        else if (m_FiringTouch != null)
        {
            var mouseRay = Camera.main.ScreenPointToRay(m_FiringTouch.Value.position);
            float hitDist;
            RaycastHit hit;
            if (Physics.Raycast(mouseRay, out hit, float.PositiveInfinity, GroundLayerMask))
                SetDesiredFirePosition(hit.point);
            else if (FloorPlane.Raycast(mouseRay, out hitDist)) SetDesiredFirePosition(mouseRay.GetPoint(hitDist));

            SetFireIsHeld(true);

            return true;
        }

        return false;
    }

	protected override bool DoMovementInput()
    {
        if (m_MovementTouch != null && !m_StoppedMoving)
        {
            var fingerpos = m_MovementTouch.Value.position;
            var movementDir = fingerpos - m_PadCenter;
            var magnitude = movementDir.magnitude;
            var dpi = Screen.dpi > 0 ? Screen.dpi : 72;

            var minTouch = m_TouchDistMinSensitivity * dpi;
            var maxTouch = m_TouchDistMaxSensitivity * dpi;
            var touchPullThreshold = maxTouch + m_TouchDistCenterPull * dpi;

            var moveSpeed = Mathf.Clamp01((magnitude - minTouch) / (maxTouch - minTouch));

            var cameraDirection = new Vector3(movementDir.x, movementDir.y, 0);

            if (cameraDirection.sqrMagnitude > 0.01f)
            {
                var worldUp = Camera.main.transform.TransformDirection(Vector3.up);
                worldUp.y = 0;
                worldUp.Normalize();
                var worldRight = Camera.main.transform.TransformDirection(Vector3.right);
                worldRight.y = 0;
                worldRight.Normalize();

                var worldDirection = worldUp * movementDir.y + worldRight * movementDir.x;
                worldDirection.Normalize();
                worldDirection *= moveSpeed;
                var moveDir = new Vector2(worldDirection.x, worldDirection.z);
                SetDesiredMovementDirection(moveDir);

                if (m_FiringTouch == null) SetDesiredFirePosition(new Vector3(moveDir.x, 0, moveDir.y));

                if (m_MovementIndicator != null)
                {
                    m_MovementIndicator.SetActive(true);
                    var moveAngle = 90 - Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
                    m_MovementIndicator.transform.rotation = Quaternion.AngleAxis(moveAngle, Vector3.up);
                    m_MovementIndicator.transform.localScale = new Vector3(1, 1, Mathf.Lerp(0.5f, 1.0f, moveSpeed));
                }
            }
            else if (m_MovementIndicator != null)
            {
                SetDesiredMovementDirection(Vector2.zero);
                m_MovementIndicator.SetActive(false);
            }
            else
            {
                SetDesiredMovementDirection(Vector2.zero);
            }

            if (magnitude <= touchPullThreshold)
            {
                return true;
            }
            var normalizedMovementDir = movementDir / magnitude;
            m_PadCenter = fingerpos - normalizedMovementDir * touchPullThreshold;

            return true;
        }
        SetDesiredMovementDirection(Vector2.zero);
        if (m_MovementIndicator != null) m_MovementIndicator.SetActive(false);

        return false;
    }

    private bool IsValidMovementTouch(Touch touch)
    {
        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return false;

        if (m_MovementTouchId >= 0) return false;

        var normalizedTouch = Vector2.Scale(touch.position, new Vector2(1.0f / Screen.width, 1.0f / Screen.height));

        return normalizedTouch.x <= m_HorizontalArea &&
               normalizedTouch.y <= m_VerticalArea;
    }

    private bool IsValidFireTouch(Touch touch)
    {
        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return false;

        return m_FiringTouchId < 0;
    }
}