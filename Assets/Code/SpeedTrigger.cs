using UnityEngine;
using UnityEngine.Rendering;

public class SpeedTrigger : MonoBehaviour
{
    public float enterSpeed;
    public float exitSpeed;
    private void OnTriggerEnter(Collider other)
    {
        other.GetComponent<PlayerMovementRunning>().speed = enterSpeed;
    }

    private void OnTriggerExit(Collider other)
    {
        other.GetComponent<PlayerMovementRunning>().speed = exitSpeed;
    }
}


