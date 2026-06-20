
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Network;

public class NetworkMonitor2 : UdonSharpBehaviour
{ 
    [SerializeField] public Material ThroughputMat;
    [SerializeField] public Material BytesInMat;
    [SerializeField] public Material BytesOutMat;
    [SerializeField] public Material HitchesMat;
    [SerializeField] public Material SufferingsMat;
    [SerializeField] public Color BadColor = new Color(255, 0, 0);
    [SerializeField] public Color GoodColor = new Color(0, 255, 0);
    private int _idColor, _idValue;
    private float _maxHitches = 0.1f, _maxSufferings = 0.1f;
    const float BadValueThreshold = 0.95f;

    void Start()
    {
        _idColor = VRCShader.PropertyToID("_Color");
        _idValue = VRCShader.PropertyToID("_Value");
    }

    private void Update()
    {
        float hitches = Stats.HitchesPerNetworkTick;
        if (_maxHitches < hitches) _maxHitches = hitches;

        float sufferings = Stats.Suffering;
        if (_maxSufferings < sufferings) _maxSufferings = sufferings;

        SetMaterial(ThroughputMat, Stats.ThroughputPercentage);
        SetMaterial(BytesInMat, Stats.BytesInAverage / Stats.BytesInMax);
        SetMaterial(BytesOutMat, Stats.BytesOutAverage / Stats.BytesOutMax);
        SetMaterial(HitchesMat, hitches / _maxHitches);
        SetMaterial(SufferingsMat, sufferings / _maxSufferings);
    }

    private void SetMaterial(Material mat, float value)
    {
        mat.SetFloat(_idValue, value);
        mat.SetColor(_idColor, GetColor(value));
    }

    private Color GetColor(float value)
    {
        return (value > BadValueThreshold) ? BadColor : GoodColor;
    }
}
