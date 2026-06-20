
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Stick : UdonSharpBehaviour
{
    public GameObject _hitColl;
    void Start()
    {
        _hitColl.SetActive(false);
    }

    public override void OnPickup()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PickupMethod");
    }

    public override void OnDrop()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DropMethod");
    }

    public void PickupMethod()
    {
        _hitColl.SetActive(true);
    }

    public void DropMethod()
    {
        _hitColl.SetActive(false);
    }
}
