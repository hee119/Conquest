using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene roots are saved disabled so additive preparation cannot run gameplay.
[DefaultExecutionOrder(-10000)]
public sealed class PreparedScene : MonoBehaviour
{
    [SerializeField] private GameObject[] roots;
    [SerializeField] private bool[] initiallyActive;
    private readonly List<GameObject> children = new();
    private readonly List<bool> childActive = new();
    private Vector3[] positions;
    private Quaternion[] rotations;
    public Scene Scene => gameObject.scene;
    public bool HasBeenShown { get; private set; }

    private void Awake()
    {
        positions = new Vector3[roots.Length];
        rotations = new Quaternion[roots.Length];
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            positions[i] = roots[i].transform.position;
            rotations[i] = roots[i].transform.rotation;
            foreach (Transform child in roots[i].GetComponentsInChildren<Transform>(true))
            {
                if (child == roots[i].transform) continue;
                children.Add(child.gameObject);
                childActive.Add(child.gameObject.activeSelf);
            }
        }
        GameSceneManager.RegisterPreparedScene(this);
        if (GameSceneManager.IsPreparingScenes) Hide();
        else Show();
    }

    public void Hide()
    {
        foreach (var root in Scene.GetRootGameObjects())
            if (root != gameObject) root.SetActive(false);
    }

    public void Show()
    {
        // Restore authored UI visibility, including a dead player and end screen.
        for (int i = 0; i < children.Count; i++)
            if (children[i] != null && children[i].scene == Scene)
                children[i].SetActive(childActive[i]);
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null || roots[i].scene != Scene) continue;
            roots[i].transform.SetPositionAndRotation(positions[i], rotations[i]);
        }
        // UpgradeManager's Awake needs an already active player.
        for (int pass = 0; pass < 3; pass++)
        for (int i = 0; i < roots.Length; i++)
        {
            var root = roots[i];
            if (root == null || root.scene != Scene) continue;
            int priority = root.GetComponent<CharacterManager>() != null ? 0
                : root.GetComponent<PlayerInfo>() != null ? 1 : 2;
            if (priority == pass) root.SetActive(initiallyActive[i]);
        }
        if (HasBeenShown)
            foreach (var root in roots)
                if (root != null && root.scene == Scene)
                    foreach (var setup in root.GetComponentsInChildren<SelectedCharacterSetup>(true))
                        setup.ApplySelectedCharacter();
        HasBeenShown = true;
    }
}
