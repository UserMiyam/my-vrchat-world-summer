
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace HenoScript
{
    public class YataimeshiContainer : UdonSharpBehaviour
    {
        [Header("This")]
        [SerializeField] Mesh[] meshArray; /// 1: 閉じ 2: 開き

        bool initialized;
        bool isOpen;

        [Header("Other")]
        [SerializeField] MeshFilter modelMeshFilter;


        private void OnEnable()
        {
            if(!initialized) { Initialize(); return; };
            Reset();
        }

        public void Initialize()
        {
            initialized = true;
        }

        public override void OnPickupUseDown()
        {
            if(!isOpen) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ContainerOpen)); }
            else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ContainerClose)); }
        }

        ///以下オリジナルイベント系///

        public void ContainerOpen()
        {
            modelMeshFilter.mesh = meshArray[1];
            isOpen = true;
        }

        public void ContainerClose()
        {
            modelMeshFilter.mesh = meshArray[0];
            isOpen = false;
        }

        public void Reset()
        {
            ///変数リセット///
            isOpen = false;

            ///見た目リセット///
            modelMeshFilter.mesh = meshArray[0];
        }
    }

}
