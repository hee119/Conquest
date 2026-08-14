using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GetStageData))]
public class GetStageDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        GetStageData data = (GetStageData)target;

        if (GUILayout.Button("스테이지 데이터 다운로드"))
        {
            data.DownloadStageData();
        }
    }
}