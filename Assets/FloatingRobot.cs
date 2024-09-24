using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    public float floatAmplitude = 0.5f;
    public float floatSpeed = 60f;
    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.position;
    }

    void Update()
    {
        float newY = initialPosition.y + Mathf.Sin(Time.time * Mathf.PI) * floatAmplitude;
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
    }
}
