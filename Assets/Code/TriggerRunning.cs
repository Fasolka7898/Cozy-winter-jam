using UnityEngine;

public class TriggerRunning : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("L_MG_1_E");
    }

}
