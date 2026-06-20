
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using HenoScript;
using UnityEngine.Animations;
using VRC.SDK3.Components;
using System.Runtime.CompilerServices;

namespace HenoScript
{
    public class HGSCore_Yataimeshi : UdonSharpBehaviour
    {
        [Header("This")]
        [SerializeField] public bool pickupableH = true;
        [SerializeField] public bool pickupableT = true;
        [HideInInspector] public bool pickupableTDefaultValue;
        [SerializeField] bool pooled;　///TrueならInitialize()、DropTimeOut()でリセットの代わりにClliderOffAndModelOffが呼ばれる
        [SerializeField] bool alwaysVisible;
        [SerializeField] bool eatable = true;
        Animator thisAnimator;

        ParentConstraint thisPC;
        Rigidbody thisRigidBody;
        Collider thisCollider;
        VRCPickup thisVRCPickup;
        bool initialized;
        [HideInInspector] public int foodState = 1; /// 1: スタンバイ, 2: 箸, 3: 手, 4: 落下, 5: 皿, 6: 食, 7: リセット
        bool resetMode;
        bool isTouchingHead;

        [Header("Sync")]
        [SerializeField] public GameObject syncGo;
        ParentConstraint syncPC;
        VRCObjectSync syncVRCObjectSync;

        [Header("Effects")]
        [SerializeField] AudioSource eatAudioSource;
        [SerializeField] AudioClip eatAudioClip;
        [SerializeField] ParticleSystem eatParticle;

        [Header("SubGimmick")]
        [SerializeField] GameObject subGGo;
        UdonBehaviour subGUdon;

        [Header("PassDown")]
        [Tooltip("ピックアップ系と同時に子（サブギミック）にも何かさせたい時はチェック必須")]
        [SerializeField] bool PassDownPickupState;

        [Header("Other")]
        Transform defaultParentTr;
        [SerializeField] Transform prefabRootTr;
        [SerializeField] GameObject modelGo;
        [SerializeField] Transform respawnerTr;
        [HideInInspector] public UdonBehaviour connectedTip;
        Collider otherCollider;

        [Header("Debug")]
        [SerializeField] Text debugText;



        private void OnEnable()
        {
            if (!initialized) { Initialize(); }

            if (foodState != 1) { State7_Reset(); } ///スイッチでOnされた場合の対策///

        }

        /*
        void Start()
        {
            if (!initialized) { Initialize(); }
        }
        */

        public void Initialize()
        {
            thisPC = GetComponent<ParentConstraint>();
            thisRigidBody = GetComponent<Rigidbody>(); ///リジッドボディ取得
            thisCollider = GetComponent<Collider>(); ///コライダー取得
            thisVRCPickup = GetComponent<VRCPickup>(); ///VRCPickup取得
            thisAnimator = GetComponent<Animator>(); ///Animator取得

            ///pooledアセットなら見た目とコライダーOFF
            if (pooled)
            {
                thisCollider.enabled = false;
                modelGo.SetActive(false);
            }

            syncPC = syncGo.GetComponent<ParentConstraint>(); ///SyncのParentConstraint取得
            syncVRCObjectSync = syncGo.GetComponent<VRCObjectSync>(); ///SyncのVRCObjectSyncを取得

            if (Utilities.IsValid(subGGo)) { subGUdon = subGGo.GetComponent<UdonBehaviour>(); } ///サブギミックのUdon取得

            defaultParentTr = this.transform.parent; ///デフォルトの親オブジェクトを取得
            pickupableTDefaultValue = pickupableT; ///pickupableTのデフォルト値を格納
            thisVRCPickup.pickupable = pickupableH; ///手でピックアップ可不可///

            initialized = true;
        }


        //public void Update()
        //{
        //    ///if (!initialized) { Initialize(); }

        //    ///if (thisCollider.enabled == true) { debugText.text = "enabled.";  }

        //    ///if (Networking.IsOwner(this.gameObject)) { debugText.text = "You are owner!"; }
        //    ///
        //    ///if (isTouchingHead) { debugText.text = "isTouchingHead = true"; }

        //    ///debugText.text = foodState.ToString();

        //    ///else { debugText.text = string.Empty; }
        //}


        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            ///オーナーはSyncのオーナーシップも変更する///
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(OnOwnershipTransferred), 1); return; }
            if (!Networking.IsOwner(this.gameObject)) { return; }
            Networking.SetOwner(Networking.LocalPlayer, syncGo);

            ///<summary>
            ///手持ち箸持ち状態でオーナーが落ちた時の対策
            ///新オーナーが2 or 3 で受け取ったということは、旧オーナーが落ちたと判断できるのでリセットとする
            ///(6は念のため）
            ///</summary>
            if (foodState == 2 || foodState == 3 || foodState == 6)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State7_Reset));
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            SendCustomEventDelayedSeconds(nameof(SyncStateForLaterJoiner), 1); ///Later joinerが落ち着くまで待ってから呼ぶ（オブジェクトONOFFスイッチの対策含む）
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            SendCustomEventDelayedSeconds(nameof(SyncStateForLeftPlayers), 1); ///新オーナーが定まるまで待ってから呼ぶ
        }

        public override void OnPickup()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State3_PickedUpByHand));

            ///SubGに引き継ぐ処理///
            if (Utilities.IsValid(subGUdon) && PassDownPickupState) { subGUdon.SendCustomEvent("OnPickupInh"); }
        }

        public override void OnPickupUseDown()
        {
            ///SubGに引き継ぐ処理///
            if (Utilities.IsValid(subGUdon) && PassDownPickupState) { subGUdon.SendCustomEvent("OnPickupUseDownInh"); }
        }

        public override void OnPickupUseUp()
        {
            ///SubGに引き継ぐ処理///
            if (Utilities.IsValid(subGUdon) && PassDownPickupState) { subGUdon.SendCustomEvent("OnPickupUseUpInh"); }
        }

        public override void OnDrop()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(OnDrop), 1); return; };
            if (!Networking.IsOwner(this.gameObject)) { return; } ///オーナー以外はreturn
            if (foodState != 3) { return; } ///手持ち中でない場合はreturn
            if (resetMode) { return; } ///リセットモード中ならreturn

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State4_FreeFall));

            ///SubGに引き継ぐ処理///
            if (Utilities.IsValid(subGUdon) && PassDownPickupState) { subGUdon.SendCustomEvent("OnDropInh"); }

        }

        public void OnTriggerEnter(Collider other)
        {
            otherCollider = other;
            string _name = otherCollider.gameObject.name;

            switch (_name)
            {
                ///皿上コライダーと接触した場合の処理
                case "HGSDishCarryCollider":
                    CollideWithDish();
                    break;

                ///頭コライダーと接触した場合の処理
                case "HGSTrackerHead":
                    CollideWithHead();
                    break;

                default:
                    otherCollider = null;
                    break;
            }
        }

        public void OnTriggerExit(Collider other)
        {
            ///頭コライダーから離れた場合の処理///
            if (other.gameObject.name != "HGSTrackerHead") { return; }
            UdonBehaviour _udonBehaviour = other.GetComponentInParent<UdonBehaviour>();
            if ((bool)_udonBehaviour.GetProgramVariable("releaseAndEatMode") == true)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(IsTouchingHeadInactive));
            }
        }

        /// <summary>
        /// 以下、HGSBaseのPassDownOnPickupState変数がオンに設定されている時のみ呼ばれる処理群
        /// </summary>

        public void OnPickupInh()
        {

        }

        public void OnPickupUseDownInh()
        {

        }

        public void OnPickupUseUpInh()
        {

        }

        public void OnDropInh()
        {

        }

        /// <summary>
        /// 以下ステート系
        /// </summary>

        public void State1_Ready()
        ///デフォルトはこれ.
        {

        }

        public void State2_PickedUpByTableware()
        {
            if (!initialized) { Initialize(); }
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(State2_PickedUpByTableware), 1); return; }

            foodState = 2;

            PhysicsOff();
            SwitchParentConst();
            ColliderOnAndModelOn();

            ///ピックアップ不可に///
            thisVRCPickup.pickupable = false;
            pickupableT = false;

            ///各タイマー停止///
            thisAnimator.SetBool("FreeFall", false);
            thisAnimator.SetBool("OnDish", false);
            thisAnimator.SetBool("IsEaten", false);

            ///SubGに引き継ぐ処理///
            if (Utilities.IsValid(subGUdon) && PassDownPickupState) { subGUdon.SendCustomEvent("State2_PickdUpByTablewareInh"); }
        }

        public void State2_PickedUpByTableware_Diffuser()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State2_PickedUpByTableware));
        }

        public void State3_PickedUpByHand()
        {
            if (!initialized) { Initialize(); }
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(State3_PickedUpByHand), 1); return; }

            foodState = 3;

            SetParentToRoot();
            PhysicsOff();
            SwitchParentConst();
            ColliderOnAndModelOn();

            ///各タイマー停止///
            thisAnimator.SetBool("FreeFall", false);
            thisAnimator.SetBool("OnDish", false);
            thisAnimator.SetBool("IsEaten", false);

            ///オーナー以外はピックアップ不可能に///
            if (!Networking.IsOwner(this.gameObject))
            {
                thisVRCPickup.pickupable = false;
                pickupableT = false;
            }
        }

        public void State4_FreeFall()
        {
            if (!initialized) { Initialize(); }
            if (resetMode) { return; } ///HGSTablewareのOnEnableリセットと競合した場合の保険（Tip経由でResetとFreeFallが同時に呼ばれる可能性がある）
            if (isTouchingHead) { State6_Eaten(); return; } ///ReleaseAndEatモード時かつ頭接触中なら、即座にState6_Eatenに移行する
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(State4_FreeFall), 1); return; }

            foodState = 4;

            ColliderOnAndModelOn();
            thisVRCPickup.pickupable = pickupableH;
            pickupableT = pickupableTDefaultValue;
            SetParentToRoot();

            ///Tipと関係性があればそれをクリアする///
            if (Utilities.IsValid(connectedTip))
            {
                connectedTip.SendCustomEvent("ClearVariables");
                connectedTip = null;
            }

            ///オーナーのみが物理オンしてAnimatorによる待ち時間を開始///
            if (!Networking.IsOwner(this.gameObject)) { return; }
            PhysicsOn();
            thisAnimator.SetBool("FreeFall", true);
        }

        public void State4_Freefall_Diffuser()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State4_FreeFall));
        }

        public void State5_OnDish()
        {
            Debug.Log("State5_OnDish called!");

            foodState = 5;

            PhysicsOff();
            ColliderOnAndModelOn();

            ///タイマー停止///
            thisAnimator.SetBool("FreeFall", false);

            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(State5_OnDish), 1); return; }
            if (!Networking.IsOwner(this.gameObject)) { return; }

            ///オーナーのみがAnimatorのタイマー開始///
            thisAnimator.SetBool("OnDish", true);

            ///オーナーのみが親子付け///
            if (!Utilities.IsValid(otherCollider)) { return; }
            Transform dishParentTr = otherCollider.transform.parent;
            this.transform.parent = dishParentTr;
            syncGo.transform.parent = dishParentTr;
        }

        public void State6_Eaten()
        {
            if (!initialized) { Initialize(); }
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(State6_Eaten), 1); return; }

            foodState = 6;

            ///音とパーティクルを出しモデルを消す///
            eatAudioSource.PlayOneShot(eatAudioClip);
            eatParticle.Play(eatParticle);
            modelGo.SetActive(false);

            ///ピックアップ不可に＆ドロップ///
            thisVRCPickup.pickupable = false;
            pickupableT = false;
            thisVRCPickup.Drop();

            ///頭接触判定と待ち時間をクリア///
            isTouchingHead = false;
            thisAnimator.SetBool("FreeFall", false);
            thisAnimator.SetBool("OnDish", false);

            ///Tipと関係性があればそれをクリアする///
            if (Utilities.IsValid(connectedTip))
            {
                connectedTip.SendCustomEvent("ClearVariables");
                connectedTip = null;
            }

            ///オーナーのみがAnimatorのタイマー開始///
            if (Networking.IsOwner(this.gameObject)) { thisAnimator.SetBool("IsEaten", true); }
        }

        public void State7_Reset()
        ///このステート自体は遷移上使用されない
        ///Pooledがオンの場合、落下後や食後にこの処理は呼ばれない。詳細はRespawnerのSearchInPool関数を参照
        {
            if (!initialized) { Initialize(); }

            foodState = 1;　///Respawnerの方の都合で1じゃないと駄目
            resetMode = true;

            ///Syncの位置リセット///
            syncPC.constraintActive = false;
            syncVRCObjectSync.FlagDiscontinuity();
            syncVRCObjectSync.TeleportTo(respawnerTr);
            //syncGo.transform.SetPositionAndRotation(respawnerTr.position, respawnerTr.rotation); ///ダメ押し

            ///本体のリセット///
            thisPC.constraintActive = false;
            PhysicsOff();
            Vector3 _pos = respawnerTr.position;
            Quaternion _rot = respawnerTr.rotation;
            this.transform.SetPositionAndRotation(_pos, _rot);
            thisVRCPickup.pickupable = pickupableH;
            pickupableT = pickupableTDefaultValue;
            thisVRCPickup.Drop();
            isTouchingHead = false;
            otherCollider = null;
            thisAnimator.SetBool("FreeFall", false);
            thisAnimator.SetBool("OnDish", false);
            thisAnimator.SetBool("IsEaten", false);
            SetParentToDefault();
            ///SetOwnershipToDefault(); Respawnerのテレポート処理とうまく共存できなかったため破棄
            if (pooled) { thisCollider.enabled = true; } /// pooledならコライダーのみをオンにする（Respawnerによるリスポーン＆テレポート処理時に、本コライダーがオンでないと箸に接触できない）

            ///Tipと関係性があればそれをクリアする///
            if (Utilities.IsValid(connectedTip))
            {
                connectedTip.SendCustomEvent("ClearVariables");
                connectedTip = null;
            }

            modelGo.SetActive(alwaysVisible);

            ///サブギミックがあればサブギミックもリセット///
            if (Utilities.IsValid(subGUdon)) { subGUdon.SendCustomEvent("State7_Reset_Inh"); }

            SendCustomEventDelayedFrames(nameof(ResetModeEnd), 1);
        }

        public void State7_Reset_Diffuser()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State7_Reset));
        }

        public void ResetModeEnd()
        {
            resetMode = false;
        }

        /// <summary>
        /// 以下オリジナルイベント系
        /// </summary>

        public void CollideWithDish()
        {
            ///自由落下中であればState5へ遷移///
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(CollideWithDish), 1); return; }
            if (!Networking.IsOwner(this.gameObject)) { return; }
            if (foodState == 4) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State5_OnDish)); }
        }

        public void CollideWithHead()
        {
            if (!eatable) { return; }
            if (foodState == 1) { return; } ///犬食い防止用
            if (foodState == 6) { return; } ///念のためリターン
            if (isTouchingHead) { return; } ///念のため重複防止

            ///手を離して食べるモードの場合の処理///
            UdonBehaviour _udonBehaviour = otherCollider.GetComponentInParent<UdonBehaviour>();
            if ((bool)_udonBehaviour.GetProgramVariable("releaseAndEatMode") == true)
            {
                ///手持ち中or箸持ち中なら特殊処理に移行///
                if (foodState == 2 || foodState == 3)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(IsTouchingHeadActive));
                    return;
                }
            }

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State6_Eaten));
        }

        public void IsTouchingHeadActive()
        {
            isTouchingHead = true;
        }

        public void IsTouchingHeadInactive()
        {
            isTouchingHead = false;
        }

        ///オーナーならSyncを引っ張る、非オーナーならSyncに追従とするスイッチ///
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

        public void PhysicsOn()
        {
            thisRigidBody.useGravity = true; ///重力ONに
            thisRigidBody.isKinematic = false; ///IsKinematicをOffに
            thisCollider.isTrigger = false; ///IsTriggerをOffに
        }

        public void PhysicsOff()
        {
            thisRigidBody.useGravity = false; ///重力OFFに
            thisRigidBody.isKinematic = true; ///IsKinematicをONに
            thisCollider.isTrigger = true; ///IsTriggerをOnに
        }

        public void ColliderOnAndModelOn()
        {
            if (!initialized) { Initialize(); }
            thisCollider.enabled = true; ///コライダーOn
            modelGo.SetActive(true); ///モデルOn
        }

        public void ColliderOffAndModelOff()
        {
            if (!initialized) { Initialize(); }
            thisCollider.enabled = false; ///コライダーOff
            modelGo.SetActive(false); ///モデルOff
        }

        ///Animatorから呼ばれる///
        public void DropTimeOut()
        {
            ///Animatorで規定された時間以上FreeFall状態で経過したらリセット（Pooledオンなら例外処理）///
            if (foodState != 4) { return; }
            if (pooled)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ColliderOffAndModelOff));
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PhysicsOff));
                thisAnimator.SetBool("FreeFall", false);
                return;
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State7_Reset));
            }
        }

        ///Animatorから呼ばれる///
        public void DishTimeOut()
        {
            ///Animatorで規定された時間以上お皿に乗っていたらリセット（Pooledオンなら例外処理）///
            if (foodState != 5) { return; }
            if (pooled)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ColliderOffAndModelOff));
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PhysicsOff));
                thisAnimator.SetBool("OnDish", false);
                return;
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State7_Reset));
            }
        }

        ///Animatorから呼ばれる///
        public void EatTimeOut()
        {
            ///Animatorで規定された時間経過したらリセット（Pooledオンなら例外処理）///
            if (foodState != 6) { return; }
            if (pooled)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ColliderOffAndModelOff));
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PhysicsOff));
                thisAnimator.SetBool("IsEaten", false);
                return;
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State7_Reset));
            }
        }

        /*
         * 未使用（待ち時間処理をアニメーターに任せたため）
        public void EatInterval()
        {
            if (foodState != 6) { return; }; ///Eaten状態でない場合はreturn

            ///pooledオンなら例外処理///
            if (pooled)
            {
                ColliderOffAndModelOff() ;
                PhysicsOff();
                return;
            }

            State7_Reset();
        }
        */

        public void SetParentToRoot()
        {
            if (!initialized) { Initialize(); }
            this.transform.SetParent(prefabRootTr);
            syncGo.transform.SetParent(prefabRootTr);
        }

        public void SetParentToDefault()
        {
            if (!initialized) { Initialize(); }
            this.transform.SetParent(defaultParentTr);
            syncGo.transform.SetParent(defaultParentTr);
        }

        /*
         * 未使用（Respawnerのテレポート処理とうまく共存できなかったため破棄）
        public void SetOwnershipToDefault()
        {
            
            if (!initialized) { Initialize(); }
            VRCPlayerApi newOwner = Networking.GetOwner(defaultParentTr.gameObject);
            if (!Utilities.IsValid(newOwner)) { SendCustomEventDelayedFrames(nameof(SetOwnershipToDefault), 1); return; };
            Networking.SetOwner(newOwner, this.gameObject);

        }
        */

        ///Later Joiner対策///
        public void SyncStateForLaterJoiner()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(SyncStateForLaterJoiner), 1); return; }
            if (!Networking.IsOwner(this.gameObject)) { return; }

            switch (foodState)
            {
                case 2:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State2_PickedUpByTableware));
                    break;

                case 3:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State3_PickedUpByHand));
                    break;

                case 4:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State4_FreeFall));
                    break;

                case 5:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State5_OnDish));
                    break;
            }

        }

        ///オーナーがLeftした後ためのステート継続処置///
        public void SyncStateForLeftPlayers()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(SyncStateForLeftPlayers), 1); return; }
            if (!Networking.IsOwner(this.gameObject)) { return; }

            switch (foodState)
            {
                case 2:
                    ///OnOwnershipTransfferedで対処するため処置なし
                    break;

                case 3:
                    ///OnOwnershipTransfferedで対処するため処置なし
                    break;

                case 4:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State4_FreeFall));
                    break;

                case 5:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State5_OnDish));
                    break;
            }

        }



    }
}
