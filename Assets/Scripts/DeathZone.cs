using UnityEngine;

public class DeathZone : MonoBehaviour {

    void Start() {
    }

    void Update() {

    }

    void OnTriggerEnter2D(Collider2D collision) {

        if (collision.TryGetComponent<Player>(out Player player)) {
            player.Kill();
        }

        GameManager.Instance.RespawnPlayer();
    }
}