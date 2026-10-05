using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Slider))]
public class DashBar : MonoBehaviour
{
    [SerializeField] private PlayerMove playerMove;
    private UnityEngine.UI.Slider slider;

    private void Awake()
    {
        slider = GetComponent<UnityEngine.UI.Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
    }

    private void LateUpdate()
    {
        if (playerMove == null)
            playerMove = FindFirstObjectByType<PlayerMove>();

        if (playerMove != null)
            slider.value = playerMove.DashEnergyNormalized;
    }
}
