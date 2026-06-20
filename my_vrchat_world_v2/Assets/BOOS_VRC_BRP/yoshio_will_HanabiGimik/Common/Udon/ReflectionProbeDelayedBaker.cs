
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(ReflectionProbe))]
    public class ReflectionProbeDelayedBaker : UdonSharpBehaviour
    {
        [SerializeField] public int Resolution = 1024;
        [SerializeField] public UnityEngine.Rendering.ReflectionProbeTimeSlicingMode TimeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
        [SerializeField] public float[] BakeTimes = { 5 };
        /* [SerializeField] public bool IsDisableProbeAfterAllBakes = false; */

        private ReflectionProbe _probe;
        private int _timeIdx, _bakeTimesLength, _renderId = -1;
        private float _startTime;

        void Start()
        {
            _probe = GetComponent<ReflectionProbe>();

            _probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;

            _bakeTimesLength = BakeTimes.Length;
            _timeIdx = 0;
            _startTime = Time.time;
        }

        private void Update()
        {
            if (_renderId >= 0)
            {
                if (_probe.IsFinishedRendering(_renderId))
                {
                    _renderId = -1;

                    RenderTexture tex = _probe.realtimeTexture;
                    _probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;
                    _probe.customBakedTexture = tex;
                }
                return;
            }

            if (_timeIdx >= _bakeTimesLength)
            {
                enabled = false;
                return;
            }

            if ((Time.time - _startTime) < BakeTimes[_timeIdx]) return;

            _timeIdx++;
            /*
            if (_timeIdx >= _bakeTimesLength && IsDisableProbeAfterAllBakes)
            {
                _probe.customBakedTexture = null;
                _probe.realtimeTexture = null;
                _probe.bakedTexture = null;
                _probe.enabled = false;
                return;
            }
            */

            _probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            _probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            _probe.resolution = Resolution;
            _renderId = _probe.RenderProbe();
        }
    }
}