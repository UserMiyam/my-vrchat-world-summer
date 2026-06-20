
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using HenoScript;
using UnityEngine.Animations;
using VRC.SDK3.Components;
using VRC.Udon.Common.Interfaces;

namespace HenoScript
{
    public class HGSDish_Yataimeshi : UdonSharpBehaviour
    {
        [Header("This")]
        ParentConstraint thisPC;
        Vector3 initialPosition;
        Quaternion initialRotation;
        VRCPickup thisVRCPickup;

        bool initialized;
        bool moved; ///スイッチEnable対策　初期位置から動いていればOnEnableでリセット処理を呼ぶ
        int foodState = 1; ///食品ではないが共通規格として宣言しておく
        bool resetMode;

        [Header("Sync")]
        [SerializeField] GameObject syncGo;
        ParentConstraint syncPC;
        VRCObjectSync syncVRCObjectSync;

        [Header("Other")]
        [SerializeField] Transform defaultTr;

        [Header("Debug")]
        [SerializeField] Text debugText;

        private void OnEnable()
        {
            if (!initialized) { Initialize(); }
            if (moved) { State7_Reset(); } ///スイッチでOnされた場合は位置リセット///
        }

        public void Initialize()
        {
            thisPC = GetComponent<ParentConstraint>();
            thisVRCPickup = GetComponent<VRCPickup>();
            syncPC = syncGo.GetComponent<ParentConstraint>();
            syncVRCObjectSync = syncGo.GetComponent<VRCObjectSync>();

            initialized = true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            ///オーナーはSyncのオーナーシップも変更///
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(OnOwnershipTransferred), 1); return; }
            if (!Networking.IsOwner(this.gameObject)) { return; }
            Networking.SetOwner(Networking.LocalPlayer, syncGo);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            SendCustomEventDelayedSeconds(nameof(SyncStateForLaterJoiner), 1); ///Later joinerが落ち着くまで待ってから呼ぶ（オブジェクトONOFFスイッチの対策含む）
        }

        public override void OnPickup()
        {
            ///State3へ移行///
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(State3_PickedUpByHand));

            ///SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetNewOwnerForChildren)); ボツ
        }

        ///Dropイベント特になし
        public override void OnDrop()
        {
            if (resetMode) { return; }
        }

        /// <summary>
        /// 以下ステート系
        /// </summary>

        public void State1_Ready()
        ///デフォルト
        {

        }

        public void State3_PickedUpByHand()
        {
            foodState = 3;

            SwitchParentConst();
            moved = true;
        }

        public void State7_Reset()
        ///このステート自体は遷移上使用されない
        {
            if (!initialized) { Initialize(); }

            foodState = 1;
            resetMode = true;
            thisVRCPickup.Drop();

            ///Syncの位置リセット///
            syncPC.constraintActive = false;
            syncVRCObjectSync.FlagDiscontinuity();
            syncVRCObjectSync.TeleportTo(defaultTr);
            //syncGo.transform.SetPositionAndRotation(defaultTr.position, defaultTr.rotation); ///ダメ押し

            ///本体のリセット///
            thisPC.constraintActive = false;
            transform.SetPositionAndRotation(defaultTr.position, defaultTr.rotation);

            moved = false;
            resetMode = false;
        }

        /// <summary>
        /// 以下オリジナルイベント系
        /// </summary>

        ///オーナーならSyncを引っ張る、非オーナーならSyncに追従とするスイッチ
        public void SwitchParentConst()
        {
            if (!initialized) { Initialize(); }
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(SwitchParentConst), 1); return; }
            if (Networking.IsOwner(this.gameObject))
            {
                thisPC.constraintActive = false;
                syncPC.constraintActive = true;
            }
            else
            {
                thisPC.constraintActive = true;
                syncPC.constraintActive = false;
            }
        }

        public void SyncStateForLaterJoiner()
        {
            ///foodState3なら、オーナーがLater joinerのためにState3を呼ぶ///
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(SyncStateForLaterJoiner), 1); return; }
            if (!Networking.IsOwner(this.gameObject)) { return; }
            if (foodState == 3) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State3_PickedUpByHand)); }
        }

        ///待機状態の子オブジェクト（HGSCore）達のオーナーシップを書き換え///
        /* ローカルでしか動かない無意味な処理になっているのでボツ
        public void SetNewOwnerForChildren()
        {
            int _childCount = this.transform.childCount;
            for (int i = 0; i < _childCount; i++)
            {
                Transform _childTr = this.transform.GetChild(i);
                HGSCore _hGSCore = _childTr.GetComponent<HGSCore>();

                if (!Utilities.IsValid(_hGSCore)) { continue; }

                Networking.SetOwner(Networking.LocalPlayer, _childTr.gameObject);
                Debug.Log("new owner set.");
            }
        }
        */

    }
}


