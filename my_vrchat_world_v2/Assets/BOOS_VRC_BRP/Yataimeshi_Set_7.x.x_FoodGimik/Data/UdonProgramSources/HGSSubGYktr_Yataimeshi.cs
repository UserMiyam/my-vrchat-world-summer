
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using HenoScript;
using UnityEngine.UI;

namespace HenoScript
{
    ///SubG系スクリプトは、必要なくても必ず変数foodStateと変数pickupableTを宣言しておくこと！
    ///（TablewareTip側から参照されるので、無いとコンソール上にエラーが出る）///
    /// Inh系イベントあるためCore側のPassDownオン必須 ///

    ///↓Yktl系特有処理↓///
    ///OnPickupInhにて、親がピックアップされると見た目とコライダーをONする処理あり///
    ///State2_PickdUpByTablewareInhにて、親のState2に合わせて可視化＆可触化///
    ///State6_EatenでState6にしない、Stateは1で固定（Core側からのリセット処理の都合）
    ///State6_Eatenで無限食い防止措置あり
    ///State7_Resetにて、待機時コライダーOFFする処理あり///
    ///CollideWithHeadにて、State1でも食べられるよう回避処理あり///

    public class HGSSubGYktr_Yataimeshi : UdonSharpBehaviour
    {
        [Header("This")]
        [SerializeField] public bool pickupableT;
        bool pickupableTDefaultValue;
        [SerializeField] bool alwaysVisible;
        [SerializeField] bool eatable;

        Collider thisCollider;

        bool initialized;
        [HideInInspector] public int foodState = 1; /// 1: スタンバイ
        bool resetMode;

        [Header("Effects")]
        [SerializeField] AudioSource eatAudioSource;
        [SerializeField] AudioClip eatAudioClip;
        [SerializeField] ParticleSystem eatParticle;

        [Header("HGSCore")]
        [SerializeField] GameObject hGSCoreGo;
        UdonBehaviour hGSCoreScript;

        [Header("Other")]
        [SerializeField] GameObject modelGo;
        Collider otherCollider;

        [Header("Yktl")]
        bool eaten;

        [Header("Debug")]
        [SerializeField] Text debugText;


        private void OnEnable()
        {
            if (!initialized) { Initialize(); }
        }

        public void Initialize()
        {
            //thisPC = GetComponent<ParentConstraint>();
            //thisRigidBody = GetComponent<Rigidbody>(); ///リジッドボディ取得
            thisCollider = GetComponent<Collider>(); ///コライダー取得
            //thisVRCPickup = GetComponent<VRCPickup>(); ///VRCPickup取得
            //thisAnimator = GetComponent<Animator>(); ///Animator取得

            //syncPC = syncGo.GetComponent<ParentConstraint>(); ///SyncのParentConstraint取得
            //syncVRCObjectSync = syncGo.GetComponent<VRCObjectSync>(); ///SyncのVRCObjectSyncを取得

            hGSCoreScript = hGSCoreGo.GetComponent<UdonBehaviour>(); ///HGSCoreのスクリプトを取得

            //defaultParentTr = this.transform.parent; ///デフォルトの親オブジェクトを取得
            pickupableTDefaultValue = pickupableT; ///pickupableTの初期値を格納

            thisCollider.enabled = alwaysVisible;

            initialized = true;
        }

        /// <summary>
        /// 以下、HGSCoreのPassDown系変数をオンに設定した時のみ呼ばれる処理群
        /// </summary>

        ///親オブジェクトがピックアップされたときに発動///
        public void OnPickupInh()
        {
            ///親とオーナーを揃える///
            if (foodState != 1) { return; } ///待機状態でなければreturn
            VRCPlayerApi _newOwner = Networking.GetOwner(hGSCoreGo);
            Networking.SetOwner(_newOwner, this.gameObject);

            ///Yktl系特有処理（親がピックアップされると見た目とコライダーをONする）///
            if (eaten) { return; };
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ColliderOnAndModelOn));
        }

        ///親オブジェクトがUseDownされたときに発動///
        public void OnPickupUseDownInh()
        {

        }

        ///親オブジェクトがUseUpされたときに発動///
        public void OnPickupUseUpInh()
        {

        }

        ///親オブジェクトがDropされたときに発動///
        public void OnDropInh()
        {

        }

        /// 親オブジェクトが箸で持たれた時に発動（＝State2に移行した時）///        
        public void State2_PickdUpByTablewareInh()
        {
            ColliderOnAndModelOn(); //Yktl系特有処理　親のState2に合わせて可視化＆可触化
        }

        public void OnTriggerEnter(Collider other)
        {
            otherCollider = other;
            string _name = otherCollider.gameObject.name;

            switch (_name)
            {
                /*
                ///皿上コライダーと接触した場合の処理
                case "HGSDishCarryCollider":
                    CollideWithDish();
                    break;
                */

                ///頭コライダーと接触した場合の処理
                case "HGSTrackerHead":
                    CollideWithHead();
                    break;

                default:
                    otherCollider = null;
                    break;
            }
        }

        /// <summary>
        /// 以下ステート系
        /// </summary>

        public void State1_Ready()
        ///デフォルトはこれ.
        {

        }

        /*
        public void State2_PickedUpByTableware()
        {
            if (!initialized) { Initialize(); }
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(State2_PickedUpByTableware), 1); return; }

            foodState = 2;
            PhysicsOff();
            SwitchParentConst();
            ColliderOnAndModelOn();
            thisVRCPickup.pickupable = false;
            pickupableT = false;
            thisAnimator.SetBool("FreeFall", false);
        }
        */

        /*
        public void State2_PickedUpByTableware_Diffuser()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State2_PickedUpByTableware));
        }
        */

        /*
        public void State3_PickedUpByHand()
        {
            if (!initialized) { Initialize(); }
            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(State3_PickedUpByHand), 1); return; }

            foodState = 3;
            SetParentToRoot();
            PhysicsOff();
            SwitchParentConst();
            ColliderOnAndModelOn();
            thisAnimator.SetBool("FreeFall", false);

            ///オーナー以外はピックアップ不可能に///
            if (!Networking.IsOwner(this.gameObject))
            {
                thisVRCPickup.pickupable = false;
                pickupableT = false;
            }
        }
        */

        /*
        public void State4_FreeFall()
        {
            if (!initialized) { Initialize(); }

            ///ReleaseAndEatモード時は、即座にState6_Eatenに移行する///
            if (isTouchingHead)
            {
                State6_Eaten();
                return;
            }

            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(State4_FreeFall), 1); return; }

            foodState = 4;
            ColliderOnAndModelOn();
            thisVRCPickup.pickupable = pickupableH;
            pickupableT = pickupableTDefaultValue;
            SetParentToRoot();

            ///オーナーのみが物理オンしてAnimatorによる待ち時間を開始///
            if (!Networking.IsOwner(this.gameObject)) { return; }
            PhysicsOn();
            thisAnimator.SetBool("FreeFall", true);
        }
        */

        /*
        public void State5_OnDish()
        {
            foodState = 5;
            PhysicsOff();
            ColliderOnAndModelOn();
            thisAnimator.SetBool("FreeFall", false);

            if (!Utilities.IsValid(Networking.LocalPlayer)) { SendCustomEventDelayedFrames(nameof(State5_OnDish), 1); return; }
            if (!Networking.IsOwner(this.gameObject)) { return; }

            if (!Utilities.IsValid(otherCollider)) { return; }
            Transform dishParentTr = otherCollider.transform.parent;
            this.transform.parent = dishParentTr;
            syncGo.transform.parent = dishParentTr;
        }
        */

        public void State6_Eaten()
        {
            if (!initialized) { Initialize(); }

            //foodState = 6; ///Yktl特有処理（親側からのリセット処理の関係で、YktlのStateは1で固定）

            ///音とパーティクルを出しモデルを消す///
            eatAudioSource.PlayOneShot(eatAudioClip);
            eatParticle.Play(eatParticle);
            modelGo.SetActive(false);
            thisCollider.enabled = false; //Yktl特有処理（無限食い防止措置）
            eaten = true; //Yktl特有処理

            ///ピックアップ不可に＆ドロップ///
            //thisVRCPickup.pickupable = false;
            //pickupableT = false;
            //thisVRCPickup.Drop();

            //isTouchingHead = false;
            //thisAnimator.SetBool("FreeFall", false);

            //SendCustomEventDelayedSeconds(nameof(EatInterval), eatInterval);
        }

        public void State7_Reset()
        ///このステート自体は遷移上使用されない
        {
            if (!initialized) { Initialize(); }

            foodState = 1;
            resetMode = true;

            ///Syncの位置リセット///
            //syncPC.constraintActive = false;
            //syncVRCObjectSync.FlagDiscontinuity();
            //syncVRCObjectSync.TeleportTo(subGDefaultTr);

            ///本体のリセット///
            //thisPC.constraintActive = false;
            //PhysicsOff();
            //Vector3 _pos = subGDefaultTr.position;
            //Quaternion _rot = subGDefaultTr.rotation;
            //this.transform.SetPositionAndRotation(_pos, _rot);
            //thisVRCPickup.pickupable = pickupableH;
            //thisVRCPickup.Drop();
            pickupableT = pickupableTDefaultValue;
            //isTouchingHead = false;
            otherCollider = null;
            //thisAnimator.SetBool("FreeFall", false);
            //SetParentToDefault();

            modelGo.SetActive(alwaysVisible);
            thisCollider.enabled = alwaysVisible; ///Yktl系特有処理（待機時コライダーOFF）///
            eaten = false; ///Yktl系特有処理

            SendCustomEventDelayedFrames(nameof(ResetModeEnd), 1);
        }

        public void State7_Reset_Diffuser()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State7_Reset));
        }

        ///親のリセットの際に呼ばれる。サブギミックも同時にリセットするかどうかのフラグ（枝豆で必要な処理）
        public void State7_Reset_Inh()
        {
            State7_Reset();
        }

        public void ResetModeEnd()
        {
            resetMode = false;
        }

        /// <summary>
        /// 以下オリジナルイベント系
        /// </summary>

        public void CollideWithHead()
        {
            if (!eatable) { return; }
            //if (foodState == 1) { return; } ///犬食い防止用 ///Yktl系はコライダーのON/OFFで可食かどうか切り替えるので、State1でも食べられる
            if (foodState == 6) { return; } ///念のため
            //if (isTouchingHead) { return; } ///念のため重複防止

            /*
            ///手を離して食べるモードの場合の処理///
            HGSManager _hGSManager = otherCollider.GetComponentInParent<HGSManager>();
            if (_hGSManager.releaseAndEatMode)
            {
                ///手持ち中or箸持ち中なら特殊処理に移行///
                if (foodState == 2 || foodState == 3)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(IsTouchingHeadActive));
                    return;
                }
            }
            */

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(State6_Eaten));
        }

        /*
        public void IsTouchingHeadActive()
        {
            isTouchingHead = true;
        }
        */

        /*
        public void IsTouchingHeadInactive()
        {
            isTouchingHead = false;
        }
        */

        public void ColliderOnAndModelOn()
        {
            Debug.Log("ColliderOnAndModelOn fired!");

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

        /*
        public void EatInterval()
        {
            if (foodState != 6) { return; };
            State7_Reset();
        }
        */

    }
}


