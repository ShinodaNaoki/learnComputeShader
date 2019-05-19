using UnityEngine;

public interface ICarDynamicInfo
{

    /// <summary>
    /// 座標
    /// </summary>
    Vector2 pos { get; set; }

    /// <summary>
    /// 向き（進行方向）
    /// </summary>
    Vector2 direction { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    float velocity { get; set; }
}
