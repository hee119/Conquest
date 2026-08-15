using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SkillSpawner : MonoBehaviour
{
    [Header("Skill Id")]
    public int skillId1;
    public int skillId2;
    public int skillId3;
    public int skillId4;

    Dictionary<string, int> skillActions = new Dictionary<string, int>();

    public void Awake()
    {
        skillActions.Add("/Mouse/leftButton", skillId1);
        skillActions.Add("/Keyboard/1", skillId2);
        skillActions.Add("/Keyboard/2", skillId3);
        skillActions.Add("/Keyboard/3", skillId4);
    }

    public void SkillUse(int skillId)
    {
        PoolManager.Instance.Spawn(skillId, transform.position);
    }
    
    
    public void PlayerInput(InputAction.CallbackContext context)
    {
        if (context.performed &&  skillActions[context.control.path] > 0)
        {
            SkillUse(skillActions[context.control.path]);
        }
    }
}
