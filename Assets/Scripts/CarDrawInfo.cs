using UnityEngine;

/// <summary>
/// 車の描画情報構造体
/// </summary>
public struct CarDrawInfo
{
    /// <summary>
    /// サイズ
    /// </summary>
    public Vector3 size;

    /// <summary>
    /// 色
    /// </summary>
    public Color color;

    /// <summary>
    /// 座標
    /// </summary>
    public Vector2 pos;

    /// <summary>
    /// 向き（進行方向）
    /// </summary>
    public Vector2 direction;
}