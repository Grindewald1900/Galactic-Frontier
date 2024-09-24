using UnityEngine;

public class BackgroundMove : MonoBehaviour
{
    private float length, startPos;
    public GameObject camera;
    public float moveEffect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        float temp = (camera.transform.position.x * (1 - moveEffect));
        float dist = (camera.transform.position.x * moveEffect);
        transform.position = new Vector3(startPos + dist, transform.position.y, transform.position.z);
    }
}
