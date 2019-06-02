using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System.Collections;
using System;
using Random = UnityEngine.Random;

using Car = ICar<Car02s, Car02>;
using CarRepository = CarRepository<Car02s, Car02>;
using System.Collections.Generic;

/// <summary>
/// 沢山の車を管理するクラス
/// </summary>
public class CarsController04 : MonoBehaviour
{
    const int MAX_CARS = 500;

    /// <summary>
    /// 車をレンダリングするシェーダー
    /// </summary>
    public Shader carShader;

    /// <summary>
    /// 車の移動を行うコンピュートシェーダー
    /// </summary>
    public ComputeShader carComputeShader;

    /// <summary>
    /// 情報表示用
    /// </summary>
    public Text statusText;

    /// <summary>
    /// 車のマテリアル
    /// </summary>
    Material material;

    /// <summary>
    /// 車ファクトリ
    /// </summary>
    CarRepository factory;

    /// <summary>
    /// 路面オブジェクト
    /// </summary>
    RoadPlane02 roadPlane;

    /// <summary>
    /// 破棄
    /// </summary>
    void OnDisable()
    {
        // コンピュートバッファは明示的に破棄しないと怒られます
        factory.ReleaseBuffers();
    }

    /// <summary>
    /// 初期化
    /// </summary>
    void Start()
    {
        material = new Material(carShader);
        roadPlane = GetComponent<RoadPlane02>();
        InitializeComputeBuffer();
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    void Update()
    {
        carComputeShader.SetBuffer(0, "CarsStatic", factory.StaticInfoBuffer);
        carComputeShader.SetBuffer(0, "CarsDynamic", factory.DynamicInfoBuffer);
        carComputeShader.SetInt("count", factory.ActiveCars);
        carComputeShader.SetFloat("DeltaTime", Time.deltaTime);
        carComputeShader.Dispatch(0, factory.Length / 8 + 1, 1, 1);
    }

    /// <summary>
    /// コンピュートバッファの初期化
    /// </summary>
    void InitializeComputeBuffer()
    {
        factory = new CarRepository(MAX_CARS, CarTemplate02.dictionary);
        factory.AssignBuffers();


        StartCoroutine(WatchLoop(OnEachScan, OnEachElement));

        factory.ApplyData();
    }

    const int MIN_INTERVAL_FRAMES = 60;
    private Dictionary<int, int> _lastEntries = new Dictionary<int, int>();

    private void OnEachScan(Car[] cars)
    {
        if (factory.ActiveCars >= MAX_CARS) return;

        var entries = roadPlane.EntryPoints;

        // 車を追加する
        int index = Random.Range(0, entries.Count);
        int f;
        if( _lastEntries.TryGetValue(index, out f) && Time.frameCount - f < MIN_INTERVAL_FRAMES)
        { // 最初から衝突するのを避けるため、一定フレーム数内に車が入ったばかりなら次の機会を待つ
            return; 
        }
        var entry = entries[index];
        factory.CreateRandomType(entry.pos, entry.dir);
        _lastEntries[index] = Time.frameCount;
        factory.ApplyData();
    }

    private void OnEachElement(int index, Car car)
    {
        if ( roadPlane.isInside(car.Dynamic.pos) ) return;

        factory.Remove(index);
        factory.ApplyData();
    }

    private int _currentIndex = 0;
    private IEnumerator WatchLoop(Action<Car[]> onScan, Action<int, Car> onElement)
    {
        // １フレーム中に進むカウント数
        const int COUNT_PER_FRAME = 10;
        Car[] carsArray = factory.GetCars();
        onScan(carsArray);
        while (true) {

            int cars = factory.ActiveCars;
            if (cars > 0)
            {
                if (_currentIndex >= cars)
                {
                    _currentIndex = 0;
                    carsArray = factory.GetCars();
                    onScan(carsArray);
                    yield return null;
                    continue;
                }
                var car = carsArray[_currentIndex];
                onElement(_currentIndex, car);
                _currentIndex++;

                // 
                if (_currentIndex % COUNT_PER_FRAME == 0)
                {
                    carsArray = factory.GetCars();
                    onScan(carsArray);
                    yield return null;
                    continue;
                }
            }
        }
    }

    /// <summary>
    /// レンダリング
    /// </summary>
    void OnRenderObject()
    {
        // 車データバッファをマテリアルに設定
        material.SetBuffer("CarsStatic", factory.StaticInfoBuffer);
        material.SetBuffer("CarsDynamic", factory.DynamicInfoBuffer);
        // レンダリングを開始
        material.SetPass(0);
        // オブジェクトをレンダリング
        Graphics.DrawProcedural(MeshTopology.Points, factory.ActiveCars);
    }

    /// <summary>
    /// StatusTextへの表示用
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return factory.ActiveCars.ToString() + " Cars";
    }
}