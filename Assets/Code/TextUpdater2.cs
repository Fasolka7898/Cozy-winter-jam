using TMPro;
using UnityEngine;

public class TextUpdater2 : MonoBehaviour
{
    public TextMeshProUGUI snowmenRemainingText;
    private PlayerMovement.PlayerMovement playerMovement; // С пространством имен

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (playerMovement == null)
        {
            FindPlayer();
            return;
        }

        UpdateSnowmenRemaining();
    }

    private void UpdateSnowmenRemaining()
    {
        if (snowmenRemainingText == null) return;

        int total = playerMovement.TotalScore;
        int current = playerMovement.CurrentScore;
        int remaining = total - current;

        // Добавляем отладку
        Debug.Log($"Total: {total}, Current: {current}, Remaining: {remaining}");

        if (remaining > 0)
        {
            snowmenRemainingText.text = $"There are snowmen left: {remaining}";
        }
        else if (remaining == 0 && total > 0)
        {
            snowmenRemainingText.text = "All snowmen destroyed!";
        }
        else
        {
            snowmenRemainingText.text = "Snowmen: 0";
        }
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement.PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("PlayerMovement component not found on Player!");
            }
            else
            {
                Debug.Log("Player found successfully!");
                // Сразу обновляем текст
                UpdateSnowmenRemaining();
            }
        }
        else
        {
            Debug.LogError("Player not found with tag 'Player'!");
        }
    }
}