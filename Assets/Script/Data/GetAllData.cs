using UnityEngine;

public class GetAllData : MonoBehaviour
{
    [ContextMenu("Data Download")]
    public void Download()
    {
        foreach (GetData child in transform.GetComponentsInChildren<GetData>())
        {
            child.DownloadStageData();
        }
    }
    
}
