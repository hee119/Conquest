using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance = null;
    public int currentCharacter = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            currentCharacter = PlayerPrefs.GetInt("SelectedCharacter", 1) == 2 ? 2 : 1;
            DontDestroyOnLoad(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
}
