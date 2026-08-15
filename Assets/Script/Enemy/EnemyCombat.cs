using Unity.VisualScripting;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public int maxHp = 5;
    public int hp;
    
    public void TakeDamage(int damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            Die();
        }
    }

    public void OnEnable()
    {
        hp = maxHp;
    }

    public void Die()
    {
        gameObject.SetActive(false);
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.gameObject.name);

        if (other.CompareTag("Skill"))
        {
            Debug.Log(other.gameObject.name);
            TakeDamage(other.gameObject.GetComponent<SkillInfo>().skillDamage);
        }
    }
}
