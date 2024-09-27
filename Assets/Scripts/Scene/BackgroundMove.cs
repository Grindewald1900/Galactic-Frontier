using UnityEngine;

public class BackgroundMove : MonoBehaviour
{
    private float length, startPos;
    public GameObject mainCamera;
    public float moveEffect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        // float temp = (mainCamera.transform.position.x * (1 - moveEffect));
        float dist = (mainCamera.transform.position.x * moveEffect);
        transform.position = new Vector3(startPos + dist, transform.position.y, transform.position.z);
    }
}
