using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// 車の構造体
/// </summary>
internal struct Car03 : IDriveInfo
{
    /// <summary>
    /// 速度
    /// </summary>
    public float velocity { get; set; }

    /// <summary>
    /// 運転者の理想速度、最大速度に影響
    /// </summary>
    public float idealVelocity { get; set; }

    /// <summary>
    /// 機動性(加減速効率、旋回半径などに影響)
    /// </summary>
    public float mobility { get; set; }
}

/// <summary>
/// 沢山の車を管理するクラス
/// </summary>
public class CarsController03 : MonoBehaviour
{
    const int MAX_CARS = 100;

    /// <summary>
    /// 車をレンダリングするシェーダー
    /// </summary>
    public Shader carShader;

    /// <summary>
    /// 車の移動を行うコンピュートシェーダー
    /// </summary>
    public ComputeShader carComputeShader;

    /// <summary>
    /// 車の表示用パーティクル
    /// </summary>
    public ParticleSystem particlesSystem;

    private ParticleSystem.Particle[] particles;

    /// <summary>
    /// 車ファクトリ
    /// </summary>
    CarRepository<Car03> factory;

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
        InitializeComputeBuffer();
        particles = new ParticleSystem.Particle[MAX_CARS];
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    void Update()
    {
        carComputeShader.SetBuffer(0, "DrawInfos", factory.DrawInfoBuffer);
        carComputeShader.SetBuffer(0, "DriveInfos", factory.DriveInfoBuffer);
        carComputeShader.SetFloat("DeltaTime", Time.deltaTime);
        carComputeShader.Dispatch(0, factory.Length / 8 + 1, 1, 1);
        UpdateParticles();
    }

    /// <summary>
    /// 計算結果をパーティクルに適用する
    /// </summary>
    private void UpdateParticles()
    {
        var cars = factory.GetCars();
        var emitter = particlesSystem.emission;
        emitter.rateOverTime = cars.Length;
        int numParticlesAlive = particlesSystem.GetParticles(particles, cars.Length);
        int i = 0;
        foreach (var car in cars)
        {
            var par = particles[i];
            var pos = car.DrawInfo.pos;
            par.position = new Vector3(pos.x, 0.1f, pos.y) * 0.5f;
            par.startSize3D = car.DrawInfo.size;
            par.startColor = car.DrawInfo.color;
            var dir = car.DrawInfo.direction;
            par.rotation3D = new Vector3(1, 1 - dir.y, dir.x) * 90;
            particles[i] = par;
            i++;
        }
        while(i < numParticlesAlive)
        {
            particles[i++].startLifetime = 0;
        }
        particlesSystem.SetParticles(particles, cars.Length);
    }

    /// <summary>
    /// コンピュートバッファの初期化
    /// </summary>
    void InitializeComputeBuffer()
    {
        factory = new CarRepository<Car03>(MAX_CARS);
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
    /// StatusTextへの表示用
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return factory.ActiveCars.ToString() + " Cars";
    }
}