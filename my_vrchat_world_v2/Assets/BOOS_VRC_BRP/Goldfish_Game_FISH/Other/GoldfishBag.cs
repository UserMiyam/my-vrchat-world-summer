using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class GoldfishBag : UdonSharpBehaviour
{
    [Header("見た目のオン/オフ")]
    [Tooltip("袋のメッシュや水面など、見た目となる子オブジェクトを登録してください（※ルートオブジェクトは入れないでください）")]
    public GameObject[] visualObjects; 

    [Header("袋の中の金魚モデル")]
    public GameObject[] type0Visuals;
    public GameObject[] type1Visuals;
    public GameObject[] type2Visuals;
    public GameObject[] type3Visuals;

    [UdonSynced] public bool isUsed = false; 
    [UdonSynced] private int count0 = 0;
    [UdonSynced] private int count1 = 0;
    [UdonSynced] private int count2 = 0;
    [UdonSynced] private int count3 = 0;

    private Collider bagCollider;
    private Rigidbody bagRigidbody;

    void Start()
    {
        bagCollider = GetComponent<Collider>();
        bagRigidbody = GetComponent<Rigidbody>();
        
        UpdateVisuals();
    }

    public void SetupBag(int c0, int c1, int c2, int c3)
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        count0 = c0;
        count1 = c1;
        count2 = c2;
        count3 = c3;
        isUsed = true;
        
        RequestSerialization();
        UpdateVisuals();
    }

    public override void OnDeserialization()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (visualObjects != null)
        {
            foreach (var obj in visualObjects)
            {
                if (obj != null) obj.SetActive(isUsed);
            }
        }

        if (bagCollider != null) bagCollider.enabled = isUsed;
        if (bagRigidbody != null) bagRigidbody.isKinematic = !isUsed; 

        UpdateSingleTypeVisuals(type0Visuals, count0);
        UpdateSingleTypeVisuals(type1Visuals, count1);
        UpdateSingleTypeVisuals(type2Visuals, count2);
        UpdateSingleTypeVisuals(type3Visuals, count3);
    }

    private void UpdateSingleTypeVisuals(GameObject[] visuals, int count)
    {
        if (visuals == null) return;
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] != null) visuals[i].SetActive(i < count);
        }
    }

    
    public void SyncBagResetRPC()
    {
        isUsed = false;
        count0 = 0;
        count1 = 0;
        count2 = 0;
        count3 = 0;
        UpdateVisuals();
    }

    public void ResetBag()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        
        
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SyncBagResetRPC));
        RequestSerialization(); 
    }
}