using UnityEngine;

public class UpgradeInfo : MonoBehaviour
{
    public int id;

    private void Awake()
    {
        gameObject.tag = "Upgrade";
    }
}
