using UnityEngine;
using UnityEngine.Serialization;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance = null;
    public int currentCharacterId = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            currentCharacterId = PlayerPrefs.GetInt("SelectedCharacter", 1) == 2 ? 2 : 1;
            DontDestroyOnLoad(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
}
