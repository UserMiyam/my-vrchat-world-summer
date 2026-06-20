
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ShavedIce : UdonSharpBehaviour
{
    [SerializeField] private GameObject _mainModel;
    [SerializeField] private GameObject[] _modelArr;
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ChangeModelNo))] private int _setModelNo = -1;

    public int ChangeModelNo
    {
        get => _setModelNo;
        set
        {
            _setModelNo = value;
            if (_setModelNo == -1) _mainModel.SetActive(true);
            else _mainModel.SetActive(false);
            for (int i = 0; i < _modelArr.Length; i++)
            {
                if (i == _setModelNo)
                {
                    _modelArr[_setModelNo].SetActive(true);
                }
                else
                {
                    _modelArr[i].SetActive(false);
                }
            }
            RequestSerialization();
        }
    }

    public void ColorReset()
    {
        SendCustomEventDelayedSeconds(nameof(ColorResetSub), 60f, VRC.Udon.Common.Enums.EventTiming.Update);
    }

    public void ColorResetSub()
    {
        ChangeModelNo = -1;
    }
}
