using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GachaController : MonoBehaviour
{
    [Header("Gacha Machine Config")]
    [SerializeField] private GachaMachineDatabase database;

    [Header("References")]
    public Transform knobObject;
    public Camera mainCamera;
    public ParticleSystem clickVfx;
    [SerializeField] private Animator gachaAnim;
    public GameObject spinUI;

    [Header("Rotation")]
    [Tooltip("Local axis to rotate around (e.g. Y for a front-facing dial axle).")]
    public Vector3 localAxis = Vector3.up;
    [Tooltip("Multiplier applied to the signed angle from mouse drag.")]
    public float rotationSpeed = 1f;
    [Tooltip("Require the drag to start over the knob's collider.")]
    public bool isAvaliable = false;

    [Header("Scoring / Limits")]
    [Tooltip("How many full clockwise turns before stopping and resetting.")]
    public int maxTurns = 3;

    [Tooltip("Current number of full clockwise turns completed.")]
    public int points = 0;

    [Tooltip("Invoked each time a full clockwise rotation (>=360°) is completed.")]
    public UnityEvent OnFullTurn;
    [Tooltip("Invoked when maxTurns is reached and the knob is reset.")]
    public UnityEvent OnMaxTurnsReached;

    [Header("Auto Spin (Idle)")]
    public bool enableAutoSpin = true;
    [Tooltip("Seconds of no input before the knob auto-spins 3 turns.")]
    public float idleTimeToAutoSpin = 5f;
    [Tooltip("Auto-spin speed in degrees per second.")]
    public float autoSpinSpeed = 360f;

    private bool isRotating;
    private bool isAutoSpinning;
    private float idleTimer = 0f;

    private Vector3 prevVecWorld;
    private Plane dragPlane;
    private Vector3 worldAxis;
    private float accumulatedDegreesCW = 0f;

    private Quaternion initialLocalRotation;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (knobObject == null) knobObject = transform;

        initialLocalRotation = knobObject.localRotation;
    }

    void Update()
    {
        // -----------------------
        // IDLE TIMER & AUTO-SPIN
        // -----------------------
        if (isAvaliable)   // ✅ Only start counting time when machine is available
        {
            if (HasUserInputThisFrame())
                idleTimer = 0f;
            else
                idleTimer += Time.deltaTime;

            if (enableAutoSpin &&
                !isAutoSpinning &&
                !isRotating &&
                points == 0 &&
                idleTimer >= idleTimeToAutoSpin)
            {
                StartCoroutine(AutoSpinRoutine());
            }
        }
        else
        {
            // Machine not available → ensure idle timer never starts
            idleTimer = 0f;
        }

        // -----------------------
        // MANUAL SPIN INPUT
        // -----------------------
        if (Input.GetMouseButtonDown(0))
        {
            if (!isAvaliable && !IsMouseOverKnob()) return;
            if (points >= maxTurns) return; // already done, ignore until mouse released or external reset

            worldAxis = knobObject.TransformDirection(localAxis.normalized);
            dragPlane = new Plane(worldAxis, knobObject.position);

            if (TryGetMouseOnPlane(out Vector3 hitPoint))
            {
                prevVecWorld = hitPoint - knobObject.position;
                isRotating = true;
            }
            spinUI.SetActive(false);
        }

        if (Input.GetMouseButtonUp(0)) isRotating = false;
        if (!isRotating) return;

        if (TryGetMouseOnPlane(out Vector3 currHit))
        {
            Vector3 currVecWorld = currHit - knobObject.position;
            float delta = Vector3.SignedAngle(prevVecWorld, currVecWorld, worldAxis);

            // Clockwise-only for your setup: allow positive deltas only.
            if (delta > 0f && points < maxTurns)
            {
                float applied = delta * rotationSpeed;

                knobObject.Rotate(localAxis, applied, Space.Self);
                accumulatedDegreesCW += applied;

                // Award points per full clockwise turn; handle multiple in one frame
                while (accumulatedDegreesCW >= 360f && points < maxTurns)
                {
                    accumulatedDegreesCW -= 360f;
                    points++;
                    OnFullTurn?.Invoke();
                    gachaAnim.SetTrigger("Shake");

                    // If we just hit the cap, stop & reset immediately
                    if (points >= maxTurns)
                    {
                        if (clickVfx != null) clickVfx.Play();
                        FinishAndReset();
                        break;
                    }
                }
            }

            prevVecWorld = currVecWorld;
        }
    }

    // -----------------------
    // AUTO-SPIN COROUTINE
    // -----------------------
    private IEnumerator AutoSpinRoutine()
    {
        isAutoSpinning = true;
        isRotating = false;
        spinUI.SetActive(false);

        accumulatedDegreesCW = 0f;
        points = 0;

        int targetTurns = maxTurns; // 3 by default

        for (int i = 0; i < targetTurns; i++)
        {
            float spunDegrees = 0f;

            // Spin 1 full turn
            while (spunDegrees < 360f)
            {
                float step = autoSpinSpeed * Time.deltaTime;
                knobObject.Rotate(localAxis, step, Space.Self);
                spunDegrees += step;
                yield return null;
            }

            points++;
            OnFullTurn?.Invoke();
            if (gachaAnim != null) gachaAnim.SetTrigger("Shake");
        }

        if (clickVfx != null) clickVfx.Play();
        FinishAndReset();

        idleTimer = 0f;        // reset so it won't immediately spin again
        isAutoSpinning = false;
    }

    void FinishAndReset()
    {
        // Stop current drag
        isRotating = false;

        // Reset transform and counters
        knobObject.localRotation = initialLocalRotation;
        accumulatedDegreesCW = 0f;
        points = 0;

        OnMaxTurnsReached?.Invoke();
    }

    bool TryGetMouseOnPlane(out Vector3 hitPoint)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            hitPoint = ray.GetPoint(enter);
            return true;
        }
        hitPoint = default;
        return false;
    }

    bool IsMouseOverKnob()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.transform == knobObject || hit.transform.IsChildOf(knobObject);
        }
        return false;
    }

    // Simple "did the player touch anything" check
    bool HasUserInputThisFrame()
    {
        // You can make this smarter (mouse movement, key presses, etc.)
        if (Input.GetMouseButtonDown(0) ||
            Input.GetMouseButton(0) ||
            Input.GetMouseButtonUp(0))
            return true;

        return false;
    }

    public int GetGachaPrice()
    {
        return database.machinePrice;
    }

    public Animator GetMachineAnim()
    {
        return gachaAnim;
    }

    public GachaMachineDatabase GetMachineDatabase()
    {
        return database;
    }
}
