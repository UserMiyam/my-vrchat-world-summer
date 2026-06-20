
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ShavedIceMachine : UdonSharpBehaviour
{
    public Animator _anim;

    public override void Interact()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(KakigooriAnime));
    }

    public void KakigooriAnime()
    {
        _anim.SetTrigger("switch");
    }
}
