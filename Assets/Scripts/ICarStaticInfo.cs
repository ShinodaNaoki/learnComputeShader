using UnityEngine;
/// <summary>
/// 車の静的情報構造体
/// </summary>
public interface ICarStaticInfo
{
    /// <summary>
    /// サイズ
    /// </summary>
    Vector3 size { get; }

    /// <summary>
    /// 色
    /// </summary>
    Color color { get; }
}