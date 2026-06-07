using UnityEngine;

// 유니티 에디터 Scene 뷰처럼: 마우스 우클릭을 누른 상태로 시점 회전 + WASD(+QE)로 이동
// - W/S: 앞/뒤, A/D: 좌/우, Q/E: 아래/위
// - 우클릭 드래그: 시점 회전 (마우스 X/Y)
// - Shift: 이동 속도 증가, 휠: 이동 속도 자체 조절
public class FreeFlyCamera : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float fastMultiplier = 3f;
    [SerializeField] private float scrollSpeedStep = 1f;
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 30f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 3f;

    private float yaw;
    private float pitch;

    void Start()
    {
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    void Update()
    {
        // 휠로 이동 속도 자체를 조절 (우클릭 안 누르고 있을 때)
        if (!Input.GetMouseButton(1))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                moveSpeed = Mathf.Clamp(moveSpeed + scroll * scrollSpeedStep * 10f, minSpeed, maxSpeed);
        }

        // 우클릭 드래그 중에만 시점 회전 (에디터 Scene 뷰와 동일한 조작감)
        if (Input.GetMouseButton(1))
        {
            yaw   += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // WASD + QE 이동
        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) dir += transform.forward;
        if (Input.GetKey(KeyCode.S)) dir -= transform.forward;
        if (Input.GetKey(KeyCode.D)) dir += transform.right;
        if (Input.GetKey(KeyCode.A)) dir -= transform.right;
        if (Input.GetKey(KeyCode.E)) dir += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) dir -= Vector3.up;

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= fastMultiplier;

        transform.position += dir.normalized * speed * Time.deltaTime;
    }
}
