using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
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

    [Header("Tilemap Bounds")]
    [SerializeField, Min(0f)] private float boundaryPadding = 0.02f;

    private SpriteRenderer playerSpriteRenderer;

    [Header("Footstep SFX")]
    [SerializeField, Min(0.05f)]
    [Tooltip("이동 중 발소리 재생 간격(초). 값을 줄이면 더 자주 재생됩니다.")]
    private float footstepInterval = 0.18f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 1f;
    private static readonly int[] FootstepSoundIds =
    {
        SoundIds.Footstep01, SoundIds.Footstep02, SoundIds.Footstep03,
        SoundIds.Footstep04, SoundIds.Footstep05, SoundIds.Footstep06,
        SoundIds.Footstep07, SoundIds.Footstep08, SoundIds.Footstep09,
        SoundIds.Footstep10, SoundIds.Footstep11
    };
    private static int previousFootstepIndex = -1;
    private float footstepTimeRemaining;
    private Vector2 lastFootstepPosition;

    public void Awake()
    {
        playerRb = GetComponent<Rigidbody2D>();
        animationController = GetComponent<AnimationController>();
        playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lastFootstepPosition = playerRb != null ? playerRb.position : (Vector2)transform.position;
    }

    private void OnEnable()
    {
        dashEnergy = 100f;
        dashTimeLeft = 0f;
        recoveryDelayLeft = 0f;
        isDashing = false;
        footstepTimeRemaining = 0f;
        lastFootstepPosition = playerRb != null ? playerRb.position : (Vector2)transform.position;
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
        if (isDashing)
        {
            playerRb.linearVelocity = dashDirection * dashSpeed;
            dashTimeLeft -= Time.fixedDeltaTime;

            if (dashTimeLeft <= 0f)
            {
                isDashing = false;
                playerRb.linearVelocity = canMove ? MoveDir * speed : Vector2.zero;
                animationController.EndDashAnimation(canMove && MoveDir != Vector2.zero);
            }
        }

        KeepInsideTilemapBounds();
        PlayFootstepForMovement();
    }

    private void PlayFootstepForMovement()
    {
        if (playerRb == null)
            return;

        Vector2 currentPosition = playerRb.position;
        bool moved = (currentPosition - lastFootstepPosition).sqrMagnitude > 0.000001f;
        lastFootstepPosition = currentPosition;

        if (!canMove || isDashing || Time.timeScale <= 0f || !moved)
        {
            footstepTimeRemaining = 0f;
            return;
        }

        footstepTimeRemaining -= Time.fixedDeltaTime;
        if (footstepTimeRemaining > 0f)
            return;

        footstepTimeRemaining = Mathf.Max(0f,
            footstepTimeRemaining + Mathf.Max(0.05f, footstepInterval));
        if (SoundManager.Instance == null) return;
        SoundManager.Instance.Play(GetRandomFootstepId(), footstepVolume);
    }

    private static int GetRandomFootstepId()
    {
        int index = Random.Range(0,
            FootstepSoundIds.Length - (previousFootstepIndex >= 0 ? 1 : 0));
        if (previousFootstepIndex >= 0 && index >= previousFootstepIndex)
            index++;
        previousFootstepIndex = index;
        return FootstepSoundIds[index];
    }

    private void KeepInsideTilemapBounds()
    {
        if (playerRb == null || StageManager.Instance == null)
            return;

        Tilemap tilemap = StageManager.Instance.backgroundTilemap;
        if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
            return;

        Bounds localBounds = tilemap.localBounds;
        if (localBounds.size.x <= 0f || localBounds.size.y <= 0f)
            return;

        Vector3 localMin = localBounds.min;
        Vector3 localMax = localBounds.max;
        Vector3 worldBottomLeft = tilemap.transform.TransformPoint(
            new Vector3(localMin.x, localMin.y, 0f));
        Vector3 worldTopRight = tilemap.transform.TransformPoint(
            new Vector3(localMax.x, localMax.y, 0f));

        float mapMinX = Mathf.Min(worldBottomLeft.x, worldTopRight.x);
        float mapMaxX = Mathf.Max(worldBottomLeft.x, worldTopRight.x);
        float mapMinY = Mathf.Min(worldBottomLeft.y, worldTopRight.y);
        float mapMaxY = Mathf.Max(worldBottomLeft.y, worldTopRight.y);

        Vector2 spriteExtents = playerSpriteRenderer != null
            ? (Vector2)playerSpriteRenderer.bounds.extents
            : Vector2.zero;
        Vector2 spriteCenterOffset = playerSpriteRenderer != null
            ? (Vector2)(playerSpriteRenderer.bounds.center - transform.position)
            : Vector2.zero;

        float padding = Mathf.Max(0f, boundaryPadding);
        float minX = mapMinX + spriteExtents.x + padding - spriteCenterOffset.x;
        float maxX = mapMaxX - spriteExtents.x - padding - spriteCenterOffset.x;
        float minY = mapMinY + spriteExtents.y + padding - spriteCenterOffset.y;
        float maxY = mapMaxY - spriteExtents.y - padding - spriteCenterOffset.y;

        if (minX > maxX)
            minX = maxX = (mapMinX + mapMaxX) * 0.5f - spriteCenterOffset.x;
        if (minY > maxY)
            minY = maxY = (mapMinY + mapMaxY) * 0.5f - spriteCenterOffset.y;

        Vector2 currentPosition = playerRb.position;
        Vector2 nextPosition = currentPosition + playerRb.linearVelocity * Time.fixedDeltaTime;
        nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
        nextPosition.y = Mathf.Clamp(nextPosition.y, minY, maxY);

        playerRb.linearVelocity = (nextPosition - currentPosition) / Time.fixedDeltaTime;
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
        SoundManager.Instance?.Play(SoundIds.Dash);
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
