using UnityEngine;

public class SpecialEffect : MonoBehaviour
{
    [SerializeField] ParticleSystem ps;

    private void Awake()
    {

        if (ps == null)
        {
            ps = GetComponent<ParticleSystem>();
            if (ps == null)
                Debug.LogError("No ParticleSystem found on this GameObject.");
        }
    }

    public void PlayEffect()
    {
        // Detach from parent so it stays in world space
        transform.parent = null;

        // Play the particle effect
        ps.Play();

        // Destroy after the particle system's duration
        Destroy(gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }

    public void PlayEffectNoDestroy()
    {
        ps.Play();
    }
}
