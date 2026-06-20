
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;
using VRC.Udon.Common;

namespace yoshio_will.common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PageSwitch : UdonSharpBehaviour
    {
        [SerializeField, Multiline] public string[] Captions;
        [SerializeField] public GameObject[] GameObjects;
        [SerializeField] public Animator Animator;
        [SerializeField] public string AnimParamName;
        [SerializeField] public bool IsNetworkSynced;
        [SerializeField] public TextMeshProUGUI Text;

        private int _selection = 0;
        [UdonSynced] private int _selectionSynced;
        private int _animParam;
        private bool _isInitialized = false;

        const float InitializeRetryDelay = 1.5f;

        void Start()
        {
            Init();
        }

        public void Init()
        {
            if (_isInitialized) return;

            if (IsNetworkSynced)
            {
                if (!Networking.IsNetworkSettled)
                {
                    SendCustomEventDelayedSeconds(nameof(Init), InitializeRetryDelay);
                    return;
                }
            }

            _animParam = Animator.StringToHash(AnimParamName);

            int captions = Captions.Length;
            int gameObjects = GameObjects.Length;
            if (captions != gameObjects)
            {
                int elements = captions;
                if (gameObjects < elements) elements = gameObjects;

                string[] newCaptions = new string[elements];
                GameObject[] newGameObjects = new GameObject[elements];
                Array.Copy(Captions, newCaptions, elements);
                Array.Copy(GameObjects, newGameObjects, elements);
                Captions = newCaptions;
                GameObjects = newGameObjects;
            }

            _isInitialized = true;

            UpdateState();
        }

        private void UpdateState(bool isActively = false)
        {
            if (!_isInitialized) Init();

            if (_selection < 0) _selection = GameObjects.Length - 1;
            if (_selection >= GameObjects.Length) _selection = 0;

            for (int idx = 0; idx < GameObjects.Length; idx++) GameObjects[idx].SetActive(idx == _selection);
            Text.text = Captions[_selection];
            if (Animator) Animator.SetInteger(_animParam, _selection);

            if (IsNetworkSynced && isActively)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                _selectionSynced = _selection;
                RequestSerialization();
            }
        }

        public void OnButtonLeft()
        {
            _selection--;
            UpdateState(true);
        }

        public void OnButtonRight()
        {
            _selection++;
            UpdateState(true);
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            if (!IsNetworkSynced) return;
            if (!_isInitialized) return;
            _selection = _selectionSynced;
            UpdateState();
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!IsNetworkSynced) return;   // 必要ないから
            if (!_isInitialized) return;    // 準備出来てないから
            if (!Utilities.IsValid(player)) return; // isLocalの判断できないから
            if (player.isLocal) return; // 自分がJoinした時は要らないから
            if (!Networking.IsNetworkSettled) return;   // この場合オーナー判定が嘘かもしれないから
            if (!Networking.IsOwner(gameObject)) return;    // オーナーじゃないから

            RequestSerialization();     // 同期
        }
    }
}
