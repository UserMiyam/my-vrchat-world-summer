using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GoldfishGameReset : UdonSharpBehaviour
{
    [Header("親オブジェクトの登録")]
    [Tooltip("すべてのポイが格納されている親オブジェクトを指定してください")]
    public Transform poiParent;
    
    [Tooltip("すべての金魚が格納されている親オブジェクトを指定してください")]
    public Transform fishParent;
    
    [Tooltip("すべてのお椀が格納されている親オブジェクトを指定してください")]
    public Transform bowlParent;

    
    private GoldfishPoi[] allPois;
    private Swim_Generator_V3_Synced[] allFishes;
    private GoldfishBowl[] allBowls;

    void Start()
    {

        if (poiParent != null)
        {
            allPois = poiParent.GetComponentsInChildren<GoldfishPoi>(true);
        }

        if (bowlParent != null)
        {
            allBowls = bowlParent.GetComponentsInChildren<GoldfishBowl>(true);
        }

        if (fishParent != null)
        {
            allFishes = fishParent.GetComponentsInChildren<Swim_Generator_V3_Synced>(true);
        }
    }

    public override void Interact()
    {
        if (allPois != null)
        {
            foreach (GoldfishPoi poi in allPois)
            {
                if (poi != null) 
                {
                    poi.ResetPoi();
                }
            }
        }

        if (allBowls != null)
        {
            foreach (GoldfishBowl bowl in allBowls)
            {
                if (bowl != null) 
                {
                    bowl.ResetBowl();
                }
            }
        }

        if (allFishes != null)
        {
            foreach (Swim_Generator_V3_Synced fish in allFishes)
            {
                if (fish != null) 
                {
                    fish.gameObject.SetActive(true);
                    fish.RespawnFish();
                }
            }
        }
        
        Debug.Log("金魚すくいゲームの全システムをリセットしました！");
    }
}
