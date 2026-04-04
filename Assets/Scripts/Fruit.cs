using UnityEngine;

public class Fruit : MonoBehaviour {

    [SerializeField]
    private GameObject collectedEffectPrefab;

    void Start() {

    }

    void Update() {

    }

    void OnTriggerEnter2D(Collider2D collision) {
        if (collision.TryGetComponent<Player>(out Player player)) {
            GameManager.Instance.AddScore();

            Destroy(gameObject);
            if (collectedEffectPrefab != null) {
                GameObject effect = Instantiate(collectedEffectPrefab, transform.position, Quaternion.identity);

                Destroy(effect, 1f);
            }

        }
    }
}
