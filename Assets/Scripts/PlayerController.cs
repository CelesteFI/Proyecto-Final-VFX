using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float walkSpeed = 6f;
    public float rotationSpeed = 10f;
    public float gravity = -15f;

    private CharacterController controller;
    private Vector3 velocity;
    private Transform cam;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
    }

    void Update()
    {
        // Lectura de teclas WASD
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Reset de gravedad al estar en el suelo
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Movimiento relativo a la cámara
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * walkSpeed * Time.deltaTime);
        }

        // Aplicar gravedad
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}