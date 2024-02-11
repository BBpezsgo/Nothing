using UnityEngine;

public class FracturedObjectScript : MonoBehaviour
{
    [SerializeField, ReadOnly] public float LifeTime;
    [SerializeField, ReadOnly] float PlaySoundCooldown;
    [SerializeField, ReadOnly, NonReorderable] public AudioClip[] AudioClips;
    [SerializeField, ReadOnly] AudioSource AudioSource;
    [SerializeField, ReadOnly] Rigidbody Rigidbody;

    void OnCollisionEnter(Collision collision)
    {
        if (AudioClips.Length == 0 ||
            PlaySoundCooldown > 0f ||
            LifeTime < .5f ||
            AudioSource == null ||
            AudioSource.isPlaying ||
            (
                Rigidbody != null &&
                collision.gameObject.TryGetComponent(out Rigidbody otherRigidbody) &&
                (Rigidbody.velocity - otherRigidbody.velocity).sqrMagnitude <= 1f
            ))
        { return; }

        AudioSource.pitch = Random.Range(.75f, 1.25f);
        AudioSource.volume = Random.Range(.5f, 1f);
        AudioSource.PlayOneShot(AudioClips[Random.Range(0, AudioClips.Length - 1)]);
    }

    void Start()
    {
        AudioSource = GetComponent<AudioSource>();
        Rigidbody = GetComponent<Rigidbody>();
        PlaySoundCooldown = .6f;
    }

    void Update()
    {
        LifeTime -= Time.deltaTime;
        PlaySoundCooldown -= Time.deltaTime;

        if (LifeTime >= 0f) return;
        if (LifeTime <= 2f) { Destroy(gameObject); return; }

        float lifeTime = (LifeTime + 2f) / 2f;
        transform.localScale = new Vector3(lifeTime, lifeTime, lifeTime);
    }
}
