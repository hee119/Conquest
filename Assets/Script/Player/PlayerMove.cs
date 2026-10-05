using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMove : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCost = 25f;
    [SerializeField] private float dashRecoveryPerSecond = 20f;
    [SerializeField] private float dashRecoveryDelay = 0.5f;

    public float DashEnergyNormalized => dashEnergy / 100f;
    public bool IsDashing => isDashing;

    private float dashEnergy = 100f;
    private float dashTimeLeft;
    private float recoveryDelayLeft;
    private Vector2 dashDirection;
    private bool isDashing;

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

    private void OnEnable()
    {
        dashEnergy = 100f;
        dashTimeLeft = 0f;
        recoveryDelayLeft = 0f;
        isDashing = false;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            TryDash();

        if (recoveryDelayLeft > 0f)
        {
            recoveryDelayLeft -= Time.deltaTime;
        }
        else if (dashEnergy < 100f)
        {
            dashEnergy = Mathf.MoveTowards(dashEnergy, 100f, dashRecoveryPerSecond * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (!isDashing)
            return;

        playerRb.linearVelocity = dashDirection * dashSpeed;
        dashTimeLeft -= Time.fixedDeltaTime;

        if (dashTimeLeft <= 0f)
        {
            isDashing = false;
            playerRb.linearVelocity = canMove ? MoveDir * speed : Vector2.zero;
            animationController.EndDashAnimation(canMove && MoveDir != Vector2.zero);
        }
    }

    private void TryDash()
    {
        bool interruptingAttack = animationController.IsBasicAttacking;
        if (isDashing || dashEnergy < dashCost || (!canMove && !interruptingAttack))
            return;

        if (interruptingAttack)
            canMove = true;

        dashDirection = MoveDir.sqrMagnitude > 0f
            ? MoveDir.normalized
            : (transform.localScale.x < 0f ? Vector2.left : Vector2.right);

        dashEnergy -= dashCost;
        recoveryDelayLeft = dashRecoveryDelay;
        dashTimeLeft = dashDuration;
        isDashing = true;
        playerRb.linearVelocity = dashDirection * dashSpeed;
        animationController.StartDashAnimation();
    }
    
    public void Move(InputAction.CallbackContext context)
    {
        MoveDir = context.ReadValue<Vector2>();

        if (isDashing)
            return;

        if (canMove && context.performed)
        {
            playerRb.linearVelocity = MoveDir * speed;
            animationController.Move(true);
            
            if (MoveDir.x != 0f)
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
        if (canIMove < 1)
        {
            isDashing = false;
        }

        if (canIMove >= 1)
        {
            canMove = true;
            if (isDashing)
                return;
            if (MoveDir != Vector2.zero)
            {
                playerRb.linearVelocity = MoveDir * speed;
                animationController.Move(true);

                if (MoveDir.x != 0f)
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
