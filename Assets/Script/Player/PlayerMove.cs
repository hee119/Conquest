using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
public class PlayerMove : MonoBehaviour
{
    public static event System.Action<PlayerMove> BecameAvailable;
    public event System.Action<float> DashEnergyChanged;
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCost = 25f;
    [SerializeField] private float dashRecoveryPerSecond = 20f;
    [SerializeField] private float dashRecoveryDelay = 0.5f;

    public float DashEnergyNormalized => dashEnergy / 100f;
    public bool IsDashing => isDashing;

    private float dashEnergy = 100f;
    private Coroutine dashCoroutine;
    private Coroutine recoveryCoroutine;
    private Coroutine movementCoroutine;
    private static readonly WaitForFixedUpdate PhysicsStep = new WaitForFixedUpdate();
    private Vector2 dashDirection;
    private bool isDashing;
    private InputAction dashAction;
    private PlayerInput playerInput;

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
    private Bounds boundarySpriteBounds;

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
        playerInput = GetComponent<PlayerInput>();
        playerRb = GetComponent<Rigidbody2D>();
        animationController = GetComponent<AnimationController>();
        playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        // Attack frames include the weapon and can have very different bounds.
        // Keep the standing footprint instead of moving the boundary with each frame.
        if (playerSpriteRenderer != null)
            boundarySpriteBounds = playerSpriteRenderer.localBounds;
        lastFootstepPosition = playerRb != null ? playerRb.position : (Vector2)transform.position;
    }

    private void OnEnable()
    {
        canMove = true;
        dashEnergy = 100f;
        isDashing = false;
        footstepTimeRemaining = 0f;
        lastFootstepPosition = playerRb != null ? playerRb.position : (Vector2)transform.position;
        if (dashAction == null)
            dashAction = new InputAction("Dash", InputActionType.Button, "<Mouse>/rightButton");
        dashAction.performed += OnDashPerformed;
        dashAction.Enable();
        DashEnergyChanged?.Invoke(DashEnergyNormalized);
        BecameAvailable?.Invoke(this);
    }

    private void OnDisable()
    {
        if (dashAction != null)
        {
            dashAction.performed -= OnDashPerformed;
            dashAction.Disable();
        }
        if (idleCor != null) StopCoroutine(idleCor);
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        if (recoveryCoroutine != null) StopCoroutine(recoveryCoroutine);
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        idleCor = null;
        dashCoroutine = null;
        recoveryCoroutine = null;
        movementCoroutine = null;
        MoveDir = Vector2.zero;
        isDashing = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
    }

    private void OnDestroy() => dashAction?.Dispose();

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f ||
            playerRb == null || animationController == null) return;
        if (playerInput != null && (!playerInput.enabled || !playerInput.inputIsActive)) return;
        TryDash();
    }

    private IEnumerator RecoverDashEnergy()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, dashRecoveryDelay));
        while (dashEnergy < 100f)
        {
            float previousEnergy = dashEnergy;
            dashEnergy = Mathf.MoveTowards(dashEnergy, 100f, dashRecoveryPerSecond * Time.deltaTime);
            if (dashEnergy != previousEnergy)
                DashEnergyChanged?.Invoke(DashEnergyNormalized);
            yield return null;
        }
        recoveryCoroutine = null;
    }

    private IEnumerator Dash()
    {
        float remaining = Mathf.Max(Time.fixedDeltaTime, dashDuration);
        while (isDashing && remaining > 0f)
        {
            playerRb.linearVelocity = dashDirection * dashSpeed;
            KeepInsideTilemapBounds();
            yield return PhysicsStep;
            remaining -= Time.fixedDeltaTime;
        }
        isDashing = false;
        playerRb.linearVelocity = canMove ? MoveDir * speed : Vector2.zero;
        animationController.EndDashAnimation(canMove && MoveDir != Vector2.zero);
        dashCoroutine = null;
    }

    private void TrackMovement()
    {
        if (movementCoroutine == null && playerRb != null &&
            (isDashing || playerRb.linearVelocity.sqrMagnitude > 0f))
            movementCoroutine = StartCoroutine(MovementPhysics());
    }

    private IEnumerator MovementPhysics()
    {
        // Run the first clamp before the next physics simulation.
        KeepInsideTilemapBounds();
        do
        {
            yield return PhysicsStep;
            // Contacts can remove one velocity axis while the input stays held.
            // Restore the requested movement before the next physics step so
            // sliding past a tree resumes both axes without another key event.
            if (!isDashing && canMove && MoveDir.sqrMagnitude > 0f)
                playerRb.linearVelocity = MoveDir * speed;
            KeepInsideTilemapBounds();
            PlayFootstepForMovement();
        }
        while (isDashing || (canMove && MoveDir.sqrMagnitude > 0f) ||
               playerRb.linearVelocity.sqrMagnitude > 0f);
        movementCoroutine = null;
        footstepTimeRemaining = 0f;
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

        Vector3 worldExtents = playerSpriteRenderer != null
            ? playerSpriteRenderer.transform.TransformVector(boundarySpriteBounds.extents)
            : Vector3.zero;
        Vector2 spriteExtents = new Vector2(Mathf.Abs(worldExtents.x), Mathf.Abs(worldExtents.y));
        Vector2 spriteCenterOffset = playerSpriteRenderer != null
            ? (Vector2)(playerSpriteRenderer.transform.TransformPoint(boundarySpriteBounds.center) - transform.position)
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
        Vector2 correctedPosition = new Vector2(
            Mathf.Clamp(currentPosition.x, minX, maxX),
            Mathf.Clamp(currentPosition.y, minY, maxY));
        // Correct an out-of-bounds position directly; never turn that correction
        // into a velocity that persists after an attack or input has stopped.
        if (correctedPosition != currentPosition)
            playerRb.position = correctedPosition;
        Vector2 nextPosition = correctedPosition + playerRb.linearVelocity * Time.fixedDeltaTime;
        nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
        nextPosition.y = Mathf.Clamp(nextPosition.y, minY, maxY);

        playerRb.linearVelocity = (nextPosition - correctedPosition) / Time.fixedDeltaTime;
    }

    private void TryDash()
    {
        bool interruptingAttack = animationController.IsBasicAttacking;
        if (isDashing || dashEnergy < dashCost || (!canMove && !interruptingAttack))
            return;

        if (interruptingAttack)
        {
            GetComponent<SkillSpawner>()?.CancelBasicAttackMovementLock();
            canMove = true;
        }

        dashDirection = MoveDir.sqrMagnitude > 0f
            ? MoveDir.normalized
            : (transform.localScale.x < 0f ? Vector2.left : Vector2.right);

        dashEnergy -= dashCost;
        DashEnergyChanged?.Invoke(DashEnergyNormalized);
        isDashing = true;
        playerRb.linearVelocity = dashDirection * dashSpeed;
        animationController.StartDashAnimation();
        SoundManager.Instance?.Play(SoundIds.Dash);
        if (recoveryCoroutine != null) StopCoroutine(recoveryCoroutine);
        recoveryCoroutine = StartCoroutine(RecoverDashEnergy());
        dashCoroutine = StartCoroutine(Dash());
        TrackMovement();
    }
    
    public void Move(InputAction.CallbackContext context)
    {
        // Disabling PlayerInput cancels actions after the player becomes inactive.
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
            return;

        if (TryGetComponent<PlayerCombat>(out var combat) && combat.isDead)
            return;
        
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
        TrackMovement();
    }

    public void StopMove(int canIMove)
    {
        if (!isActiveAndEnabled)
        {
            canMove = canIMove >= 1;
            return;
        }
        if (canIMove < 1)
        {
            if (dashCoroutine != null) StopCoroutine(dashCoroutine);
            dashCoroutine = null;
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
            SetIdleDelayed();
            canMove = false;
        }
        TrackMovement();
    }
    
    private void SetIdleDelayed()
    {
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled || idleCor != null)
            return;

        if (TryGetComponent<PlayerCombat>(out var combat) && combat.isDead)
            return;

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
