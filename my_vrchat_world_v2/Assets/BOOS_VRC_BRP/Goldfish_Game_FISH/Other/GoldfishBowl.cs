using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class GoldfishBowl : UdonSharpBehaviour
{
    [Header("金魚がまとまっている親オブジェクト")]
    [Tooltip("Hierarchy上で金魚たちを入れている空のオブジェクト（親）を1つだけ登録してください")]
    public Transform fishParent;

    [Header("お椀の中の金魚モデル")]
    public GameObject[] type0Visuals;
    public GameObject[] type1Visuals;
    public GameObject[] type2Visuals; 
    public GameObject[] type3Visuals; 

    [UdonSynced, HideInInspector] public int type0Count = 0;
    [UdonSynced, HideInInspector] public int type1Count = 0;
    [UdonSynced, HideInInspector] public int type2Count = 0; 
    [UdonSynced, HideInInspector] public int type3Count = 0;

    private Vector3 spawnPos;
    private Quaternion spawnRot;
    private bool inWater = false;

    void Start()
    {
        spawnPos = transform.position;
        spawnRot = transform.rotation;
        UpdateVisuals();
    }

    void Update()
    {
        if (!Networking.IsOwner(gameObject)) return;

        if (inWater && transform.up.y < -0.5f)
        {
            if (type0Count > 0 || type1Count > 0 || type2Count > 0 || type3Count > 0)
            {
                ReleaseFishes();
            }
        }
    }

    public override void OnDeserialization()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        UpdateSingleTypeVisuals(type0Visuals, type0Count);
        UpdateSingleTypeVisuals(type1Visuals, type1Count);
        UpdateSingleTypeVisuals(type2Visuals, type2Count); 
        UpdateSingleTypeVisuals(type3Visuals, type3Count); 
    }

    private void UpdateSingleTypeVisuals(GameObject[] visuals, int count)
    {
        if (visuals == null) return;
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] != null) visuals[i].SetActive(i < count);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (other.gameObject.name.Contains("Water")) inWater = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (other.gameObject.name.Contains("Water")) inWater = false;
    }

    public void SyncAddFish0RPC() { type0Count++; UpdateVisuals(); }
    public void SyncAddFish1RPC() { type1Count++; UpdateVisuals(); }
    public void SyncAddFish2RPC() { type2Count++; UpdateVisuals(); }
    public void SyncAddFish3RPC() { type3Count++; UpdateVisuals(); }

    public void SyncEmptyBowlRPC()
    {
        type0Count = 0;
        type1Count = 0;
        type2Count = 0;
        type3Count = 0;
        UpdateVisuals();
    }

    public bool AddFish(int fishType, Swim_Generator_V3_Synced fishScript)
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        bool isAdded = false;

        if (fishType == 0) { if (type0Visuals != null && type0Count < type0Visuals.Length) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncAddFish0RPC)); isAdded = true; } }
        else if (fishType == 1) { if (type1Visuals != null && type1Count < type1Visuals.Length) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncAddFish1RPC)); isAdded = true; } }
        else if (fishType == 2) { if (type2Visuals != null && type2Count < type2Visuals.Length) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncAddFish2RPC)); isAdded = true; } }
        else if (fishType == 3) { if (type3Visuals != null && type3Count < type3Visuals.Length) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncAddFish3RPC)); isAdded = true; } }
        
        if (isAdded) RequestSerialization(); 
        return isAdded;
    }

    private void ReleaseFishes()
    {
        RespawnCaughtFishInScene();
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncEmptyBowlRPC));
        RequestSerialization();
    }

    public void ResetBowl()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        transform.position = spawnPos;
        transform.rotation = spawnRot;

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncEmptyBowlRPC));
        RequestSerialization();
    }
    
    public void EmptyBowlForBagging()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        transform.position = spawnPos;
        transform.rotation = spawnRot;
        
        RespawnCaughtFishInScene();

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncEmptyBowlRPC));
        RequestSerialization();
    }

    
    private void RespawnCaughtFishInScene()
    {
        if (fishParent == null) return;

        int released0 = 0, released1 = 0, released2 = 0, released3 = 0;

        
        for (int i = 0; i < fishParent.childCount; i++)
        {
            Transform child = fishParent.GetChild(i);
            Swim_Generator_V3_Synced fish = child.GetComponent<Swim_Generator_V3_Synced>();

            if (fish == null) continue;
            
            Collider col = fish.GetComponent<Collider>();
            if (col != null && !col.enabled)
            {
                if (fish.fishType == 0 && released0 < type0Count) { fish.RespawnFish(); released0++; }
                else if (fish.fishType == 1 && released1 < type1Count) { fish.RespawnFish(); released1++; }
                else if (fish.fishType == 2 && released2 < type2Count) { fish.RespawnFish(); released2++; }
                else if (fish.fishType == 3 && released3 < type3Count) { fish.RespawnFish(); released3++; }
            }
        }
    }
}