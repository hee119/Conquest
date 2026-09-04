using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour
{
    public PlayerCombat playerCombat;
    public Slider hpSlider;
    public Coroutine hpUpdateCor;
    private float targetHp;
    public float velocity;
    public void Awake()
    {
        hpSlider = GetComponent<Slider>();
        hpSlider.value = 1;
    }
    public void Start()
    {
        playerCombat.hpUpdate += HpUpdate;
    }
    
    
    public void HpUpdate()
    {
        targetHp = (float)playerCombat.hp / playerCombat.maxHp;

        if (hpUpdateCor == null)
        {
            velocity = 0;
            hpUpdateCor = StartCoroutine(HpUpdateCor());
        }
        
        Debug.Log(
            $"HP={playerCombat.hp}/{playerCombat.maxHp}, " +
            $"Target={targetHp}, Slider={hpSlider.value}"
        );
    }

    IEnumerator HpUpdateCor()
    {
        while (Mathf.Abs(hpSlider.value - targetHp) > 0.001f)
        {
            hpSlider.value = Mathf.SmoothDamp(
                hpSlider.value,
                targetHp,
                ref velocity,
                0.15f
            );
            yield return null;
        }

        hpUpdateCor = null;
    }
}
