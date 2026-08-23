using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMove : MonoBehaviour
{
    private Vector2 moveDir;
    private Rigidbody2D playerRb;
    private AnimationController animationController;
    public float speed = 3;

    public void Awake()
    {
        playerRb = GetComponent<Rigidbody2D>();
        animationController = GetComponent<AnimationController>();
    }
    
    public void Move(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            moveDir = context.ReadValue<Vector2>();
            playerRb.linearVelocity = moveDir * speed;
            animationController.Move(true);

            if (moveDir.x != 0)
                transform.localScale = new Vector3(
                    Mathf.Sign(moveDir.x) * Mathf.Abs(transform.localScale.x),
                    transform.localScale.y,
                    transform.localScale.z
                );
        }
        else
        {
            playerRb.linearVelocity = Vector2.zero;
            animationController.Move(false);
        }
    }
}
