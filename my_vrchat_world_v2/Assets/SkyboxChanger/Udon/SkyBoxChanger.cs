
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
using VRC.Udon.Common;

public class SkyBoxChanger : UdonSharpBehaviour
{
    public Material[] skyboxes=new Material[3];
    public AudioSource changeSE = null;
    bool isAllNull=true;
    public bool isGlobal = false;

    [UdonSynced]
    int g_state = -1;
    [UdonSynced]
    bool g_hasChanged = false;

    // LocalUse
    int l_state = -1;

    public void Start()
    {
        if(skyboxes==null || skyboxes.Length == 0)
        {
            return;
        }
        for(int i = 0; i < skyboxes.Length; i++)
        {
            if (skyboxes[i] == null)
            {
                continue;
            }
            else
            {
                isAllNull = false;
            }
        }
    }

    public override void Interact()
    {
        if (isGlobal)
        {
            // For changing the parameter, need Owner permission.
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
            {
               Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            }
            changeSkybox();
            setSkybox();
            g_hasChanged = true;
        }
        else
        {
            // Local
            changeSkybox();
            setSkybox();
        }
    }

    public override void OnDeserialization()
    {
        // Sync only when global.
        if (isGlobal && g_hasChanged)
        {
            setSkybox();
        }
    }

    public override void OnPostSerialization(SerializationResult result)
    {
        if (isGlobal)
        {
            g_hasChanged = false;
        }
    }

    public void changeSkybox()
    {
        if (isAllNull) return;

        int state = l_state;
        if (isGlobal) state = g_state;

        int cnt = 0;
        do
        {
            state++;
            cnt++;
            if (state >= skyboxes.Length)
            {
                state = 0;
            }
            if (skyboxes[state] != null)
            {
                break;
            }
        } while (cnt<skyboxes.Length);

        if( isGlobal ) g_state = state;
        else l_state = state;
    }

    private void setSkybox()
    {
        int state = l_state;
        if (isGlobal) state = g_state;

        // Initate, ignore
        if (state < 0) return;

        if (skyboxes[state] != null)
         {
             RenderSettings.skybox = skyboxes[state];
             if (changeSE != null)
             {
                 changeSE.Play();
             }
         }
    }
}
