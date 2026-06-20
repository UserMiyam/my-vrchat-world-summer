
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Star_Water_Melon : UdonSharpBehaviour
{
    public GameObject _piece;
    public Animator animator;

    void Start()
    {
        animator = this.GetComponent<Animator>();
    }

    public void PeacePosReset()
    {
        for (int i = 0; i < _piece.transform.childCount; i++)
        {
            _piece.transform.GetChild(i).gameObject.transform.GetChild(0).gameObject.transform.localPosition = Vector3.zero;
        }
    }
}
