using UnityEngine;

public class DragRotateObject : MonoBehaviour
{
    [Header("Rotation Settings")]
    private float rotationSpeed = 0.2f;
    public bool invertX = false;
    private bool invertY = true;

    [Header("Idle Reset")]
    public float idleResetTime = 2f;      // seconds of idleness
    public float resetSpeed = 4f;         // smoothing speed for reset

    private float idleTimer = 0f;
    private bool isResetting = false;

    private bool dragging = false;
    private Vector3 lastMousePos;

    // Mesh center (world space)
    private Vector3 center;

    // Store initial transform values
    private Quaternion initialRotation;
    private Vector3 initialPosition;

    void Start()
    {
        // Get mesh center
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        center = (mr != null) ? mr.bounds.center : transform.position;

        // Store starting values
        initialRotation = Quaternion.identity;
        initialPosition = Vector3.zero;
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

            MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null)
                center = mr.bounds.center;
        }

        if (Input.GetMouseButtonUp(0))
            dragging = false;

        if (dragging)
        {
            idleTimer = 0f; // reset idle timer

            Vector3 delta = Input.mousePosition - lastMousePos;

            float rotX = delta.y * rotationSpeed * (invertY ? -1 : 1);
            float rotY = -delta.x * rotationSpeed * (invertX ? -1 : 1);

            // Rotate around center
            transform.RotateAround(center, transform.right, rotX);
            transform.RotateAround(center, Vector3.up, rotY);

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
            // Smooth rotation reset
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                initialRotation,
                Time.deltaTime * resetSpeed
            );

            // Smooth position reset
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                initialPosition,
                Time.deltaTime * resetSpeed
            );

            // Stop when close enough
            if (Quaternion.Angle(transform.rotation, initialRotation) < 0.5f &&
                (transform.localPosition - initialPosition).sqrMagnitude < 0.0005f)
            {
                isResetting = false;
                idleTimer = 0f;
            }
        }
    }
}
