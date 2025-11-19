using Gravitons.UI.Modal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaManager : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject machineStartInfo;
    [SerializeField] private Button startButton;
    [SerializeField] private MainMenu mainMenu;

    [Header("Module Reference")]
    [SerializeField] private GachaMachineSelector gachaMachineSelector;
    [SerializeField] private PlayerScript playerScirpt;
    [SerializeField] private ModalManager modalManager;
    [SerializeField] private GachaReward gachaRewardScript;
    private GachaController currGachaMachine;

    private float normalFOV = 80f;
    private float focusFOV = 65f;
    private float duration = 0.3f;
    private Coroutine fovRoutine;

    private void Start()
    {
        mainMenu.gameObject.SetActive(true);
        startButton.onClick.AddListener(StartGacha);
        gachaRewardScript.OnRewardClosed += HandleRewardClosed;
    }

    public void StartGacha()
    {
        ModalManager.Show("Start Gacha","Are you sure you want to pick this gacha?",
        new[] { new ModalButton() { Text = "NO"}, new ModalButton() { Text = "YES", Callback = CheckPlayerBalance} });
    }

    public void CheckPlayerBalance()
    {
        var gachaMachine = gachaMachineSelector.GetCurrentSelectedMachine();
        if (playerScirpt.CheckIssufficient(gachaMachine.GetGachaPrice()))
        {
            currGachaMachine = gachaMachine;
            playerScirpt.ReducePlayerCoin(currGachaMachine.GetGachaPrice());
            SelectGacha(currGachaMachine);
            currGachaMachine.OnMaxTurnsReached.AddListener(OnMaxTurns);
        }
        else
        {
            ModalManager.Show("Insufficient Coin", "Your coin is insufficient please recharge your coin first before selecting this gacha.",
       new[] { new ModalButton() { Text = "OK" }});
        }
    }

    private void OnMaxTurns()
    {
        //Roll
        var reward = gachaRewardScript.Roll(currGachaMachine.GetMachineDatabase());
        gachaRewardScript.SetReward(reward);
        gachaRewardScript.ShowReward();
        currGachaMachine.isAvaliable = false;
    }

    public void SelectGacha(GachaController gachaMachine)
    {
        StartMachineCouroutine();
        gachaMachineSelector.DisableSwipe();
        machineStartInfo.gameObject.SetActive(false);
    }

    public void StartMachineCouroutine()
    {
        StartFOVRoutine(normalFOV, focusFOV, duration);
        StartCoroutine(StartMachine());
    }
    void HandleRewardClosed()
    {
        OnFinishGacha();
    }

    public void OnFinishGacha()
    {
        gachaMachineSelector.EnableSwipe();
        ResetMachine();
        currGachaMachine.OnMaxTurnsReached.RemoveListener(OnMaxTurns);
    }

    public void ResetMachine()
    {
        StartFOVRoutine(focusFOV, normalFOV, duration);
        machineStartInfo.gameObject.SetActive(true);
    }

    // --- Camera Focus ---
    void StartFOVRoutine(float from, float to, float time)
    {
        if (fovRoutine != null)
            StopCoroutine(fovRoutine);

        fovRoutine = StartCoroutine(FOVRoutine(from, to, time));
    }

    IEnumerator FOVRoutine(float from, float to, float time)
    {
        Camera cam = Camera.main;
        if (cam == null)
            yield break;

        float t = 0f;
        cam.fieldOfView = from;

        while (t < time)
        {
            t += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(from, to, t / time);
            yield return null;
        }

        cam.fieldOfView = to;
    }

    IEnumerator StartMachine()
    {
        currGachaMachine.GetMachineAnim().SetTrigger("Start");
        yield return new WaitForSeconds(1.5f);
        currGachaMachine.spinUI.SetActive(true);
        currGachaMachine.isAvaliable = true;
    }
}
