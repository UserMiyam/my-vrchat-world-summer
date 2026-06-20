
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SyrupPS : UdonSharpBehaviour
{    
    void OnParticleCollision(GameObject other)
    {
        if (Networking.LocalPlayer.IsOwner(other))
        {
            ShavedIce _ice = other.GetComponent<ShavedIce>();
            if (_ice != null && _ice.ChangeModelNo == -1)
            {
                _ice.ChangeModelNo = Random.Range(0, 5);
                _ice.ColorReset();
            }
        }
    }    
}
