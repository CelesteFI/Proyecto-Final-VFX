using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Aceleración y Suavizado")]
    public float acceleration = 14f;
    public float deceleration = 18f;
    public float rotationSmoothTime = 0.08f;

    [Header("Física y Salto Asistido")]
    public float jumpHeight = 2.0f;
    public float gravity = -20f;
    public float fallMultiplier = 1.8f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.12f;

    [Header("Dash")]
    public float dashDuration = 0.22f;

    private CharacterController controller;
    private Transform camTransform;
    private TagTeamManager tagTeam;

    private Vector3 currentMoveVelocity;
    private float verticalVelocityY;
    private float turnSmoothVelocity;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        tagTeam = GetComponent<TagTeamManager>();
        if (Camera.main != null) camTransform = Camera.main.transform;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (kb == null) return;

        HeroCombatData activeHero = (tagTeam != null) ? tagTeam.ActiveHero : null;
        float baseSpeed = (activeHero != null) ? activeHero.moveSpeed : 7f;
        float dashSpeed = (activeHero != null) ? activeHero.dashSpeed : 16f;

        // 1. Detección de suelo y Coyote Time
        if (controller.isGrounded)
        {
            coyoteCounter = coyoteTime;
            if (verticalVelocityY < 0f) verticalVelocityY = -2f;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // 2. Buffer de salto
        if (kb.spaceKey.wasPressedThisFrame) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        // 3. Ejecutar salto si las ventanas coinciden
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !isDashing)
        {
            verticalVelocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // 4. Dash
        if (kb.leftShiftKey.wasPressedThisFrame && !isDashing)
        {
            IniciarDash(kb);
        }

        // 5. Locomoción horizontal
        if (isDashing)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) isDashing = false;
        }
        else
        {
            ProcesarMovimiento(kb, baseSpeed);
        }

        // 6. Gravedad con caída rápida responsiva
        if (!isDashing)
        {
            float mult = (verticalVelocityY < 0f) ? fallMultiplier : 1f;
            verticalVelocityY += gravity * mult * Time.deltaTime;
            controller.Move(new Vector3(0, verticalVelocityY, 0) * Time.deltaTime);
        }
    }

    private void ProcesarMovimiento(Keyboard kb, float speed)
    {
        float h = 0f; float v = 0f;
        if (kb.wKey.isPressed) v += 1f;
        if (kb.sKey.isPressed) v -= 1f;
        if (kb.dKey.isPressed) h += 1f;
        if (kb.aKey.isPressed) h -= 1f;

        Vector3 targetDir = new Vector3(h, 0f, v).normalized;
        Vector3 targetVelocity = Vector3.zero;

        if (targetDir.magnitude >= 0.1f && camTransform != null)
        {
            float targetAngle = Mathf.Atan2(targetDir.x, targetDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            targetVelocity = (Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward).normalized * speed;
        }

        // Aceleración / desaceleración suave
        float rate = (targetVelocity.magnitude > 0.1f) ? acceleration : deceleration;
        currentMoveVelocity = Vector3.MoveTowards(currentMoveVelocity, targetVelocity, rate * Time.deltaTime);

        controller.Move(currentMoveVelocity * Time.deltaTime);
    }

    private void IniciarDash(Keyboard kb)
    {
        isDashing = true;
        dashTimer = dashDuration;
        verticalVelocityY = 0f; // Suspender caída durante el dash

        float h = 0f; float v = 0f;
        if (kb.wKey.isPressed) v += 1f;
        if (kb.sKey.isPressed) v -= 1f;
        if (kb.dKey.isPressed) h += 1f;
        if (kb.aKey.isPressed) h -= 1f;
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude >= 0.1f && camTransform != null)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            dashDirection = (Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward).normalized;
        }
        else
        {
            dashDirection = transform.forward;
        }
    }
}