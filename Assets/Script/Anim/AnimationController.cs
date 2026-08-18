using System;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    Animator animator;
    public bool canSkillUse = true;

    public void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Skill1()
    {
        animator.SetTrigger("Skill1");
        canSkillUse = false;
    }
    
    public void Skill2()
    {
        animator.SetTrigger("Skill2");
        canSkillUse = false;
    }
    
    public void Skill3()
    {
        animator.SetTrigger("Skill3");
        canSkillUse = false;
    }
    
    public void Skill4()
    {
        animator.SetTrigger("Skill4");
        canSkillUse = false;
    }

    public void Move(bool isMove)
    {
        animator.SetBool("Move", isMove);
    }
    
    public void CanUseSkill()
    {
        canSkillUse = true;
    }
}
