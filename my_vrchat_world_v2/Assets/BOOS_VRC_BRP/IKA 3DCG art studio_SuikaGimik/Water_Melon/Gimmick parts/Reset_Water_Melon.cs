
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Reset_Water_Melon : UdonSharpBehaviour
{
    [SerializeField] private GameObject _reSetColl;
    private int _timer = 0;

    void Start()
    {
        _reSetColl.SetActive(false);
    }

    private void Update()
    {
        if (_timer > 0)
        {
            _timer--;
        }
        else
        {
            _reSetColl.SetActive(false);
        }
    }

    public override void Interact()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetButton");
    }

    public void ResetButton()
    {
        _reSetColl.SetActive(true);
        _timer = 20;
    }
}
