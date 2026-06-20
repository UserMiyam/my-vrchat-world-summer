
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace HenoScript
{
    public class HGSRespawner_Yataimeshi : UdonSharpBehaviour
    {
        [Header("This")]
        bool initialized;
        [SerializeField] bool poolMode;
        [SerializeField][Tooltip("単体モードならセット不要")] GameObject[] respawneeGoArray;

        [Header("Respawnee")]
        [SerializeField][Tooltip("Poolモードならセット不要")] GameObject respawneeGo;
        [SerializeField][Tooltip("Poolモードならセット不要")] GameObject respawneeSyncGo;
        VRCObjectSync respawneeSyncVRCObjectSync;
        UdonBehaviour hGSCoreScript;

        [Header("Other")]
        Collider tipCollider;

        void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (poolMode) { initialized = true; return; } ///Poolモードなら即リターン

            hGSCoreScript = respawneeGo.GetComponent<UdonBehaviour>();
            respawneeSyncVRCObjectSync = respawneeSyncGo.GetComponent<VRCObjectSync>();
            initialized = true;
        }

        public void OnTriggerEnter(Collider other)
        {
            /// <summary>
            /// 触れたオブジェクトの名前がHandやTipだった場合に処理発生
            /// 対象のCoreが準備よし状態であればリスポーンさせる
            /// Poolモードだった場合は、複数のCoreから休眠状態のものを探してリスポーンさせる
            /// </summary>

            if (!initialized) { Initialize(); }
            string _name = other.gameObject.name;

            switch (_name)
            {
                case "HGSTrackerHandL":

                    if (poolMode) { SearchInPool(); }　///PoolモードならPool内をサーチ

                    if (!Utilities.IsValid(hGSCoreScript)) { return; }
                    if (!(bool)hGSCoreScript.GetProgramVariable("pickupableH")) { return; }
                    int _foodState1 = (int)hGSCoreScript.GetProgramVariable("foodState"); ///switch関数内だと同じスコープとしてローカル変数が被るので、末尾に数字をつけて別変数とする
                    if (_foodState1 == 2 || _foodState1 == 3) { return; } ///手持ち箸持ち状態ならreturn
                    RespawnFood();

                    if (poolMode) { ClearRespawneeVar(); } ///Poolモードなら後片付け

                    break;

                case "HGSTrackerHandR":

                    if (poolMode) { SearchInPool(); }　///PoolモードならPool内をサーチ

                    if (!Utilities.IsValid(hGSCoreScript)) { return; }
                    if (!(bool)hGSCoreScript.GetProgramVariable("pickupableH")) { return; }
                    int _foodState2 = (int)hGSCoreScript.GetProgramVariable("foodState"); ///switch関数内だと同じスコープとしてローカル変数が被るので、末尾に数字をつけて別変数とする
                    if (_foodState2 == 2 || _foodState2 == 3) { return; } ///手持ち箸持ち状態ならreturn
                    RespawnFood();

                    if (poolMode) { ClearRespawneeVar(); } ///Poolモードなら後片付け

                    break;

                case "HGSTablewareTip":

                    if (poolMode) { SearchInPool(); }　///PoolモードならPool内をサーチ

                    if (!Utilities.IsValid(hGSCoreScript)) { return; }
                    if (!(bool)hGSCoreScript.GetProgramVariable("pickupableTDefaultValue")) { return; }
                    int _foodState3 = (int)hGSCoreScript.GetProgramVariable("foodState"); ///switch関数内だと同じスコープとしてローカル変数が被るので、末尾に数字をつけて別変数とする
                    if (_foodState3 == 2 || _foodState3 == 3) { return; } ///手持ち箸持ち状態ならreturn
                    RespawnFood();

                    ///Tipに触れた場合はテレポートも発動///
                    tipCollider = other;
                    SendCustomEventDelayedFrames(nameof(TeleportFoodToTip), 1);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 配列の中からコライダーがOff（＝休眠状態）になっているものを探す処理
        /// ※PooledがオンになっているCoreは、落下後や食後にリセットが呼ばれず、
        /// 代わりにコライダーとモデルをオフにしてその場で休眠状態となるため、このSearchInPool関数が成り立つ
        /// </summary>
        public void SearchInPool()
        {
            foreach (GameObject _respawneeGo in respawneeGoArray)
            {
                if (!Utilities.IsValid(_respawneeGo)) { continue; }
                if (_respawneeGo.GetComponent<Collider>().enabled == false)
                {
                    respawneeGo = _respawneeGo; ///GameObject取得
                    hGSCoreScript = _respawneeGo.GetComponent<UdonBehaviour>(); ///HGSCoreのUdon取得
                    respawneeSyncGo = (GameObject)hGSCoreScript.GetProgramVariable("syncGo"); ///SyncのGameObject取得
                    respawneeSyncVRCObjectSync = respawneeSyncGo.GetComponent<VRCObjectSync>(); ///SyncのVRCObjectSync取得
                    return;
                }
            }
        }

        public void RespawnFood()
        {
            if (!initialized) { Initialize(); }
            hGSCoreScript.SendCustomEvent("State7_Reset_Diffuser");
        }

        public void TeleportFoodToTip()
        {
            ///試作段階ではここでCoreのオーナー変更をしていたが、リセット処理と相性が悪かったため破棄///

            ///Coreをテレポート///
            Vector3 _pos = tipCollider.transform.position;
            Quaternion _rot = tipCollider.transform.rotation;
            respawneeGo.transform.SetPositionAndRotation(_pos, _rot);

            ///Syncも念の為テレポート///
            respawneeSyncVRCObjectSync.FlagDiscontinuity();
            respawneeSyncVRCObjectSync.TeleportTo(tipCollider.transform);

            ///後始末として各変数クリア///
            tipCollider = null;
            if (poolMode) { ClearRespawneeVar(); }
        }

        public void ClearRespawneeVar()
        {
            respawneeGo = null;
            hGSCoreScript = null;
            respawneeSyncGo = null;
            respawneeSyncVRCObjectSync = null;
        }
    }

}

