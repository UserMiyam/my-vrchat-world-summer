
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using HenoScript;

namespace HenoScript
{
    public class HGSTableware_Yataimeshi : UdonSharpBehaviour
    {
        [Header("This")]
        ParentConstraint thisPC;
        [SerializeField][Tooltip("0:null  1:箸やトング  2:フォークやスプーン")] int tablewareType = 1;
        VRC_Pickup thisVRCPickup;

        Vector3 initialPosition;
        Quaternion initialRotation;
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
        [SerializeField] Animator hashiAnimator;
        [SerializeField] GameObject tipGo;
        UdonBehaviour tipScript;

        [Header("Debug")]
        [SerializeField] Text debugText;


        public void OnEnable()
        {
            if (!initialized) { Initialize(); }
            if (moved) { State7_Reset(); } ///スイッチでOnされた場合は位置リセット///
        }

        /*
        void Start()
        {
            Initialize();
        }
        */

        public void Initialize()
        {
            thisPC = GetComponent<ParentConstraint>();
            thisVRCPickup = GetComponent<VRCPickup>();
            syncPC = syncGo.GetComponent<ParentConstraint>();
            syncVRCObjectSync = syncGo.GetComponent<VRCObjectSync>();
            tipScript = tipGo.GetComponent<UdonBehaviour>();

            initialized = true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            ///オーナーはSyncのオーナーシップも変更///
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(OnOwnershipTransferred), 1); return; }
            if (!Networking.IsOwner(this.gameObject)) { return; }
            Networking.SetOwner(Networking.LocalPlayer, syncGo);

            ///Tipのオーナーシップも変更///
            Networking.SetOwner(Networking.LocalPlayer, tipGo);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            SendCustomEventDelayedSeconds(nameof(SyncStateForLaterJoiner), 1); ///Later joinerが落ち着くまで待ってから呼ぶ（オブジェクトONOFFスイッチの対策含む）
        }

        public override void OnPickup()
        {
            ///State3へ移行///
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State3_PickedUpByHand));
        }

        public override void OnPickupUseDown()
        {
            ///箸タイプなら箸を少し閉じる///
            if (tablewareType == 1) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(HashiOpenHalf)); }

            ///Tipコライダーを有効化///
            tipScript.SendCustomEvent("TipColliderEnable");
        }

        public override void OnPickupUseUp()
        {
            ///箸タイプなら箸を開く///
            if (tablewareType == 1) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(HashiOpen)); }

            ///Tipを初期化///
            tipScript.SendCustomEvent("TipColliderDisable");
            tipScript.SendCustomEvent("ReleaseFood");
            tipScript.SendCustomEvent("ClearVariables");
        }

        public override void OnDrop()
        {
            if (resetMode) { return; }

            ///箸タイプなら箸を閉じる///
            if (tablewareType == 1) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(HashiClose)); }

            ///Tipを初期化///
            tipScript.SendCustomEvent("TipColliderDisable");
            tipScript.SendCustomEvent("ReleaseFood");
            tipScript.SendCustomEvent("ClearVariables");
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
            if (tablewareType == 1) { HashiOpen(); } ///箸タイプなら箸を開く
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

            ///箸タイプなら箸を閉じる///
            if (tablewareType == 1) { HashiClose(); }

            ///Tipを初期化///
            tipScript.SendCustomEvent("TipColliderDisable");
            tipScript.SendCustomEvent("ReleaseFood");
            tipScript.SendCustomEvent("ClearVariables");

            resetMode = false;
        }

        /// <summary>
        /// 以下オリジナルイベント系
        /// </summary>

        ///オーナーならSyncを引っ張る、/非オーナーならSyncに追従とするスイッチ
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

        public void HashiOpen()
        {
            if (!initialized) { Initialize(); }
            hashiAnimator.SetInteger("HashiOpen", 1);
        }

        public void HashiClose()
        {
            if (!initialized) { Initialize(); }
            hashiAnimator.SetInteger("HashiOpen", 0);
        }

        public void HashiOpenHalf()
        {
            if (!initialized) { Initialize(); }
            hashiAnimator.SetInteger("HashiOpen", 2);
        }

        public void SyncStateForLaterJoiner()
        {
            ///foodState3なら、オーナーがLater joinerのためにState3を呼ぶ///
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(SyncStateForLaterJoiner), 1); return; }
            if (!Networking.IsOwner(this.gameObject)) { return; }
            if (foodState == 3) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State3_PickedUpByHand)); }
        }


    }
}