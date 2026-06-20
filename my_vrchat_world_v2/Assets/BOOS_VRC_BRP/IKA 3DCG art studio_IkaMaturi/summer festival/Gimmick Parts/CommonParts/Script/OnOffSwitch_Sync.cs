
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class OnOffSwitch_Sync : UdonSharpBehaviour
{
    [SerializeField] private GameObject _model;
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ModelSwitch))] private bool _flg = true;

    public bool ModelSwitch
    {
        get => _flg;
        set
        {
            _flg = value;
            _model.SetActive(_flg);
        }
    }

    void Start()
    {
        
    }

    public override void Interact()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(ShowSwitch));
    }

    public void ShowSwitch()
    {
        if (ModelSwitch)
        {
            ModelSwitch = false;
        }
        else
        {
            ModelSwitch = true;
        }
    }
}
