using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System.Collections;
using System;
using Random = UnityEngine.Random;

using Car = ICar<Car03s, Car05d>;
using CarRepository = CarRepository<Car03s, Car05d>;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 沢山の車を管理するクラス
/// </summary>
public partial class CarsController06 : MonoBehaviour
{
#if CPU_DRIVING
    const int MAX_CARS = 100;
#else
    const int MAX_CARS = 400;
#endif

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

    private int[] Kernels;

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
#if CPU_DRIVING
#else
        carComputeShader.SetInt("count", factory.ActiveCars);
        carComputeShader.SetFloat("DeltaTime", Time.deltaTime);
        var carnum = factory.ActiveCars;
        foreach (int index in Kernels)
        {
            carComputeShader.SetBuffer(index, "CarsStatic", factory.StaticInfoBuffer);
            carComputeShader.SetBuffer(index, "CarsDynamic", factory.DynamicInfoBuffer);
            if(index == 1)
            { // Scanフェーズだけ二次元
                carComputeShader.Dispatch(index, carnum / 8 + 1, carnum / 8 + 1, 1);
            }
            else
            {
                carComputeShader.Dispatch(index, carnum / 64 + 1, 1, 1);
            }
        }
#endif
    }

    /// <summary>
    /// コンピュートバッファの初期化
    /// </summary>
    void InitializeComputeBuffer()
    {
        factory = new CarRepository(MAX_CARS, CarTemplate05.dictionary);
        factory.AssignBuffers();


        StartCoroutine(WatchLoop(OnEachScan, OnEachElement));

        Kernels = new int[]
        {
            carComputeShader.FindKernel("Init"),
            carComputeShader.FindKernel("Scan"),
            carComputeShader.FindKernel("Drive")
        };
        
        factory.ApplyData();
    }

    const int MIN_INTERVAL_FRAMES = 100;
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
        var car = factory.CreateRandomType(entry.pos, entry.dir);
        var car_d = car.Dynamic;
        car_d.lane = index;
        car.Dynamic = car_d;
        _lastEntries[index] = Time.frameCount;
        factory.ApplyData();
    }

    private void OnEachElement(int index, Car car)
    {
        if ( roadPlane.isInside(car.Dynamic.pos) ) return;

        factory.Remove(index);
        factory.ApplyData();
    }

#if CPU_DRIVING
    private IEnumerator WatchLoop(Action<Car[]> onScan, Action<int, Car> onElement)
    {
        while (true)
        {
            Car[] carsArray = factory.GetCars();

            //FullDump(carsArray);

            onScan(carsArray);
            int cars = factory.ActiveCars;
            for (int index = 0; index < cars; index++)
            {
                Drive(carsArray, index);
                onElement(index, carsArray[index]);
            }
            factory.ApplyData();
            yield return null;
        }
    }

    private void FullDump(Car[] carsArray)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < MAX_CARS; i++)
        {
            var car = carsArray[i];
            if (car.Static.size.z == 0)
            {
                sb.AppendFormat("#{0} Empty", i);
                sb.AppendLine();
                continue;
            }
            sb.Append(DebugStr(i, car));
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }
#else
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

#endif


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