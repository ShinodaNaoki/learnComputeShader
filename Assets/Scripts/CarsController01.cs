using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// 車の構造体
/// </summary>
struct Car01
{
    /// <summary>
    /// サイズ
    /// </summary>
    public Vector3 size;

    /// <summary>
    /// 色
    /// </summary>
    public Color color;

    /// <summary>
    /// 座標
    /// </summary>
    public Vector2 pos;

    /// <summary>
    /// 向き（進行方向）
    /// </summary>
    public Vector2 direction;

    /// <summary>
    /// 速度
    /// </summary>
    public float velocity;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public Car01(Vector3 size, Color color, Vector2 pos, Vector2 velocity)
    {
        this.pos = pos;
        this.size = size;
        this.color = color;
        this.direction = velocity.normalized;
        this.velocity = velocity.magnitude;
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public Car01(Vector3 size, Color color, Vector2 pos, Vector2 direction, float velocity)
    {
        this.pos = pos;
        this.size = size;
        this.color = color;
        this.direction = direction;
        this.velocity = velocity;
    }
}

/// <summary>
/// 沢山の車を管理するクラス
/// </summary>
public class CarsController01 : MonoBehaviour
{

    /// <summary>
    /// 車をレンダリングするシェーダー
    /// </summary>
    public Shader carShader;

    /// <summary>
    /// 車の移動を行うコンピュートシェーダー
    /// </summary>
    public ComputeShader carComputeShader;

    /// <summary>
    /// 車のマテリアル
    /// </summary>
    Material material;

    /// <summary>
    /// 車のコンピュートバッファ
    /// </summary>
    ComputeBuffer carsBuffer;


    /// <summary>
    /// 破棄
    /// </summary>
    void OnDisable()
    {
        // コンピュートバッファは明示的に破棄しないと怒られます
        carsBuffer.Release();
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
        carComputeShader.SetBuffer(0, "Cars", carsBuffer);
        carComputeShader.SetFloat("DeltaTime", Time.deltaTime);
        carComputeShader.Dispatch(0, carsBuffer.count / 8 + 1, 1, 1);
    }

    /// <summary>
    /// コンピュートバッファの初期化
    /// </summary>
    void InitializeComputeBuffer()
    {
        var count = 6;
        // 車数は1万個
        carsBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Car01)));

        // 配列に初期値を代入する
        /*
        Car[] bullets = new Car[count];
        for (int i = 0; i < count; i++)
        {
            bullets[i] =
                new Car(
                    new Vector3(Random.Range(4, 6), Random.Range(2, 4), Random.Range(3, 6)*2),
                    Color.white,
                    new Vector2(Random.Range(-10.0f, 10.0f) * 10, Random.Range(-10.0f, 10.0f) * 10),
                    new Vector2(Random.Range(-1.0f, 1.0f)*3, Random.Range(-1.0f, 1.0f)*3)
                );
        }*/
        Car01[] bullets = new Car01[] {
                new Car01(
                    new Vector3(2, 1, 4),
                    Color.white,
                    new Vector2(-10, 0),
                    - Vector2.one,
                    0f
                ),
                new Car01(
                    new Vector3(1.5f, 1, 2.5f),
                    new Color(0.1f,0.8f,0.1f),
                    new Vector2(0, 0),
                    - Vector2.one,
                    0f
                ),
                new Car01(
                    new Vector3(2, 0.75f, 4),
                    Color.red,
                    new Vector2(10, 0),
                    - Vector2.one,
                    0f
                ),
                new Car01(
                    new Vector3(2.4f, 2f, 7f),
                    Color.yellow,
                    new Vector2(-10, 10),
                    - Vector2.one,
                    0f
                ),
                new Car01(
                    new Vector3(2.4f, 2f, 7f),
                    Color.cyan,
                    new Vector2(0, 10),
                    - Vector2.one,
                    0f
                ),
                new Car01(
                    new Vector3(2.5f, 2f, 9),
                    new Color(0.1f,0.1f,1),
                    new Vector2(10, 10),
                    - Vector2.one,
                    0f
                ),
        };

        // バッファに適応
        carsBuffer.SetData(bullets);
    }

    /// <summary>
    /// レンダリング
    /// </summary>
    void OnRenderObject()
    {
        // テクスチャ、バッファをマテリアルに設定
        material.SetBuffer("Cars", carsBuffer);
        // レンダリングを開始
        material.SetPass(0);    
        // オブジェクトをレンダリング
        Graphics.DrawProcedural(MeshTopology.Points, carsBuffer.count);
    }
}