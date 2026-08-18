using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Diagnostics;

public class SkillSpawner : MonoBehaviour
{
    [Header("Skill Id")]
    public int skillId1;
    public int skillId2;
    public int skillId3;
    public int skillId4;

    Dictionary<string, int> skillActions = new Dictionary<string, int>();
    
    AnimationController animationController;

    public void Awake()
    {
        skillActions.Add("/Mouse/leftButton", skillId1);
        skillActions.Add("/Keyboard/1", skillId2);
        skillActions.Add("/Keyboard/2", skillId3);
        skillActions.Add("/Keyboard/3", skillId4);
        
        animationController = GetComponent<AnimationController>();
    }

    public void SkillUse(int skillId)
    {
        if (animationController.canSkillUse)
        {
            PoolManager.Instance.Spawn(skillId, transform.position);
            if (skillId == skillId1)
            {
                animationController.Skill1();
            }
            else if (skillId == skillId2)
            {
                animationController.Skill2();
            }
            else if (skillId == skillId3)
            {
                animationController.Skill3();
            }
            else
            {
                animationController.Skill4();
            }
        }
    }
    
    
    public void PlayerInput(InputAction.CallbackContext context)
    {
        if (context.performed &&  skillActions[context.control.path] > 0)
        {
            SkillUse(skillActions[context.control.path]);
        }
    }
}
