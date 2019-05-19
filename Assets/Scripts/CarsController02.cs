using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;


/// <summary>
/// 沢山の車を管理するクラス
/// </summary>
public class CarsController02 : MonoBehaviour
{
    const int MAX_CARS = 10000;

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
    CarRepository<Car02s,Car02> factory;

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
        InitializeComputeBuffer();        
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    void Update()
    {
        carComputeShader.SetBuffer(0, "CarsStatic", factory.StaticInfoBuffer);
        carComputeShader.SetBuffer(0, "CarsDynamic", factory.DynamicInfoBuffer);
        carComputeShader.SetFloat("DeltaTime", Time.deltaTime);
        carComputeShader.Dispatch(0, factory.Length / 8 + 1, 1, 1);
    }

    /// <summary>
    /// コンピュートバッファの初期化
    /// </summary>
    void InitializeComputeBuffer()
    {
        factory = new CarRepository<Car02s,Car02>(MAX_CARS, CarTemplate02.dictionary);
        factory.AssignBuffers();

        RoadPlane02 roadPlane = GetComponent<RoadPlane02>();
        var entries = roadPlane.EntryPoints;

        // 配列に初期値を代入する
        for (int i = 0; i < MAX_CARS; i++)
        {
            var entry = entries[ Random.Range(0,entries.Count) ];
            factory.CreateRandomType(entry.pos, entry.dir);
        }

        factory.ApplyData();
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