
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class Syrup : UdonSharpBehaviour
{
    [SerializeField] private GameObject _ps;
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(AngleCount))] private float _timeCount = 0;

    public float AngleCount
    {
        get => _timeCount;
        set
        {
            _timeCount = value;
        }
    }
    void Start()
    {
        _ps.SetActive(false);
        AngleCount = 0f;
    }

    private void Update()
    {
        if (30f < Vector3.Angle(this.transform.up, Vector3.up))
        {
            if (AngleCount < 30f)
            {
                _ps.SetActive(true);
                AngleCount += Time.deltaTime;
            }
            else
            {
                _ps.SetActive(false);
            }
        }
        else
        {
            _ps.SetActive(false);
            AngleCount = 0f;
        }

    }
}
