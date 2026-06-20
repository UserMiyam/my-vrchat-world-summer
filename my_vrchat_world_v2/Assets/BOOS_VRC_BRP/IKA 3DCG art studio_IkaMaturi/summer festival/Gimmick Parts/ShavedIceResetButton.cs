
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ShavedIceResetButton : UdonSharpBehaviour
{
    [SerializeField] private GameObject[] _objArr;
    [SerializeField] private GameObject _resetPos;

    void Start()
    {

    }

    public override void Interact()
    {
        ResetObj();
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetObj));        
    }

    public void ResetObj()
    {
        for (int i = 0; i < _objArr.Length; i++)
        {
            ShavedIce ice = _objArr[i].transform.GetChild(0).GetComponent<ShavedIce>();
            if (ice != null)
            {
                ice.ChangeModelNo = -1;
            }
            _objArr[i].transform.position = _resetPos.transform.position;
        }
    }
}
