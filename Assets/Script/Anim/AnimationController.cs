using System;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    Animator animator;
    public bool canSkillUse1 = true;
    public bool canSkillUse2 = true;
    public bool canSkillUse3 = true;
    public bool canSkillUse4 = true;

    public void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Skill1()
    {
        animator.SetTrigger("Skill1");
        canSkillUse1 = false;
    }
    
    public void Skill2()
    {
        animator.SetTrigger("Skill2");
        canSkillUse2 = false;
    }
    
    public void Skill3()
    {
        animator.SetTrigger("Skill3");
        canSkillUse3 = false;
    }
    
    public void Skill4()
    {
        animator.SetTrigger("Skill4");
        canSkillUse4 = false;
    }

    public void Move(bool isMove)
    {
        if (canSkillUse1 && canSkillUse2 && canSkillUse3 && canSkillUse4)
        {
            animator.SetBool("Move", isMove); 
        }
    }
    
    public void CanUseSkill1()
    {
        canSkillUse1 = true;
    }
    
    public void CanUseSkill2()
    {
        canSkillUse2 = true;
    }
    
    public void CanUseSkill3()
    {
        canSkillUse3 = true;
    }
    
    public void CanUseSkill4()
    {
        canSkillUse4 = true;
    }
}
