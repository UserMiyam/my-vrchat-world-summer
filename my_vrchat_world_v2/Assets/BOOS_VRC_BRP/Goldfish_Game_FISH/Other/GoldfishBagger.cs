using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GoldfishBagger : UdonSharpBehaviour
{
    [Header("時間設定")]
    public float baggingTime = 3.0f;
    public float unbaggingTime = 3.0f;

    [Header("オブジェクト設定")]
    public Transform bagSpawnPoint;
    public GoldfishBag[] bagPool; 

    [Header("UI設定")]
    public TextMeshProUGUI statusText; 
    public Image progressCircle; 

    [Header("テキスト設定")]
    [TextArea(2, 5)] public string textWaiting = "ボウルを置いてください";
    [TextArea(2, 5)] public string textBagging = "袋詰め中…";
    [TextArea(2, 5)] public string textUnbagging = "袋詰めを解除中…";
    [TextArea(2, 5)] public string textNoBagWarning = "利用可能な袋がありません。\nプールの数を増やしてください。";

    [UdonSynced] private int currentState = 0; 

    private float currentProgress = 0f;
    private GoldfishBowl targetBowl;
    private GoldfishBag targetBag;

    void Start()
    {
        ResetStateUI();
    }

    void Update()
    {
        if (currentState == 1)
        {
            UpdateUI(textBagging);
            currentProgress += Time.deltaTime / baggingTime;
            if (currentProgress > 1.0f) currentProgress = 1.0f;

            if (Networking.IsOwner(gameObject) && targetBowl != null && currentProgress >= 1.0f)
            {
                FinishBagging();
            }
        }
        else if (currentState == 2)
        {
            UpdateUI(textUnbagging);
            currentProgress += Time.deltaTime / unbaggingTime;
            if (currentProgress > 1.0f) currentProgress = 1.0f;

            if (Networking.IsOwner(gameObject) && targetBag != null && currentProgress >= 1.0f)
            {
                FinishUnbagging();
            }
        }
        else
        {
            ResetStateUI();
        }
    }

    private void UpdateUI(string text)
    {
        if (statusText != null) statusText.text = text;
        if (progressCircle != null)
        {
            progressCircle.gameObject.SetActive(true);
            progressCircle.fillAmount = currentProgress;
        }
    }

    public override void OnDeserialization()
    {
        if (currentState == 0) ResetStateUI();
    }

    void OnTriggerEnter(Collider other)
    {
        if (currentState != 0) return; 
        
        GoldfishBowl bowl = other.GetComponent<GoldfishBowl>();
        if (bowl != null)
        {
            if (bowl.type0Count > 0 || bowl.type1Count > 0 || bowl.type2Count > 0 || bowl.type3Count > 0)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                targetBowl = bowl;
                currentState = 1;
                currentProgress = 0f;
                RequestSerialization();
                return;
            }
        }

        GoldfishBag bag = other.GetComponent<GoldfishBag>();
        if (bag != null)
        {
            if (bag.isUsed)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                targetBag = bag;
                currentState = 2;
                currentProgress = 0f;
                RequestSerialization();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!Networking.IsOwner(gameObject)) return;

        if (currentState == 1 && targetBowl != null && other.gameObject == targetBowl.gameObject)
        {
            ResetToIdle();
        }
        else if (currentState == 2 && targetBag != null && other.gameObject == targetBag.gameObject)
        {
            ResetToIdle();
        }
    }

    private void FinishBagging()
    {
        GoldfishBag availableBag = null;
        foreach (var bag in bagPool)
        {
            if (bag != null && !bag.isUsed)
            {
                availableBag = bag;
                break;
            }
        }

        if (availableBag != null)
        {
            availableBag.transform.position = bagSpawnPoint.position;
            availableBag.transform.rotation = bagSpawnPoint.rotation;
            availableBag.SetupBag(targetBowl.type0Count, targetBowl.type1Count, targetBowl.type2Count, targetBowl.type3Count);
        }
        else
        {
            Debug.LogWarning(textNoBagWarning);
        }

        targetBowl.EmptyBowlForBagging();
        ResetToIdle();
    }

    private void FinishUnbagging()
    {
        if (targetBag != null) targetBag.ResetBag();
        ResetToIdle();
    }

    private void ResetToIdle()
    {
        currentState = 0;
        currentProgress = 0f;
        targetBowl = null;
        targetBag = null;
        RequestSerialization();
    }

    private void ResetStateUI()
    {
        currentProgress = 0f;
        if (statusText != null) statusText.text = textWaiting;
        if (progressCircle != null)
        {
            progressCircle.gameObject.SetActive(false);
            progressCircle.fillAmount = 0f;
        }
    }
}