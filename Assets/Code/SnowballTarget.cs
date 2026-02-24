using UnityEngine;
using System.Collections;

namespace PlayerMovement
{
    public class SnowballTarget : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private int currentHealth;

        [Header("Score Settings")]
        [SerializeField] private int scoreValue = 1;

        [Header("Visual Feedback")]
        [SerializeField] private MeshRenderer targetRenderer;
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private float hitFlashDuration = 0.1f;

        [Header("Death Effects")]
        [SerializeField] private GameObject deathEffect;
        [SerializeField] private AudioClip deathSound;
        [SerializeField][Range(0f, 1f)] private float deathSoundVolume = 0.7f;
        [SerializeField] private GameObject[] dropItems;

        private AudioSource audioSource;
        private Color originalColor;
        private bool isDead = false;
        private Collider targetCollider;

        private void Awake()
        {
            currentHealth = maxHealth;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.spatialBlend = 1f;

            targetCollider = GetComponent<Collider>();
            if (targetCollider == null)
            {
                targetCollider = gameObject.AddComponent<BoxCollider>();
                Debug.Log("Added BoxCollider to target");
            }

            if (targetRenderer != null && targetRenderer.material != null)
            {
                originalColor = targetRenderer.material.color;
            }
        }

        private void Start()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        public void TakeDamage(int damage, PlayerMovement player)
        {
            if (isDead) return;

            currentHealth -= damage;
            Debug.Log($"Target hit! Health: {currentHealth}/{maxHealth}");

            StartCoroutine(HitFlash());

            if (currentHealth <= 0)
            {
                Die(player);
            }
        }

        private IEnumerator HitFlash()
        {
            if (targetRenderer != null)
            {
                targetRenderer.material.color = hitColor;
                yield return new WaitForSeconds(hitFlashDuration);

                if (!isDead)
                {
                    targetRenderer.material.color = originalColor;
                }
            }
        }

        private void Die(PlayerMovement player)
        {
            if (isDead) return;
            isDead = true;

            if (player != null)
            {
                player.AddScore(scoreValue);
                Debug.Log($"Target destroyed! Added {scoreValue} points. Total killed: {player.GetCurrentScore()}");
            }

            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }

            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound, deathSoundVolume);
            }

            DropItems();
            Destroy(gameObject, 0.3f);
        }

        private void DropItems()
        {
            if (dropItems == null || dropItems.Length == 0) return;

            foreach (GameObject item in dropItems)
            {
                if (item != null)
                {
                    Vector3 randomOffset = new Vector3(
                        Random.Range(-0.5f, 0.5f),
                        0.2f,
                        Random.Range(-0.5f, 0.5f)
                    );

                    Instantiate(item, transform.position + randomOffset, Quaternion.identity);
                }
            }
        }

        public void TakeDamage(int damage)
        {
            TakeDamage(damage, null);
        }

        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = Color.yellow;

                if (col is BoxCollider box)
                {
                    Gizmos.DrawWireCube(transform.position + box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
                }
            }
        }
    }
}