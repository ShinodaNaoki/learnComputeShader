using UnityEngine;

/// <summary>
/// 車の静的情報構造体
/// </summary>
public struct CarStaticInfo : ICarStaticInfo
{
    /// <summary>
    /// サイズ
    /// </summary>
    public Vector3 size { get; set;  }

    /// <summary>
    /// 色
    /// </summary>
    public Color color { get; set; }

    /// <summary>
    /// 運転者の理想速度、最大速度に影響
    /// </summary>
    public float idealVelocity { get; set; }

    /// <summary>
    /// 機動性(加減速効率、旋回半径などに影響)
    /// </summary>
    public float mobility { get; set; }
}