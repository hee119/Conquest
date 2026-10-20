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
                animationController.Skill1(skillId);
            }
            else if (animationController.canSkillUse2 && skillId == skillId2)
            {
                animationController.Skill2(skillId);
            }
            else if (animationController.canSkillUse3 && skillId == skillId3)
            {
                animationController.Skill3(skillId);
            }
            else if(animationController.canSkillUse4 && skillId == skillId4)
            {
                animationController.Skill4(skillId);
            }
    }
    
    
    public void PlayerInput(InputAction.CallbackContext context)
    {
        // The mage uses a separate aimed basic attack instead of the sword skill.
        if (CharacterManager.Instance != null && CharacterManager.Instance.currentCharacter == 2 &&
            context.control.path == "/Mouse/leftButton")
            return;

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
        bool isBasicAttack = skillId == skillId1;
        int characterId = CharacterManager.Instance != null
            ? CharacterManager.Instance.currentCharacter
            : 0;
        BasicAttackModifiers modifiers = isBasicAttack && UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetBasicAttackModifiers(characterId)
            : BasicAttackModifiers.Default;

        int attackCount = isBasicAttack ? modifiers.attackCount : 1;
        float verticalSpacing = 0.35f * modifiers.rangeMultiplier;

        for (int i = 0; i < attackCount; i++)
        {
            Vector3 spawnPos = transform.position;
            spawnPos.x += transform.localScale.x > 0 ? offset : -offset;
            spawnPos.y += (i - (attackCount - 1) * 0.5f) * verticalSpacing;

            GameObject skillPrefab = PoolManager.Instance.Spawn(skillId, spawnPos);
            if (skillPrefab == null)
                continue;

            if (isBasicAttack)
            {
                BasicAttackInstanceState state =
                    skillPrefab.GetComponent<BasicAttackInstanceState>();
                if (state == null)
                    state = skillPrefab.AddComponent<BasicAttackInstanceState>();
                state.Apply(modifiers);
            }

            ParticleSystemRenderer[] particleRenderers =
                skillPrefab.GetComponentsInChildren<ParticleSystemRenderer>();

            foreach (ParticleSystemRenderer particleRenderer in particleRenderers)
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
}
