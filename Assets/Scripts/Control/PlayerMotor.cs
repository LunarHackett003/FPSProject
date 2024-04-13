using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMotor : NetworkBehaviour
{
    [SerializeField] CharacterController controller;
    [SerializeField] float groundMoveSpeed, airMoveForce;
    [SerializeField] Vector2 moveInput;
    [SerializeField] float groundedDrag, airDrag;

    [SerializeField] Vector3 velocity;
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

    [SerializeField, Header("Stamina")] float stamina;
    [SerializeField] float maximumStamina, staminaRegenRate, staminaPerSecSprint, staminaPerJump, minStaminaJumpForce, maxStaminaJumpForce;
    [SerializeField] float jumpCooldown;
    [SerializeField] bool jumpBlocked;


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

        if (stamina <= 0)
            sprinting = false;
        stamina = Mathf.Clamp(stamina + ((sprinting && moveInput != Vector2.zero) ? -staminaPerSecSprint : staminaRegenRate) * Time.fixedDeltaTime, 0, maximumStamina);

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y)
            * (grounded ?  (groundMoveSpeed * (sprinting ? (tacSprinting ? sprintMultiplier + tacSprintBoost : sprintMultiplier) : 1))
            : airMoveForce);
        if (grounded)
            move = Vector3.ProjectOnPlane(move, groundNormal);
        if (grounded)
            controller.Move(transform.rotation * move * Time.fixedDeltaTime);
        else
            velocity += Time.fixedDeltaTime * (transform.rotation *  move);
        velocity /= Mathf.Max(grounded ? groundedDrag : airDrag, 1.01f);
        controller.Move(velocity * Time.fixedDeltaTime);
    }
    void CheckGround()
    {
        grounded = Physics.SphereCast(transform.position, groundCheckRadius, -transform.up, out RaycastHit hit, groundCheckDistance, groundCheckLayermask) && Vector3.Dot(hit.normal, transform.up) > walkableDotThreshold;
        if (grounded)
        {
            groundNormal = hit.normal;
            if(velocity.y < 0)
                velocity.y = -groundMoveSpeed * groundedDrag;
        }
        else
        {
            velocity += Physics.gravity * Time.fixedDeltaTime;
        }
    }
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && grounded && stamina > 0 && !jumpBlocked)
        {
        stamina -= staminaPerJump;
            StartCoroutine(JumpCD());
            velocity = transform.up
                       * Mathf.Lerp(minStaminaJumpForce, maxStaminaJumpForce, Mathf.InverseLerp(0, maximumStamina, stamina))
                       + (transform.rotation * (groundMoveSpeed * new Vector3(moveInput.x, 0, moveInput.y)));
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
        animator.SetFloat("Horizontal", moveInput.x);
        animator.SetFloat("Vertical", moveInput.y);
    }
}
