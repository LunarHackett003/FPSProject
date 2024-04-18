using FishNet.Component.Animating;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class PlayerMotor : NetworkBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] NetworkAnimator netAnim;
    [SerializeField] float groundMoveSpeed, airMoveForce;
    [SerializeField] Vector2 moveInput;
    [SerializeField] float groundedDrag, airDrag;

    [SerializeField] float lookAngle;
    [SerializeField] Vector2 lookSpeed;
    [SerializeField] bool freeLooking;
    [SerializeField] Vector2 freeLookPosDeviance;
    [SerializeField] float freeLookSpeed,freeLookSnapbackTime,freeLookMaxPosDeviance;
    [SerializeField] Transform aimTransform, freeLookAimTarget;
    Vector3 flAimTargStartPos;
    //Helps to turn the head properly by adding forward offset to the aim target
    [SerializeField] float freeLookMaxRearOffset;

    [SerializeField] bool grounded;
    [SerializeField] float groundCheckDistance, groundCheckRadius;
    [SerializeField] LayerMask groundCheckLayermask;
    [SerializeField] float walkableDotThreshold;
    [SerializeField] Vector3 groundNormal;

    [SerializeField] bool sprinting, tacSprinting;
    [SerializeField] float sprintMultiplier, tacSprintBoost, tacSprintTime, maxTacSprintTime;

    [SerializeField] Animator animator;
    
    [SerializeField] float jumpCooldown, jumpForce, verticalVelocityScalar;
    [SerializeField] bool jumpBlocked, doubleJumped;

    [SerializeField] Renderer bodyRenderer;
    
    [SerializeField, Tooltip("For local players, the weight on this will be set to 1 to make sure everything lines up correctly")] MultiRotationConstraint highestSpineConstraint;
    [SerializeField, Tooltip("The hand constraint - when a weapon is equipped, the weight for this will be set to 1")] 
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
        {
            bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            highestSpineConstraint.weight = 1;
        }
        else
        {
            highestSpineConstraint.weight = 0.6f;
        }
    }
    private void Start()
    {
        flAimTargStartPos = freeLookAimTarget.localPosition;
    }
    private void FixedUpdate()
    {
        if(IsOwner)
        Movement();


    }
    void Movement()
    {
        CheckGround();
        animator.SetBool("Moving",moveInput != Vector2.zero);

        animator.SetFloat("Horizontal", moveInput.x, 0.1f, Time.smoothDeltaTime);
        animator.SetFloat("Vertical", moveInput.y, 0.1f, Time.smoothDeltaTime);

        rb.drag = grounded ? groundedDrag : airDrag;

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y)
            * (grounded ?  (groundMoveSpeed * (sprinting ? (tacSprinting ? sprintMultiplier + tacSprintBoost : sprintMultiplier) : 1))
            : airMoveForce);
        if (grounded)
            move = Vector3.ProjectOnPlane(move, groundNormal);
        rb.AddRelativeForce(move);
    }
    void CheckGround()
    {
        bool oldGrounded = grounded;
        grounded = Physics.SphereCast(transform.position, groundCheckRadius, -transform.up, out RaycastHit hit, groundCheckDistance, groundCheckLayermask) && Vector3.Dot(hit.normal, transform.up) > walkableDotThreshold;
        groundNormal = grounded ? hit.normal : transform.up;
        if (grounded)
        {
            doubleJumped = false;
            //We've also just landed, check if we've just jumped
            if(!jumpBlocked && !oldGrounded)
                netAnim.SetTrigger("Landing");
        }
    }
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && ((grounded && !jumpBlocked) || (!grounded && !doubleJumped)))
        {
            if (!grounded)
                doubleJumped = true;
            if (grounded)
                netAnim.SetTrigger("Jump");
            netAnim.ResetTrigger("Landing");
            StartCoroutine(JumpCD());
            rb.AddForce((transform.up * jumpForce) + (Mathf.Abs(Mathf.Min(rb.velocity.y, 0)) * verticalVelocityScalar * transform.up), ForceMode.Impulse);
        }
    }
    public void LookInput(InputAction.CallbackContext context)
    {
            Vector2 lookInput = context.ReadValue<Vector2>() * Time.smoothDeltaTime;
        if (!freeLooking)
        {
            lookInput *= lookSpeed;
            lookAngle = Mathf.Clamp(lookAngle + lookInput.y, -85f, 85f);
            transform.Rotate(transform.up, lookInput.x);
            aimTransform.localRotation = Quaternion.Euler(-lookAngle,0 ,0);
        }
        else
        {
            freeLookPosDeviance += lookInput * freeLookSpeed;
            freeLookPosDeviance = Vector3.ClampMagnitude(freeLookPosDeviance, freeLookMaxPosDeviance);
            UpdateAimTarget();
        }
    }
    void UpdateAimTarget()
    {
        freeLookAimTarget.localPosition = flAimTargStartPos + Vector3.Lerp(Vector3.zero, (Vector3.back * freeLookMaxRearOffset), Mathf.InverseLerp(0, freeLookMaxPosDeviance, freeLookPosDeviance.magnitude)) + (Vector3)freeLookPosDeviance;
    }
    public void FreeLookInput(InputAction.CallbackContext context)
    {
        freeLooking = context.performed || context.started;
        
        if(!freeLooking)
            StartCoroutine(FreeLookSnapback());
        else
            StopCoroutine(FreeLookSnapback());
    }
    IEnumerator FreeLookSnapback()
    {
        float time = 0;
        while (time < freeLookSnapbackTime)
        {
            time += Time.fixedDeltaTime;
            freeLookPosDeviance = Vector3.Lerp(freeLookPosDeviance, Vector3.zero, Mathf.InverseLerp(0, freeLookSnapbackTime, time));
            UpdateAimTarget();
            yield return new WaitForFixedUpdate();
        }
    }
    IEnumerator JumpCD()
    {
        jumpBlocked = true;
        yield return new WaitForSeconds(jumpCooldown);
        jumpBlocked = false;
        yield break;
    }
    public void MoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

    }
}
