using UnityEngine;

public class DragRotateObject : MonoBehaviour
{
    [Header("Rotation Settings")]
    private float rotationSpeed = 0.2f;
    public bool invertX = false;
    private bool invertY = true;

    [Header("Idle Reset")]
    public float idleResetTime = 2f;
    public float resetSpeed = 4f;

    private float idleTimer = 0f;
    private bool isResetting = false;

    private bool dragging = false;
    private Vector3 lastMousePos;

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        HandleDragging();
        HandleIdleReset();
    }

    void HandleDragging()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            isResetting = false;
            idleTimer = 0f;
            lastMousePos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
            dragging = false;

        if (dragging)
        {
            idleTimer = 0f;

            Vector3 delta = Input.mousePosition - lastMousePos;

            float rotX = delta.y * rotationSpeed * (invertY ? -1 : 1);
            float rotY = -delta.x * rotationSpeed * (invertX ? -1 : 1);

            transform.localRotation *= Quaternion.Euler(rotX, rotY, 0f);

            lastMousePos = Input.mousePosition;
        }
    }

    void HandleIdleReset()
    {
        if (dragging) return;

        idleTimer += Time.deltaTime;

        if (idleTimer >= idleResetTime)
            isResetting = true;

        if (isResetting)
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                initialRotation,
                Time.deltaTime * resetSpeed
            );

            if (Quaternion.Angle(transform.localRotation, initialRotation) < 0.5f)
            {
                isResetting = false;
                idleTimer = 0f;
            }
        }
    }
}
