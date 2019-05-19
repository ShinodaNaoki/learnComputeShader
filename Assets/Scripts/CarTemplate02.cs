using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 車の構造体
/// </summary>
public struct Car02 : ICarDynamicInfo
{
    public Vector2 pos { get; set; }
    public Vector2 direction { get; set; }
    public float velocity { get; set; }
}

/// <summary>
/// 車の構造体
/// </summary>
public struct Car02s : ICarStaticInfo
{
    public Vector3 size { get; set; }
    public Color color { get; set; }
}

/**
 * 車種テンプレート for CarsController02
 */
public class CarTemplate02 : ICarTemplate<Car02s,Car02>
{


    // 車種ごとの雛形を準備
    public static Dictionary<CarType, ICarTemplate<Car02s, Car02>> dictionary = new Dictionary<CarType, ICarTemplate<Car02s, Car02>>
    {
        {CarType.Hidden, new CarTemplate02(0, 0, 0, Color.black, 0f, 0) },
        {CarType.Normal, new CarTemplate02(2, 1, 4, Color.white, 1.0f, 80) },
        {CarType.Senior, new CarTemplate02(1.5f, 1, 2.5f, new Color(0.1f,0.8f,0.1f), 1.0f, 40) },
        {CarType.Sports, new CarTemplate02(2, 0.75f, 4, Color.red, 1.0f, 120) },
        {CarType.Bus,    new CarTemplate02(2.4f, 2f, 7f, Color.yellow, 0.85f, 60) },
        {CarType.TruckM, new CarTemplate02(2.4f, 2f, 7f, Color.cyan, 0.85f, 80) },
        {CarType.TruckL, new CarTemplate02(2.5f, 2f, 9, new Color(0.1f,0.1f,1), 0.7f, 100) },
    };


    /// <summary>
    /// サイズ
    /// </summary>
    internal Vector3 size { get; private set; }

    /// <summary>
    /// 色
    /// </summary>
    internal Color color { get; private set; }

    /// <summary>
    /// 運転者の理想速度、最大速度に影響
    /// </summary>
    internal float idealVelocity { get; private set; }

    /// <summary>
    /// 機動性(加減速効率、旋回半径などに影響)
    /// </summary>
    internal float mobility { get; private set; }

    public CarTemplate02(float sx, float sy, float sz, Color col, float mob, float speed)
    {
        size = new Vector3(sx, sy, sz);
        color = col;
        mobility = mob;
        idealVelocity = speed;
    }

    // ICarStaticInfo の生成
    public Car02s MakeStaticInfo()
    {
        Car02s data = new Car02s();
        data.color = color;
        data.size = size;
        return data;
    }

    // ICarDynamicInfo の生成
    public Car02 MakeDynaminInfo(Vector2 pos, Vector2 dir)
    {
        Car02 data = new Car02();
        data.direction = dir.normalized;
        data.velocity = idealVelocity;
        data.pos = pos;

        return data;
    }
}