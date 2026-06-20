
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon;

public class PhoneSpownSystem : UdonSharpBehaviour
{
    [SerializeField]
    private VRCObjectPool phonePool;

    public override void Interact()
    {

        //このスクリプトのオーナーを自分に変更する
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        this.setPhoneCall();
    }


    private void setPhoneCall()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "phoneSpawn");
    }

    public void phoneSpawn()
    {
        phonePool.TryToSpawn();

    }
}
