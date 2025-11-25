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
    public ParticleSystem shortClickVfx;
    [SerializeField] private Animator gachaAnim;
    public GameObject spinUI;

    [Header("Rotation")]
    public Vector3 localAxis = Vector3.up;
    public float rotationSpeed = 1f;
    public bool isAvaliable = false;

    [Header("Scoring / Limits")]
    public int maxTurns = 3;
    public int points = 0;
    public UnityEvent OnFullTurn;
    public UnityEvent OnMaxTurnsReached;

    [Header("Auto Spin (Idle)")]
    public bool enableAutoSpin = true;
    public float idleTimeToAutoSpin = 5f;
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
        // IDLE TIMER & AUTO-SPIN
        if (isAvaliable)
        {
            if (HasUserInputThisFrame())
            {
                idleTimer = 0f;
            }
            else
            {
                idleTimer += Time.deltaTime;
            }

            // START auto-spin if we haven't reached maxTurns yet
            if (enableAutoSpin &&
                !isAutoSpinning &&
                !isRotating &&
                points < maxTurns &&               
                idleTimer >= idleTimeToAutoSpin)
            {
                StartCoroutine(AutoSpinRoutine());
            }
        }
        else
        {
            idleTimer = 0f;
        }

        // MANUAL SPIN INPUT
        if (Input.GetMouseButtonDown(0))
        {
            if (!isAvaliable && !IsMouseOverKnob()) return;
            if (points >= maxTurns) return;

            worldAxis = knobObject.TransformDirection(localAxis.normalized);
            dragPlane = new Plane(worldAxis, knobObject.position);

            if (TryGetMouseOnPlane(out Vector3 hitPoint))
            {
                prevVecWorld = hitPoint - knobObject.position;
                isRotating = true;
            }

            idleTimer = 0f;
            spinUI.SetActive(false);
        }

        if (Input.GetMouseButtonUp(0)) isRotating = false;
        if (!isRotating) return;

        if (TryGetMouseOnPlane(out Vector3 currHit))
        {
            Vector3 currVecWorld = currHit - knobObject.position;
            float delta = Vector3.SignedAngle(prevVecWorld, currVecWorld, worldAxis);

            if (delta > 0f && points < maxTurns)
            {
                float applied = delta * rotationSpeed;

                knobObject.Rotate(localAxis, applied, Space.Self);
                accumulatedDegreesCW += applied;

                while (accumulatedDegreesCW >= 360f && points < maxTurns)
                {
                    accumulatedDegreesCW -= 360f;
                    points++;
                    OnFullTurn?.Invoke();
                    if (gachaAnim != null) gachaAnim.SetTrigger("Shake");
                    if (shortClickVfx != null) shortClickVfx.Play();

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

    // AUTO-SPIN COROUTINE (spins remaining turns only)
    private IEnumerator AutoSpinRoutine()
    {
        isAutoSpinning = true;
        isRotating = false;
        isAvaliable = false;
        spinUI.SetActive(false);

        // Do NOT reset points here — spin only remaining turns
        int remainingTurns = Mathf.Clamp(maxTurns - points, 0, maxTurns);

        if (remainingTurns <= 0)
        {
            // Nothing to do (safety)
            isAutoSpinning = false;
            yield break;
        }

        // accumulate from current state (do not zero out accumulatedDegreesCW)
        for (int i = 0; i < remainingTurns; i++)
        {
            float spunDegrees = 0f;

            while (spunDegrees < 360f)
            {
                float step = autoSpinSpeed * Time.deltaTime;
                knobObject.Rotate(localAxis, step, Space.Self);
                spunDegrees += step;
                yield return null;
            }

            if (shortClickVfx != null) shortClickVfx.Play();
            points++;
            OnFullTurn?.Invoke();
            if (gachaAnim != null) gachaAnim.SetTrigger("Shake");
        }

        if (shortClickVfx != null) shortClickVfx.Play();
        if (clickVfx != null) clickVfx.Play();
        FinishAndReset();

        idleTimer = 0f;
        isAutoSpinning = false;
    }

    void FinishAndReset()
    {
        isRotating = false;

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

    bool HasUserInputThisFrame()
    {
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

    public int GetGachaPriceTen()
    {
        return database.machinePriceTen; 
    }


    public Animator GetMachineAnim()
    {
        return gachaAnim;
    }

    public GachaMachineDatabase GetMachineDatabase()
    {
        return database;
    }

    public int GetURPityLeft()
    {
        if (database == null || database.ultraRarePity <= 0)
            return -1; 

        return Mathf.Max(0, database.ultraRarePity - database.currentUltraRareRolls);
    }

    public int GetSRPityLeft()
    {
        if (database == null || database.superRarePity <= 0)
            return -1; 

        return Mathf.Max(0, database.superRarePity - database.currentSuperRareRolls);
    }
}
