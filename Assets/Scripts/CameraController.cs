using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target & Positioning")]
    public Transform target;
    public float distance = 5f;

    [Header("Input Settings")]
    public float mouseSensitivity = 2f;
    public Vector2 pitchMinMax = new Vector2(-20, 85);

    [Header("Collision Avoidance")]
    public LayerMask collisionMask; 
    public float cameraRadius = 0.25f;

    private float pitch;
    private float yaw;
    private bool canMoveCamera = true;

    private void OnEnable()
    {
        GameManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= HandleGameOver;
    }

    private void HandleGameOver(string message)
    {
        canMoveCamera = false;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (target)
        {
            Vector3 euler = transform.eulerAngles;
            pitch = euler.x;
            yaw = euler.y;
        }
    }

    private void LateUpdate()
    {
        if (!target) return;

        //input
        if (canMoveCamera)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
        }

        //compute base orbit rotation relative to target's UP axis
        Quaternion alignment = Quaternion.FromToRotation(Vector3.up, target.up);
        Quaternion localOrbit = Quaternion.Euler(pitch, yaw, 0f);
        Quaternion cameraRotation = alignment * localOrbit;
        
        Vector3 desiredPosition = target.position - (cameraRotation * Vector3.forward) * distance;

        //obstacle avoidance
        Vector3 dirToCamera = (desiredPosition - target.position).normalized;
        float distToCamera = distance;

        if (Physics.Linecast(target.position, desiredPosition, out RaycastHit hit, collisionMask, QueryTriggerInteraction.Ignore))
        {
            distToCamera = Mathf.Clamp(hit.distance - cameraRadius, 0.1f, distance);
        }

        Vector3 finalPosition = target.position + dirToCamera * distToCamera;
        
        transform.position = finalPosition;
        transform.rotation = Quaternion.LookRotation((target.position - finalPosition).normalized, alignment * Vector3.up);
    }
}
