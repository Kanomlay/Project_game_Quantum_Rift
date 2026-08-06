using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    void Start()
    {
        if (GameManager.selectedCharacter != null && GameManager.selectedCharacter.characterPrefab != null)
        {
            GameObject player = Instantiate(GameManager.selectedCharacter.characterPrefab, transform.position, Quaternion.identity);
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();

            if (camFollow != null)
            {
                camFollow.target = player.transform;
            }
        }
    }
}