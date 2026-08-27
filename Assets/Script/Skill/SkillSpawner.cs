using System;
using System.Collections;
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
    
    PlayerMove playerMove;

    public void Awake()
    {
        skillActions.Add("/Mouse/leftButton", skillId1);
        skillActions.Add("/Keyboard/1", skillId2);
        skillActions.Add("/Keyboard/2", skillId3);
        skillActions.Add("/Keyboard/3", skillId4);
        
        animationController = GetComponent<AnimationController>();
        playerMove = GetComponent<PlayerMove>();
    }

    public void SkillUse(int skillId)
    {
            if (animationController.canSkillUse1 && skillId == skillId1)
            {
                animationController.Skill1();
            }
            else if (animationController.canSkillUse2 && skillId == skillId2)
            {
                animationController.Skill2();
            }
            else if (animationController.canSkillUse3 && skillId == skillId3)
            {
                animationController.Skill3();
            }
            else if(animationController.canSkillUse4 && skillId == skillId4)
            {
                animationController.Skill4();
            }
    }
    
    
    public void PlayerInput(InputAction.CallbackContext context)
    {
        if (context.performed &&  skillActions[context.control.path] > 0)
        {
            SkillUse(skillActions[context.control.path]);
        }
    }

    public IEnumerator LifeTime(int skillId, GameObject obj)
    {
        yield return new WaitForSeconds(DataManager.Instance.skillDatasDic[skillId].lifetime);
        
        obj.GetComponent<PooledObject>().DestroyObj();
    }

    public void MovementLock(int canMove)
    {
        if(canMove > 0)
            playerMove.StopMove(1);
        else
            playerMove.StopMove(0);
    }

    public void SkillSpawn(int skillId)
    {
        float offset = 1f;

        Vector3 spawnPos = transform.position;
        spawnPos.x += transform.localScale.x > 0 ? offset : -offset;
        
        GameObject skillPrefab = PoolManager.Instance.Spawn(skillId, spawnPos);
        
        ParticleSystemRenderer[] particleRenderers =
            skillPrefab.GetComponentsInChildren<ParticleSystemRenderer>();

        foreach (var particleRenderer in particleRenderers)
        {
            particleRenderer.flip = new Vector3(
                playerMove.transform.localScale.x < 0 ? 1 : 0,
                particleRenderer.flip.y,
                particleRenderer.flip.z
            );
        }
        
        StartCoroutine(LifeTime(skillId, skillPrefab));
    }
}
