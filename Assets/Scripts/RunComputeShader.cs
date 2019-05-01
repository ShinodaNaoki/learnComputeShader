using UnityEngine;
using System.Collections;
using System;

public class RunComputeShader : MonoBehaviour
{

    /// <summary>
    /// 使用するコンピュートシェーダー
    /// </summary>
    public ComputeShader computeShader;

    /// <summary>
    /// 書き込み可能なテクスチャ
    /// </summary>
    RenderTexture rwTexture;

    /// <summary>
    /// このスクリプトが生産されてからの経過時間
    /// </summary>
    float totalTime;

    /// <summary>
    /// 初期化
    /// </summary>
    void Start()
    {
        ResetRWTexture();
        totalTime = 0;
    }

    /// <summary>
    /// 書き込み可能なテクスチャをリセット
    /// </summary>
    void ResetRWTexture()
    {
        // 書き込み可能なテクスチャを生産。
        if (rwTexture != null) rwTexture.Release();
        rwTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        rwTexture.enableRandomWrite = true;
        rwTexture.Create();
        GC.Collect();
    }

    void Update()
    {
        totalTime += Time.deltaTime;
    }

    /// <summary>
    /// レンダリング
    /// </summary>
    void OnGUI()
    {
        if (Screen.width != rwTexture.width || Screen.height != rwTexture.height)
            ResetRWTexture();

        // コンピュートシェーダーを呼ぶ
        computeShader.SetTexture(0, "Result", rwTexture);
        computeShader.SetVector("Resolution", new Vector2(rwTexture.width, rwTexture.height));
        computeShader.SetFloat("Time", totalTime);
        computeShader.SetVector("Mouse", Input.mousePosition);
        computeShader.Dispatch(0, rwTexture.width / 8 + 1, rwTexture.height / 8 + 1, 1);

        // テクスチャを描画
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), rwTexture);
    }
}
