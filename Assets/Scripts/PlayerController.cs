using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    public static event Action OnOrbCollected;
    public static event Action OnPlayerDied;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float gravityMagnitude = 9.81f;

    [Header("References")]
    public Transform visualTransform;
    public Animator animator;
    public Transform hologramIndicator;
    public Transform headPoint;
    public Transform cameraTransform;

    [Header("Gravity Transition")]
    public float headClearanceOffset = 0.1f;

    //State Variables
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector3 currentGravityDir = Vector3.down;
    private Vector3 targetGravityDir;
    private bool isHologramActive = false;
    private bool isGroundedCustom = false;
    private bool jumpRequested = false;
    private bool canMove = true;
    
    //Animation const
    private readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    
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
        canMove = false;
        jumpRequested = false;
    }

    private void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        //setup rb
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;

        if(cameraTransform) cameraTransform = Camera.main.transform;
        
        if (hologramIndicator)
            hologramIndicator.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (canMove)
        {
            HandleGravityChange();
            
            //Queue Jump
            if (Input.GetButtonDown("Jump") && isGroundedCustom){
                jumpRequested = true;
            }
        }

        HandleAnimations();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        
        if (canMove)
        {
            HandleMovement();
        }
        else
        {
            // Nullify horizontal velocity to stop slipping, retain vertical free fall
            rb.velocity = Vector3.Project(rb.velocity, currentGravityDir.normalized);
        }

        //gravity
        rb.AddForce(currentGravityDir.normalized * gravityMagnitude, ForceMode.Acceleration);
    }

    #region Movement
    
    private void CheckGrounded()
    {
        if (!capsuleCollider) return;

        Vector3 capsuleCenter = transform.TransformPoint(capsuleCollider.center);
        float radius = capsuleCollider.radius * 0.9f;
        float castDistance = (capsuleCollider.height / 2f) - radius + 0.1f;

        // gravity check
        if (Physics.SphereCast(capsuleCenter, radius, currentGravityDir.normalized, out RaycastHit hit, castDistance)){
            isGroundedCustom = !hit.collider.isTrigger;
        }
        else{
            isGroundedCustom = false;
        }
    }
    
    private void HandleMovement()
    {
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        Vector3 localMoveDirection = Vector3.zero;

        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            Vector3 upDirection = -currentGravityDir.normalized;

            camForward = Vector3.ProjectOnPlane(camForward, upDirection).normalized;
            camRight = Vector3.ProjectOnPlane(camRight, upDirection).normalized;

            localMoveDirection = (camRight * moveHorizontal + camForward * moveVertical).normalized;
        }
        else
            localMoveDirection = (transform.right * moveHorizontal + transform.forward * moveVertical).normalized;

        // separate vertical velocity
        Vector3 currentVelocity = rb.velocity;
        Vector3 fallingVelocity = Vector3.Project(currentVelocity, currentGravityDir.normalized);
        
        Vector3 targetMoveVelocity = localMoveDirection * moveSpeed;
        rb.velocity = targetMoveVelocity + fallingVelocity;

        // jump
        if (jumpRequested){
            rb.velocity -= Vector3.Project(rb.velocity, currentGravityDir.normalized); //nullify current downwards velocity
            rb.AddForce(-currentGravityDir.normalized * jumpForce, ForceMode.VelocityChange);
            jumpRequested = false;
        }

        // update visual rotation
        if (localMoveDirection.magnitude > 0.01f && visualTransform){
            Quaternion faceDirection = Quaternion.LookRotation(localMoveDirection, transform.up);
            visualTransform.rotation = Quaternion.Slerp(visualTransform.rotation, faceDirection, 15f * Time.deltaTime);
        }
    }

    #endregion

    #region Animation

    private void HandleAnimations()
    {
        if (!animator) return;
        
        Vector3 moveVelocity = rb.velocity;
        moveVelocity -= Vector3.Project(moveVelocity, currentGravityDir.normalized);

        float horizontalSpeed = moveVelocity.magnitude;
        animator.SetBool(IsRunningHash, horizontalSpeed > 0.1f);

        bool isFalling = !isGroundedCustom && Vector3.Dot(rb.velocity, currentGravityDir.normalized) > 0.5f;
        animator.SetBool(IsFallingHash, isFalling);
    }

    #endregion

    #region Gravity Manuplation
    
    private void HandleGravityChange()
    {
        bool axisSelectedThisFrame = false;

        Vector3 refForward = transform.forward;
        Vector3 refRight = transform.right;

        if (cameraTransform)
        {
            refForward = Vector3.ProjectOnPlane(cameraTransform.forward, transform.up).normalized;
            refRight = Vector3.ProjectOnPlane(cameraTransform.right, transform.up).normalized;
        }
        
        if (Input.GetKeyDown(KeyCode.UpArrow)){
            targetGravityDir = GetClosestWorldAxis(refForward);
            axisSelectedThisFrame = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow)){
            targetGravityDir = GetClosestWorldAxis(-refForward);
            axisSelectedThisFrame = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow)){
            targetGravityDir = GetClosestWorldAxis(-refRight);
            axisSelectedThisFrame = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow)){
            targetGravityDir = GetClosestWorldAxis(refRight);
            axisSelectedThisFrame = true;
        }

        // update hologram
        if (axisSelectedThisFrame){
            isHologramActive = true;
            if (hologramIndicator){
                hologramIndicator.gameObject.SetActive(true);

                // Point hologram's designated forward axis towards the player's core `Up` direction.
                Vector3 holoForward = transform.up;
                Vector3 holoUp = -targetGravityDir;

                hologramIndicator.rotation = Quaternion.LookRotation(holoForward, holoUp);
            }
        }

        // Apply gravity change
        if (Input.GetKeyDown(KeyCode.Return)){
            if (isHologramActive && targetGravityDir != currentGravityDir)
                    ApplyGravity(targetGravityDir);
        }
    }
    
    /// <summary>
    /// Identifies the absolute closest unified world axis aligning to a custom directional pointer.
    /// </summary>
    private Vector3 GetClosestWorldAxis(Vector3 direction)
    {
        Vector3[] worldAxes = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        Vector3 closestAxis = Vector3.down;
        float maximumDotProduct = -Mathf.Infinity;

        foreach (var axis in worldAxes){
            float currentDot = Vector3.Dot(direction.normalized, axis);
            if (currentDot > maximumDotProduct){
                maximumDotProduct = currentDot;
                closestAxis = axis;
            }
        }

        return closestAxis;
    }
    
    private void ApplyGravity(Vector3 newGravityDirection)
    {
        currentGravityDir = newGravityDirection;
        isHologramActive = false;

        if (hologramIndicator) hologramIndicator.gameObject.SetActive(false);
        
        Vector3 newUp = -currentGravityDir;

        //Compute the target rotation
        Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, newUp);
        if (projectedForward.sqrMagnitude < 0.001f){
            projectedForward = Vector3.ProjectOnPlane(transform.right, newUp);
        }
        projectedForward.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(projectedForward, newUp);

        //HeadPoint raycast logic
        Vector3 headPos = headPoint.position;
        float colliderHeight = capsuleCollider.height;
        Vector3 rayDir = currentGravityDir.normalized;

        bool didHit = Physics.Raycast(headPos, rayDir, out RaycastHit hit);
        float hitDistance = didHit ? hit.distance : Mathf.Infinity;

        if (!didHit || hitDistance > colliderHeight)
        {
            //Enough space, simply rotate around HeadPoint as pivot
            RotateAroundHeadPoint(headPos, targetRotation);
        }
        else
        {
            // Not enough space, teleport first, then rotate
            Vector3 safeHeadPos = hit.point + (-rayDir) * (colliderHeight + headClearanceOffset);
            Vector3 positionShift = safeHeadPos - headPos;
            rb.MovePosition(rb.position + positionShift);

            // Recalculate head position after teleport
            Vector3 newHeadPos = headPoint.position + positionShift;
            RotateAroundHeadPoint(newHeadPos, targetRotation);
        }

        //nullify velocity so the player cleanly engages the new drop mechanics
        rb.velocity = Vector3.zero;
    }

    private void RotateAroundHeadPoint(Vector3 pivotWorldPos, Quaternion targetRotation)
    {
        Vector3 pivotToBody = rb.position - pivotWorldPos;

        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.rotation);

        Vector3 rotatedOffset = deltaRotation * pivotToBody;

        // Apply rotation and corrected position via Rigidbody
        rb.MoveRotation(targetRotation);
        rb.MovePosition(pivotWorldPos + rotatedOffset);
    }
    
    #endregion

    #region Interactions

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Orb"))
        {
            CollectOrb(other.gameObject);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Void"))
        {
            OnPlayerDied?.Invoke();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Orb"))
        {
            CollectOrb(collision.gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Void"))
        {
            OnPlayerDied?.Invoke();
        }
    }

    private void CollectOrb(GameObject orb)
    {
        Destroy(orb);
        OnOrbCollected?.Invoke();
    }

    #endregion
}