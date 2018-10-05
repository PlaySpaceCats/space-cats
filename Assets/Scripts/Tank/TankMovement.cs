using UnityEngine;

public class TankMovement : MonoBehaviour
{
    public enum MovementMode
    {
        Forward = 1,
        Backward = -1
    }

    private const float TurnSpeed = 180f;

    private bool _isServer;

    private float _speed = 11f;

    private Vector2 m_DesiredDirection;

    private Vector2 m_DesiredPosition;

    [SerializeField] protected AudioClip m_EngineDriving;

    [SerializeField] protected AudioClip m_EngineIdling;

    [SerializeField] protected AudioClip m_EngineStartDriving;

    private bool m_HadMovementInput;

    private Vector3 m_LastPosition;

    [SerializeField] protected AudioSource m_MovementAudio;

    private RigidbodyConstraints m_OriginalConstrains;

    public Rigidbody Rigidbody { get; private set; }

    public MovementMode currentMovementMode { get; private set; }

    private Vector3 velocity { get; set; }

    public bool isMoving
    {
        get
        {
            if (_isServer) return m_DesiredDirection.sqrMagnitude > 0.01f;

            return (new Vector3(m_DesiredPosition.x, 0, m_DesiredPosition.y) - transform.position).normalized
                   .sqrMagnitude > 0.01f;
        }
    }

    public void Init(bool isServer)
    {
        enabled = false;

        _isServer = isServer;

        SetDefaults();
    }

    public void SetDesiredMovementDirection(Vector2 moveDir)
    {
        m_DesiredDirection = moveDir;
        m_HadMovementInput = true;

        if (m_DesiredDirection.sqrMagnitude > 1) m_DesiredDirection.Normalize();
    }

    public void SetDesiredMovementPosition(Vector2 movePos)
    {
        m_DesiredPosition = movePos;
    }

    private void Awake()
    {
        LazyLoadRigidBody();
        m_OriginalConstrains = Rigidbody.constraints;

        currentMovementMode = MovementMode.Forward;
    }

    private void LazyLoadRigidBody()
    {
        if (Rigidbody != null) return;

        Rigidbody = GetComponent<Rigidbody>();
    }


    private void Start()
    {
        m_LastPosition = transform.position;
    }

    private void Update()
    {
        if (!m_HadMovementInput || !isMoving) m_DesiredDirection = Vector2.zero;

        EngineAudio();
    }

    private void EngineAudio()
    {
        if (_speed <= float.Epsilon) return;

        if ((m_LastPosition - transform.position).sqrMagnitude <= Mathf.Epsilon)
        {
            if (m_MovementAudio.clip == m_EngineIdling)
            {
                return;
            }
            m_MovementAudio.loop = true;
            m_MovementAudio.clip = m_EngineIdling;
            m_MovementAudio.Play();
        }
        else
        {
            if (m_MovementAudio.clip == m_EngineIdling)
            {
                m_MovementAudio.clip = m_EngineStartDriving;

                m_MovementAudio.loop = false;
                m_MovementAudio.Play();
            }
            else
            {
                if (m_MovementAudio.clip != m_EngineStartDriving || m_MovementAudio.isPlaying)
                {
                    return;
                }
                m_MovementAudio.loop = true;

                m_MovementAudio.clip = m_EngineDriving;

                m_MovementAudio.Play();
            }
        }
    }

    private void FixedUpdate()
    {
        velocity = transform.position - m_LastPosition;
        m_LastPosition = transform.position;

        if (!isMoving) return;

        if (_isServer)
        {
            Turn();
            Move();
        }
        else
        {
            TurnClient();
            MoveClient();
        }
    }


    private void Move()
    {
        var moveDistance = m_DesiredDirection.magnitude * _speed * Time.deltaTime;

        var movement = currentMovementMode == MovementMode.Backward ? -transform.forward : transform.forward;
        movement *= moveDistance;

        Rigidbody.position = Rigidbody.position + movement;
    }


    private void MoveClient()
    {
        Rigidbody.position = Vector3.MoveTowards(transform.position,
            new Vector3(m_DesiredPosition.x, 0, m_DesiredPosition.y),
            _speed * Time.deltaTime);
        transform.position = Rigidbody.position;
    }


    private void Turn()
    {
        var desiredAngle = 90 - Mathf.Atan2(m_DesiredDirection.y, m_DesiredDirection.x) * Mathf.Rad2Deg;

        var facing = new Vector2(transform.forward.x, transform.forward.z);
        var facingDot = Vector2.Dot(facing, m_DesiredDirection);

        if (currentMovementMode == MovementMode.Forward &&
            facingDot < -0.5)
            currentMovementMode = MovementMode.Backward;
        if (currentMovementMode == MovementMode.Backward &&
            facingDot > 0.5)
            currentMovementMode = MovementMode.Forward;

        if (currentMovementMode == MovementMode.Backward) desiredAngle += 180;

        var turn = TurnSpeed * Time.deltaTime;

        var desiredRotation = Quaternion.Euler(0f, desiredAngle, 0f);

        Rigidbody.rotation = Quaternion.RotateTowards(Rigidbody.rotation, desiredRotation, turn);
        transform.rotation = Rigidbody.rotation;
    }


    private void TurnClient()
    {
        var currentPosition = new Vector2(transform.position.x, transform.position.z);
        var desiredDirection = (m_DesiredPosition - currentPosition).normalized;
        if (desiredDirection.sqrMagnitude <= 0.01f) return;
        var desiredAngle = 90 - Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg;

        var facing = new Vector2(transform.forward.x, transform.forward.z);
        var facingDot = Vector2.Dot(facing, desiredDirection);

        if (facingDot < -0.5) currentMovementMode = MovementMode.Backward;
        if (facingDot > 0.5) currentMovementMode = MovementMode.Forward;

        if (currentMovementMode == MovementMode.Backward) desiredAngle += 180;

        var turn = TurnSpeed * Time.deltaTime;

        var desiredRotation = Quaternion.Euler(0f, desiredAngle, 0f);

        Rigidbody.rotation = Quaternion.RotateTowards(Rigidbody.rotation, desiredRotation, turn);
        transform.rotation = Rigidbody.rotation;
    }

    public void SetDefaults()
    {
        enabled = true;
        LazyLoadRigidBody();

        Rigidbody.velocity = Vector3.zero;
        Rigidbody.angularVelocity = Vector3.zero;

        m_DesiredDirection = Vector2.zero;
        currentMovementMode = MovementMode.Forward;
    }

    public void DisableMovement()
    {
        _speed = 0;
        m_MovementAudio.enabled = false;
    }

    public void EnableMovement()
    {
        _speed = 11f;
        m_MovementAudio.enabled = true;
    }

    public void SetAudioSourceActive(bool isActive)
    {
        if (m_MovementAudio != null) m_MovementAudio.enabled = isActive;
    }

    private void OnDisable()
    {
        m_OriginalConstrains = Rigidbody.constraints;
        Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    private void OnEnable()
    {
        Rigidbody.constraints = m_OriginalConstrains;
    }
}