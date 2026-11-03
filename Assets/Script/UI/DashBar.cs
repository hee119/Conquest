using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Slider))]
public class DashBar : MonoBehaviour
{
    [SerializeField] private PlayerMove playerMove;
    private UnityEngine.UI.Slider slider;
    private PlayerMove subscribedPlayer;

    private void Awake()
    {
        slider = GetComponent<UnityEngine.UI.Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
    }

    private void OnEnable()
    {
        PlayerMove.BecameAvailable += OnPlayerAvailable;
        BindPlayer();
    }

    private void OnDisable()
    {
        PlayerMove.BecameAvailable -= OnPlayerAvailable;
        if (subscribedPlayer != null)
            subscribedPlayer.DashEnergyChanged -= SetEnergy;
        subscribedPlayer = null;
    }

    private void OnPlayerAvailable(PlayerMove player)
    {
        if (playerMove != null && playerMove != player)
            return;
        playerMove = player;
        BindPlayer();
    }

    private void BindPlayer()
    {
        if (subscribedPlayer != null)
            subscribedPlayer.DashEnergyChanged -= SetEnergy;
        if (playerMove == null)
            playerMove = FindFirstObjectByType<PlayerMove>();
        subscribedPlayer = playerMove;
        if (subscribedPlayer == null)
            return;
        subscribedPlayer.DashEnergyChanged += SetEnergy;
        SetEnergy(subscribedPlayer.DashEnergyNormalized);
    }

    private void SetEnergy(float value) => slider.value = value;
}
