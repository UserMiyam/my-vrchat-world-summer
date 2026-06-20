
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.fireworks
{
    // 全花火リスポーンボタン
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class RespawnAll : UdonSharpBehaviour
    {
        [SerializeField] private GameObject Parent;

        public override void Interact()
        {
            Component[] comps = Parent.GetComponentsInChildren(typeof(UdonBehaviour));
            foreach (var comp in comps)
            {
                UdonBehaviour ub = (UdonBehaviour)comp;
                ub.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Respawn");
            }
        }
    }
}
