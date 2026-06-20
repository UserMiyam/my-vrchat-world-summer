using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GimmickPrewarmer : UdonSharpBehaviour
{
    [Tooltip("ロード時に表示しておき、後で消すオブジェクト")]
    [SerializeField] private GameObject[] prewarmTargets;

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            
            foreach (var target in prewarmTargets)
            {
                if (target != null)
                {
                    target.SetActive(true);
                }
            }

            
            SendCustomEventDelayedSeconds(nameof(HideTargets), 1.0f);
        }
    }

    public void HideTargets()
    {
        foreach (var target in prewarmTargets)
        {
            if (target != null)
            {
                target.SetActive(false);
            }
        }
    }
}