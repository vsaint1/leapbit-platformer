using UnityEngine;

public class GameManager : MonoBehaviour {

    public static GameManager Instance { get; private set; }

    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private Transform respawnPoint;

    private Player player;

    private int score = 0;

    void Awake() {
        Instance = this;
    }

    void Start() {
    }

    void Update() {

    }

    public void AddScore(int amount = 1) {
        score += amount;
        Debug.Log("Score: " + score);
    }

    public void RespawnPlayer() {
        GameObject playerObject = Instantiate(playerPrefab, respawnPoint.position, Quaternion.identity);
        player = playerObject.GetComponent<Player>();

    }

    public Player GetPlayer() {
        return player;
    }
}
