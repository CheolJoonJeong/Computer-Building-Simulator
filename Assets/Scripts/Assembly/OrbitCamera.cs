using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;        
    public float rotateSpeed = 5f;
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 10f;

    private float distance;

    void Start()
    {
        distance = Vector3.Distance(transform.position, target.position);
    }

    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            float x = Input.GetAxis("Mouse X") * rotateSpeed;
            float y = Input.GetAxis("Mouse Y") * rotateSpeed;
            transform.RotateAround(target.position, Vector3.up, x);
            transform.RotateAround(target.position, transform.right, -y);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minZoom, maxZoom);
        transform.position = target.position - transform.forward * distance;
    }
}