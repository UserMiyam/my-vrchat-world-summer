
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class HGSManager_Yataimeshi : UdonSharpBehaviour
{

    [Header("Mode")]
    public bool releaseAndEatMode = false;

    [Header("Tracker")]
    [SerializeField] Transform trackerHead;
    [SerializeField] Transform trackerHandL;
    [SerializeField] Transform trackerHandR;

    bool initialized;

    public void Initialize()
    {
        ///if (releaseAndEatMode) { trackerHead.localScale = new Vector3(1.5f, 1.5f, 1.5f); } ///大きすぎたので廃止

        initialized = true;
    }

    public override void PostLateUpdate()
    {
        if (!initialized) { Initialize(); }
        if (!Utilities.IsValid(Networking.LocalPlayer)) { return; }

        trackerHead.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        trackerHead.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

        trackerHandL.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
        trackerHandL.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;

        trackerHandR.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
        trackerHandR.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;

    }
}
