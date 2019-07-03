using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 車の構造体
/// </summary>
public struct Car05d : ICarDynamicInfo
{
    public Vector2 pos { get; set; }
    public Vector2 direction { get; set; }
    public float velocity { get; set; }
    public int lane { get; set; }
    public int colider { get; set; } // 衝突予想相手
    public float ticks { get; set; } // 衝突予想時間

    public override string ToString()
    {
        return string.Format("[{0},{5}({1:0.0},{2:0.0})>>({3:0.0},{4:0.0})]", 
            typeof(Car05d).Name, 
            pos.x, pos.y, 
            direction.x * velocity, direction.y * velocity,
            lane
        );
    }
}


/**
 * 車種テンプレート for CarsController03
 */
public class CarTemplate05 : ICarTemplate<Car03s,Car05d>
{


    // 車種ごとの雛形を準備
    public static Dictionary<CarType, ICarTemplate<Car03s, Car05d>> dictionary = new Dictionary<CarType, ICarTemplate<Car03s, Car05d>>
    {
        {CarType.Hidden, new CarTemplate05(0, 0, 0, Color.black, 0f, 0) },
        {CarType.Normal, new CarTemplate05(2, 1, 4, Color.white, 1.0f, 80) },
        {CarType.Senior, new CarTemplate05(1.5f, 1, 2.5f, new Color(0.1f,0.8f,0.1f), 1.0f, 60) },
        {CarType.Sports, new CarTemplate05(2, 0.75f, 4, Color.red, 1.0f, 100) },
        {CarType.Bus,    new CarTemplate05(2.4f, 2f, 7f, Color.yellow, 0.85f, 70) },
        {CarType.TruckM, new CarTemplate05(2.4f, 2f, 7f, Color.cyan, 0.85f, 70) },
        {CarType.TruckL, new CarTemplate05(2.5f, 2f, 9, new Color(0.1f,0.1f,1), 0.7f, 90) },
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

    public CarTemplate05(float sx, float sy, float sz, Color col, float mob, float speed)
    {
        size = new Vector3(sx, sy, sz);
        color = col;
        mobility = mob;
        idealVelocity = speed;
    }

    // ICarStaticInfo の生成
    public Car03s MakeStaticInfo()
    {
        Car03s data = new Car03s();
        data.color = color;
        data.size = size;
        data.idealVelocity = idealVelocity;
        data.mobility = mobility;
        return data;
    }

    // ICarDynamicInfo の生成
    public Car05d MakeDynaminInfo(Vector2 pos, Vector2 dir)
    {
        Car05d data = new Car05d();
        data.direction = dir.normalized;
        data.velocity = idealVelocity;
        data.pos = pos;
        return data;
    }
}