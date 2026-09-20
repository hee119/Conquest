using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMove : MonoBehaviour
{
    public Vector2 MoveDir
    {
        get
        {
            return moveDir;
        }
        private set
        {
            moveDir = value;
        }
    }

    public Vector2 moveDir;
    private Rigidbody2D playerRb;
    private AnimationController animationController;
    private Coroutine idleCor;
    public float idleDelay;
    public bool canMove = true;
    public float speed = 3;

    public void Awake()
    {
        playerRb = GetComponent<Rigidbody2D>();
        animationController = GetComponent<AnimationController>();
    }
    
    public void Move(InputAction.CallbackContext context)
    {
        MoveDir = context.ReadValue<Vector2>();

        if (canMove && context.performed)
        {
            playerRb.linearVelocity = MoveDir * speed;
            animationController.Move(true);
            
            if (MoveDir != Vector2.zero)
                transform.localScale = new Vector3(
                    Mathf.Sign(MoveDir.x) * Mathf.Abs(transform.localScale.x),
                    transform.localScale.y,
                    transform.localScale.z
                );
        }
        
        if (!canMove)
        {
            Debug.Log("canMove");
            animationController.Move(true);
            playerRb.linearVelocity = Vector2.zero;
        }
        
        if(context.canceled)
        {
            Debug.Log("cancel");
            SetIdleDelayed();
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    public void StopMove(int canIMove)
    {
        if (canIMove >= 1)
        {
            canMove = true;
            if (MoveDir != Vector2.zero)
            {
                playerRb.linearVelocity = MoveDir * speed;
                animationController.Move(true);

                if (MoveDir != Vector2.zero)
                    transform.localScale = new Vector3(
                        Mathf.Sign(MoveDir.x) * Mathf.Abs(transform.localScale.x),
                        transform.localScale.y,
                        transform.localScale.z
                    );            
            }
        }
        else
        {
            playerRb.linearVelocity = Vector2.zero;
            StartCoroutine(IdleDelayCor());
            canMove = false;
        }
    }
    
    private void SetIdleDelayed()
    {
        if (idleCor == null)
            idleCor = StartCoroutine(IdleDelayCor());
    }

    private IEnumerator IdleDelayCor()
    {
        yield return new WaitForSeconds(idleDelay);
        if (MoveDir == Vector2.zero)
        {
            animationController.Move(false);
        }
        idleCor = null;
    }
}
