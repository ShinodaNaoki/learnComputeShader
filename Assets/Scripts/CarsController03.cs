using UnityEngine;
using System.Runtime.InteropServices;
using System;

/// <summary>
/// 車の構造体
/// </summary>
internal struct Car03 : ICarDynamicInfo
{
    public float velocity { get; set; }
    public Vector2 pos { get; set; }
    public Vector2 direction { get; set; }
}

/// <summary>
/// 沢山の車を管理するクラス
/// </summary>
public class CarsController03 : MonoBehaviour
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
    /// 車の表示用パーティクル
    /// </summary>
    public ParticleSystem particlesSystem;

    private ParticleSystem.Particle[] particles;

    /// <summary>
    /// パーティクルをGPU側で設定するか
    /// </summary>
    public bool setParticlesOnGPU;

    private ComputeBuffer particleBuffer;

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
        particleBuffer.Release();
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
        if (setParticlesOnGPU)
        {
            carComputeShader.SetBuffer(0, "Particles", particleBuffer);

            carComputeShader.SetBuffer(0, "CarsStatic", factory.StaticInfoBuffer);
            carComputeShader.SetBuffer(0, "CarsDynamic", factory.DynamicInfoBuffer);
            carComputeShader.SetFloat("DeltaTime", Time.deltaTime);
            carComputeShader.Dispatch(0, factory.Length / 8 + 1, 1, 1);
            gpu_UpdateParticles();
        }
        else
        {
            carComputeShader.SetBuffer(0, "CarsStatic", factory.StaticInfoBuffer);
            carComputeShader.SetBuffer(0, "CarsDynamic", factory.DynamicInfoBuffer);
            carComputeShader.SetFloat("DeltaTime", Time.deltaTime);
            carComputeShader.Dispatch(0, factory.Length / 8 + 1, 1, 1);
            cpu_UpdateParticles();
        }
    }

    /// <summary>
    /// 計算結果をパーティクルに適用する(GPU版)
    /// </summary>
    private void gpu_UpdateParticles()
    {
        var cars = factory.GetCars();
        var emitter = particlesSystem.emission;
        emitter.rateOverTime = cars.Length;
        int numParticlesAlive = cars.Length;

        particlesSystem.GetParticles(particles, cars.Length);
        particleBuffer.GetData(particles, 0, 0, numParticlesAlive);

        particlesSystem.SetParticles(particles, numParticlesAlive);
    }

    /// <summary>
    /// 計算結果をパーティクルに適用する
    /// </summary>
    private void cpu_UpdateParticles()
    {
        var cars = factory.GetCars();
        var emitter = particlesSystem.emission;
        emitter.rateOverTime = cars.Length;
        int numParticlesAlive = particlesSystem.GetParticles(particles, cars.Length);
        int i = 0;
        foreach (var car in cars)
        {
            var par = particles[i];
            var pos = car.Dynamic.pos;
            par.position = new Vector3(pos.x, 0.1f, pos.y) * 0.5f;
            par.startSize3D = car.Static.size;
            par.startColor = car.Static.color;
            var dir = car.Dynamic.direction;
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
        factory = new CarRepository<Car02s,Car02>(MAX_CARS, CarTemplate02.dictionary);
        factory.AssignBuffers();

        RoadPlane02 roadPlane = GetComponent<RoadPlane02>();
        var entries = roadPlane.EntryPoints;

        // 配列に初期値を代入する
        for (int i = 0; i < MAX_CARS; i++)
        {
            var entry = entries[ UnityEngine.Random.Range(0,entries.Count) ];
            factory.CreateRandomType(entry.pos, entry.dir);
        }

        factory.ApplyData();

        // Particle用バッファの準備
        particles = new ParticleSystem.Particle[MAX_CARS];
        particleBuffer = new ComputeBuffer(MAX_CARS, Marshal.SizeOf(typeof(ParticleSystem.Particle)));
        particleBuffer.SetData(particles);
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