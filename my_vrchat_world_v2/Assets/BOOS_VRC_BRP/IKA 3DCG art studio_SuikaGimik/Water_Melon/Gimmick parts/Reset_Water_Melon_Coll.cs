
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Reset_Water_Melon_Coll : UdonSharpBehaviour
{
    private Bom_Water_Melon _bomMelon;
    private Star_Water_Melon _starMelon;

    void OnTriggerStay(Collider other)
    {
        _bomMelon = other.GetComponent<Bom_Water_Melon>();
        if (_bomMelon != null)
        {
            _bomMelon.animator.SetBool("trigger", false);
            _bomMelon.PeacePosReset();
        }
        _starMelon = other.GetComponent<Star_Water_Melon>();
        if (_starMelon != null)
        {
            _starMelon.animator.SetBool("trigger", false);
            _starMelon.PeacePosReset();
        }
    }
}
