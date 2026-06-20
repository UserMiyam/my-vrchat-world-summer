using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class GoldfishPoi : UdonSharpBehaviour
{
    [Header("ポイの耐久度設定")]
    public float maxHP = 100f;
    public float baseWaterDamage = 10f; 
    public float moveDamageMultiplier = 20f; 
    
    [Header("オブジェクト参照")]
    public GameObject paperIntact; 
    public GameObject paperHalfBroken; 
    public GameObject paperBroken; 
    
    public GameObject caughtFishVisual0; 
    public GameObject caughtFishVisual1; 
    public GameObject caughtFishVisual2; 
    public GameObject caughtFishVisual3; 

    [Header("UI設定")]
    public Slider hpSlider;

    [Header("裏表判定の設定")]
    public Vector3 surfaceNormalAxis = new Vector3(0f, 0f, 1f);

    [UdonSynced] private float currentHP;
    [UdonSynced] private bool isBroken = false;
    [UdonSynced] private bool hasFish = false;
    [UdonSynced] private int currentCaughtFishType = 0;
    [UdonSynced] private bool isCaughtFlipped = false; 
    
    private bool inWater = false;
    private Vector3 lastPosition;
    private GameObject lastCaughtFish;
    private Vector3 spawnPos;
    private Quaternion spawnRot;

    private Vector3[] visualOrigPos = new Vector3[4];
    private Quaternion[] visualOrigRot = new Quaternion[4];
    
    private bool isHalfBrokenLocal = false; 

    void Start()
    {
        spawnPos = transform.position;
        spawnRot = transform.rotation;

        SaveOrigTransform(0, caughtFishVisual0);
        SaveOrigTransform(1, caughtFishVisual1);
        SaveOrigTransform(2, caughtFishVisual2);
        SaveOrigTransform(3, caughtFishVisual3);

        if (Networking.IsOwner(gameObject)) ResetPoi();
        else { UpdateVisuals(); UpdateHPBar(); }
    }

    private void SaveOrigTransform(int index, GameObject visual)
    {
        if (visual != null)
        {
            visualOrigPos[index] = visual.transform.localPosition;
            visualOrigRot[index] = visual.transform.localRotation;
        }
    }

    void Update()
    {
        if (isBroken) return;
        if (!Networking.IsOwner(gameObject)) return;

        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        if (inWater)
        {
            float speed = velocity.magnitude;
            float damage = (baseWaterDamage + (speed * moveDamageMultiplier)) * Time.deltaTime;
            currentHP -= damage;
            UpdateHPBar();

            if (currentHP <= (maxHP * 0.5f) && currentHP > 0 && !isHalfBrokenLocal)
            {
                isHalfBrokenLocal = true;
                UpdateVisuals();
            }

            if (currentHP <= 0) BreakPoi();
        }
    }

    public override void OnDeserialization()
    {
        UpdateVisuals();
        UpdateHPBar();
    }

    private void UpdateVisuals()
    {
        bool showIntact = !isBroken && currentHP > (maxHP * 0.5f);
        bool showHalf = !isBroken && currentHP <= (maxHP * 0.5f);
        
        if (paperIntact != null) paperIntact.SetActive(showIntact);
        if (paperHalfBroken != null) paperHalfBroken.SetActive(showHalf);
        if (paperBroken != null) paperBroken.SetActive(isBroken);
        
        UpdateSingleFishVisual(0, caughtFishVisual0);
        UpdateSingleFishVisual(1, caughtFishVisual1);
        UpdateSingleFishVisual(2, caughtFishVisual2);
        UpdateSingleFishVisual(3, caughtFishVisual3);
        
        if (hpSlider != null) hpSlider.gameObject.SetActive(!isBroken);
    }

    private void UpdateSingleFishVisual(int type, GameObject visual)
    {
        if (visual == null) return;
        bool isActive = hasFish && currentCaughtFishType == type;
        visual.SetActive(isActive);
    }

    void LateUpdate()
    {
        if (isBroken || !hasFish) return;

        UpdateSingleFishVisualTransformOnly(0, caughtFishVisual0);
        UpdateSingleFishVisualTransformOnly(1, caughtFishVisual1);
        UpdateSingleFishVisualTransformOnly(2, caughtFishVisual2);
        UpdateSingleFishVisualTransformOnly(3, caughtFishVisual3);
    }

    private void UpdateSingleFishVisualTransformOnly(int type, GameObject visual)
    {
        if (visual == null || !visual.activeSelf) return;
        if (currentCaughtFishType == type)
        {
            visual.transform.localPosition = isCaughtFlipped ? new Vector3(visualOrigPos[type].x, visualOrigPos[type].y, -visualOrigPos[type].z) : visualOrigPos[type];
            visual.transform.localRotation = isCaughtFlipped ? visualOrigRot[type] * Quaternion.Euler(0f, 180f, 0f) : visualOrigRot[type];
        }
    }

    private void UpdateHPBar()
    {
        if (hpSlider != null) hpSlider.value = Mathf.Clamp01(currentHP / maxHP);
    }

    public void SyncFishRemovedRPC()
    {
        hasFish = false;
        UpdateVisuals();
    }

    public void SyncPoiBrokenRPC()
    {
        isBroken = true;
        hasFish = false;
        UpdateVisuals();
    }

    private void BreakPoi()
    {
        if (hasFish && lastCaughtFish != null)
        {
            Swim_Generator_V3_Synced fishScript = lastCaughtFish.GetComponent<Swim_Generator_V3_Synced>();
            if (fishScript != null) fishScript.RespawnFish();
        }
        
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncPoiBrokenRPC));
        RequestSerialization(); 
    }

    void OnTriggerEnter(Collider other)
    {
        if (isBroken || !Networking.IsOwner(gameObject)) return;
        Swim_Generator_V3_Synced fishScript = other.GetComponent<Swim_Generator_V3_Synced>();

        if (other.gameObject.name.Contains("Water")) inWater = true;
        else if (fishScript != null && !hasFish)
        {
            Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
            if (velocity.y > 0.05f) CatchThisFish(other.gameObject, fishScript);
            else fishScript.FleeFrom(transform.position);
        }
        else if (other.gameObject.name.Contains("Bowl") && hasFish)
        {
            PutInBowl(other.GetComponent<GoldfishBowl>());
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (Networking.IsOwner(gameObject) && other.gameObject.name.Contains("Water")) inWater = false;
    }

    private void CatchThisFish(GameObject targetFish, Swim_Generator_V3_Synced fishScript)
    {
        hasFish = true;
        lastCaughtFish = targetFish;
        Vector3 surfaceWorldDir = transform.TransformDirection(surfaceNormalAxis.normalized);
        isCaughtFlipped = Vector3.Dot(surfaceWorldDir, Vector3.up) < 0f;
        currentCaughtFishType = (fishScript != null) ? fishScript.fishType : 0;
        if (fishScript != null) fishScript.CatchFish();
        
        RequestSerialization();
        UpdateVisuals();
    }

    private void PutInBowl(GoldfishBowl bowlScript)
    {
        if (bowlScript == null) return;

        int passedFishType = currentCaughtFishType; 
        Swim_Generator_V3_Synced passedFish = (lastCaughtFish != null) ? lastCaughtFish.GetComponent<Swim_Generator_V3_Synced>() : null;

        bool isAdded = bowlScript.AddFish(passedFishType, passedFish);

        if (!isAdded && passedFish != null)
        {
            passedFish.RespawnFish();
        }

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncFishRemovedRPC));
        RequestSerialization(); 
    }

    public void ResetPoi()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        transform.position = spawnPos;
        transform.rotation = spawnRot;
        currentHP = maxHP;
        isBroken = false;
        isHalfBrokenLocal = false; 
        hasFish = false;
        isCaughtFlipped = false;
        inWater = false;
        currentCaughtFishType = 0;
        lastPosition = transform.position;
        
        RequestSerialization();
        UpdateVisuals();
        UpdateHPBar();
    }
}