
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class phone : UdonSharpBehaviour
{
    public GameObject camera1;
    public GameObject camera2;

    public MeshRenderer quad1;
    public MeshRenderer quad2;


    public AudioSource asShatter;
    public AudioClip acShatter;


    public BoxCollider Sheet;
    public MeshRenderer SheetShomen;
    public MeshRenderer SheetHaimen;

    public MeshRenderer shoumen;
    public MeshRenderer haimen;

    [UdonSynced(UdonSyncMode.None)] private bool blAngle = false;
    [UdonSynced(UdonSyncMode.None)] private bool blAfterPhoto = false;


    private void Update()
    {
        Sheet.enabled = blAfterPhoto;

        if (blAfterPhoto)
        {
            SheetShomen.enabled = !blAngle;
            SheetHaimen.enabled = blAngle;
            shoumen.enabled = false;
            haimen.enabled = false;
        }
        else
        {
            SheetShomen.enabled = false;
            SheetHaimen.enabled = false;
            shoumen.enabled = !blAngle;
            haimen.enabled = blAngle;
        }

        quad1.enabled = !blAngle;
        quad2.enabled =  blAngle;

    }

    public override void Interact()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(angleChange));
    }

    public void angleChange()
    {

        if (!blAfterPhoto)
        {
            blAngle = !blAngle;
        }

    }


    
    public override void OnPickup()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CameraOn));
    }
    public override void OnDrop()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CameraOff));
    }


    public override void OnPickupUseDown()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(shatter));
    }

    public void shatter()
    {
        if (!blAfterPhoto)
        {
            asShatter.PlayOneShot(acShatter);
        }

        camera1.SetActive(blAfterPhoto);
        camera2.SetActive(blAfterPhoto);
        blAfterPhoto = !blAfterPhoto;

    }

    public void CameraOn()
    {
        if (!blAfterPhoto)
        {
            camera1.SetActive(!blAfterPhoto);
            camera2.SetActive(!blAfterPhoto);

        }
    }
    public void CameraOff()
    {
        camera1.SetActive(false);
        camera2.SetActive(false);
    }
}
