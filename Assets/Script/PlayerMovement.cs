using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public KeyCode runKey = KeyCode.LeftShift;

    public float gravity = -9.81f;
    private float groundGravity = -2f;

    private CharacterController characterController;
    private Vector3 velocity;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Set speed (run or walk)
        float currentSpeed = Input.GetKey(runKey) ? runSpeed : walkSpeed;

        // Move player
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // Apply gravity
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = groundGravity;
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}
