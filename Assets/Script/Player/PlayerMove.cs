using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMove : MonoBehaviour
{
    private Vector2 moveDir;
    private Rigidbody2D playerRb;
    private Animator animator;
    private float speed = 3;

    public void Awake()
    {
        playerRb = GetComponent<Rigidbody2D>();
    }
    
    public void Move(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            moveDir = context.ReadValue<Vector2>();
            playerRb.linearVelocity = moveDir * speed;
        }
        else
        {
            playerRb.linearVelocity = Vector2.zero;
        }
    }
}
