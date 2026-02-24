using UnityEngine;

public class TriggerConfirmMenu : MonoBehaviour
{
    [SerializeField] private ConfirmMenu confirmMenu;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string menuMessage = "Начать игру?";
    [SerializeField] private bool showOnce = true;
    [SerializeField] private float triggerDelay = 0f;

    private bool hasBeenShown = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (showOnce && hasBeenShown) return;

            if (triggerDelay > 0)
                Invoke(nameof(ShowMenu), triggerDelay);
            else
                ShowMenu();

            hasBeenShown = true;
        }
    }
    private void ShowMenu()
    {
        if (confirmMenu != null)
        {
            confirmMenu.ShowMenu(menuMessage);
        }
    }
}