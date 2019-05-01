using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Diagnostics;

/// <summary>
/// 道(ブランチ)の構造体
/// </summary>
[DebuggerDisplay("Dump()")]
struct Road
{
    /// <summary>
    /// 座標
    /// </summary>
    public Vector2 pos1;
    public Vector2 pos2;

    /// <summary>
    /// 車線数
    /// </summary>
    public Vector2 lanes;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public Road(Vector2 pos1, Vector2 pos2, Vector2 lanes)
    {
        this.pos1 = pos1;
        this.pos2 = pos2;
        this.lanes = lanes;
    }

    public string Dump()
    {
        return string.Format("({0},{1})-({2},{3})[{4}|{5}]", pos1.x, pos1.y, pos2.x, pos2.y, lanes.x, lanes.y);
    }
}

/// <summary>
/// 道の集合を管理するクラス
/// </summary>
[ExecuteAlways]
public class RoadPlane : MonoBehaviour
{
    private const int MAP_SIZE = 256;
    private const int LANE_WIDTH = 8;

    /// <summary>
    /// 道をレンダリングするシェーダー
    /// </summary>
    public Shader roadShader;

    /// <summary>
    /// 道の更新を行うコンピュートシェーダー
    /// </summary>
    public ComputeShader roadsComputeShader;

    /// <summary>
    /// 書き込み可能なテクスチャ
    /// </summary>
    RenderTexture _renderTexture;

    /// <summary>
    /// 道のマテリアル
    /// </summary>
    Material _material;

    /// <summary>
    /// 道のコンピュートバッファ
    /// </summary>
    ComputeBuffer roadsBuffer;

    private bool renderd;


    /// <summary>
    /// 破棄
    /// </summary>
    void OnDisable()
    {
        // コンピュートバッファは明示的に破棄しないと怒られます
        if (roadsBuffer != null)
        {
            roadsBuffer.Release();
            roadsBuffer = null;
        }
        renderd = false;
    }

    /// <summary>
    /// 初期化
    /// </summary>
    void Start()
    {
        InitializeComputeBuffer();
        renderd = false;
    }



    /// <summary>
    /// 更新処理
    /// </summary>
    void Update()
    {
        if (renderd) return;
        roadsComputeShader.SetBuffer(0, "Roads", roadsBuffer);
        roadsComputeShader.SetTexture(0, "Result", _renderTexture);
        roadsComputeShader.SetFloat("laneWidth", LANE_WIDTH);
        roadsComputeShader.Dispatch(0, _renderTexture.width / 8, _renderTexture.height / 8, 1);
        renderd = true;
    }

    /// <summary>
    /// コンピュートバッファの初期化
    /// </summary>
    void InitializeComputeBuffer()
    {
        var count = 3;
        roadsBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Road)));

        // 配列に初期値を代入する
        Road[] roads = new Road[count];
        var step = MAP_SIZE / (count+1);
        var x = step;
        for (int i = 0; i < count; i++)
        {
            roads[i] = new Road(new Vector2(x, 0.1f * MAP_SIZE) , new Vector2(x, 0.9f * MAP_SIZE), new Vector2(3,3));
            x += step;
        }

        // バッファに適用
        roadsBuffer.SetData(roads);

        _renderTexture = new RenderTexture(MAP_SIZE, MAP_SIZE, 0);
        _renderTexture.enableRandomWrite = true;
        _renderTexture.useMipMap = false;
        _renderTexture.Create();

        Renderer ren = GetComponent<Renderer>();
        _material = new Material(roadShader);
        _material = ren.material;
        _material.mainTexture = _renderTexture;
    }


}