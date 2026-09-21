using System.Collections;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animator animator;

    public bool canSkillUse1 = true;
    public bool canSkillUse2 = true;
    public bool canSkillUse3 = true;
    public bool canSkillUse4 = true;

    public void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Skill1(int skillId)
    {
        if (!canSkillUse1)
            return;

        SetSkillAnimationSpeed(skillId);

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

    private void SetSkillAnimationSpeed(int skillId)
    {
        float speed = UpgradeManager.Instance.GetSkillAnimationSpeed(skillId);

        animator.SetFloat("SkillSpeed", speed);
    }

    private IEnumerator SkillCooldown1(int skillId)
    {
        float coolTime =
            DataManager.Instance.skillDatasDic[skillId].skillCoolTime;

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
}