using System.Collections;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private static readonly int DashState = Animator.StringToHash("Base Layer.Dash");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
    private static readonly int RunState = Animator.StringToHash("Base Layer.Run");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");

    private Animator animator;

    public bool canSkillUse1 = true;
    public bool canSkillUse2 = true;
    public bool canSkillUse3 = true;
    public bool canSkillUse4 = true;

    public bool IsBasicAttacking =>
        animator.GetCurrentAnimatorStateInfo(0).fullPathHash == AttackState ||
        (animator.IsInTransition(0) &&
         animator.GetNextAnimatorStateInfo(0).fullPathHash == AttackState);

    public void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        // Disabling a prepared scene stops cooldown coroutines before they finish.
        StopAllCoroutines();
        canSkillUse1 = canSkillUse2 = canSkillUse3 = canSkillUse4 = true;
    }

    public void Skill1(int skillId)
    {
        if (!canSkillUse1)
            return;

        SetSkillAnimationSpeed(skillId, true);

        animator.SetTrigger("Skill1");
        canSkillUse1 = false;

        StartCoroutine(SkillCooldown1(skillId));
    }

    public void Skill2(int skillId)
    {
        if (!canSkillUse2)
            return;

        SetSkillAnimationSpeed(skillId);

        animator.SetTrigger("Skill2");
        canSkillUse2 = false;

        StartCoroutine(SkillCooldown2(skillId));
    }

    public void Skill3(int skillId)
    {
        if (!canSkillUse3)
            return;

        SetSkillAnimationSpeed(skillId);

        animator.SetTrigger("Skill3");
        canSkillUse3 = false;

        StartCoroutine(SkillCooldown3(skillId));
    }

    public void Skill4(int skillId)
    {
        if (!canSkillUse4)
            return;

        SetSkillAnimationSpeed(skillId);

        animator.SetTrigger("Skill4");
        canSkillUse4 = false;

        StartCoroutine(SkillCooldown4(skillId));
    }

    private void SetSkillAnimationSpeed(int skillId, bool basicAttack = false)
    {
        float speed = basicAttack
            ? DataManager.Instance.skillDatasDic[skillId].basicAttackAnimationSpeed
            : UpgradeManager.Instance.GetSkillAnimationSpeed(skillId);
        animator.SetFloat("SkillSpeed", speed);
    }

    private IEnumerator SkillCooldown1(int skillId)
    {
        float coolTime =
            DataManager.Instance.skillDatasDic[skillId].basicAttackCooldown;

        yield return new WaitForSeconds(coolTime);

        canSkillUse1 = true;
    }

    private IEnumerator SkillCooldown2(int skillId)
    {
        float coolTime =
            DataManager.Instance.skillDatasDic[skillId].skillCoolTime;

        yield return new WaitForSeconds(coolTime);

        canSkillUse2 = true;
    }

    private IEnumerator SkillCooldown3(int skillId)
    {
        float coolTime =
            DataManager.Instance.skillDatasDic[skillId].skillCoolTime;

        yield return new WaitForSeconds(coolTime);

        canSkillUse3 = true;
    }

    private IEnumerator SkillCooldown4(int skillId)
    {
        float coolTime =
            DataManager.Instance.skillDatasDic[skillId].skillCoolTime;

        yield return new WaitForSeconds(coolTime);

        canSkillUse4 = true;
    }

    public void Move(bool isMove)
    {
        animator.SetBool("Move", isMove);
    }

    public void StartDashAnimation()
    {
        animator.ResetTrigger("Skill1");
        animator.Play(DashState, 0, 0f);
    }

    public void EndDashAnimation(bool isMoving)
    {
        animator.SetBool("Move", isMoving);

        // An attack can take over the dash before its movement timer ends.
        // Only replace the dash state itself, never an active transition or attack.
        if (!animator.IsInTransition(0) &&
            animator.GetCurrentAnimatorStateInfo(0).fullPathHash == DashState)
        {
            animator.Play(isMoving ? RunState : IdleState, 0, 0f);
        }
    }
}
