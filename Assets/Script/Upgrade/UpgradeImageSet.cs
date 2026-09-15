using UnityEngine;

public class UpgradeImageSet : MonoBehaviour
{
    public int id;
    public UpgradeCombineResult result;

    public UpgradeImageSet selected;

    public void UpgradeCombineStart()
    {
        if (selected == null)
        {
            return;
        }
        
        result.SetResult();
    }
}