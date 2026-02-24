using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Получен урон {damage}. Осталось здоровья: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"Восстановлено {amount} здоровья. Текущее здоровье: {currentHealth}");
    }

    private void Die()
    {
        Debug.Log("Игрок погиб от холода!");
        // Здесь можно добавить логику смерти
    }

    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}