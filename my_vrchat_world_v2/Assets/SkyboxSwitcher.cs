using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// このスクリプトは「司令塔」役。
// SkyboxController などの空オブジェクトにアタッチして使う。
// GameObjectではなく「Skyboxマテリアル」を切り替える専用スクリプトです。
public class SkyboxSwitcher : UdonSharpBehaviour
{
    [Tooltip("切り替えたいSkyboxマテリアルを、ここへ順番にドラッグ＆ドロップしてください。0番から順に並びます。（例：0=墓地の夜、1=朝焼けパート4、2=天の川）")]
    public Material[] skyboxMaterials;

    [Tooltip("最初に表示しておきたいSkyboxの番号（0から数えます）")]
    public int defaultIndex = 0;

    // ワールドに入った人全員で同じSkyboxが見えるように同期する変数
    [UdonSynced]
    private int currentIndex;

    void Start()
    {
        currentIndex = defaultIndex;
        ApplySkybox();
    }

    // 他のプレイヤーが切り替えた時に、自分の画面にも反映するための処理
    public override void OnDeserialization()
    {
        ApplySkybox();
    }

    // ボタン側のスクリプトから呼び出される、切り替え本体の処理
    public void SwitchTo(int index)
    {
        if (skyboxMaterials == null || index < 0 || index >= skyboxMaterials.Length)
        {
            return;
        }

        // 同期変数を書き換える権限（オーナー権）を自分に移してから変更する
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        currentIndex = index;
        RequestSerialization(); // 他のプレイヤーへ変更を送信
        ApplySkybox();          // 自分の画面にも即反映
    }

    // Lighting > Environment > Skybox Material の中身を実際に差し替える処理
    private void ApplySkybox()
    {
        if (skyboxMaterials == null || currentIndex < 0 || currentIndex >= skyboxMaterials.Length)
        {
            return;
        }

        RenderSettings.skybox = skyboxMaterials[currentIndex];
    }
}
