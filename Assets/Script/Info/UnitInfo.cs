using System.Collections;
using UnityEngine;

public class UnitInfo : MonoBehaviour
{
    public int id;
    public string name;
    public int hp;
    public int hpRegenRate;
    public float speed;
    public int damage;
    public int defense;
    public float criticalDamage;
    public int criticalChance;

    public void OnEnable()
    {
        StartCoroutine(GetUnitData());
    }

    private IEnumerator GetUnitData()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);

        if (DataManager.Instance.playerAndEnemyInfos.TryGetValue(id, out PlayerAndEnemyInfos data))
        {
            name = data.name;
            hp = data.hp;
            hpRegenRate = data.hpRegenRate;
            speed = data.speed;
            damage = data.damage;
            defense = data.defense;
            criticalDamage = data.criticalDamage;
            criticalChance = data.criticalChance;
        }
    }
}
