
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class 説明書 : UdonSharpBehaviour
{

    public override void Interact()
    {
        this.gameObject.SetActive(false);

    }
}
