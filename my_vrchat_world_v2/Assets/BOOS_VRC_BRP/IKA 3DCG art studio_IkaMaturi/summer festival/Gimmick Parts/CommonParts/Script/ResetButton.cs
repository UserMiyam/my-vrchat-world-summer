
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ResetButton : UdonSharpBehaviour
{
    public GameObject[] _objs;
    private Vector3 _resetVec = new Vector3(0, -10000f, 0);

    public override void Interact()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetButtonMethod));
    }

    public void ResetButtonMethod()
    {
        for (int i = 0; i < _objs.Length; i++)
        {
            if (Networking.LocalPlayer.IsOwner(_objs[i]))
            {
                _objs[i].transform.position = _resetVec;
            }
        }
    }
}
