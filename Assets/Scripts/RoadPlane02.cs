using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Diagnostics;
using System.Collections.Generic;

/// <summary>
/// 道(ブランチ)の構造体
/// </summary>
[DebuggerDisplay("Dump()")]
struct Road02
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
    public Road02(Vector2 pos1, Vector2 pos2, Vector2 lanes)
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

public class EntryPoint
{
    /// <summary>
    /// 座標
    /// </summary>
    public readonly Vector2 pos;
    public readonly Vector2 dir;

    public EntryPoint(Vector2 pos, Vector2 dir)
    {
        this.pos = pos;
        this.dir = dir;
    }
}

/// <summary>
/// 道の集合を管理するクラス
/// </summary>
public class RoadPlane02 : MonoBehaviour
{
    private const int MAP_SIZE = 512;
    private const int LANE_WIDTH = 10;

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
    private RenderTexture _renderTexture;

    /// <summary>
    /// 道のマテリアル
    /// </summary>
    private Material _material;

    /// <summary>
    /// 道のコンピュートバッファ
    /// </summary>
    private ComputeBuffer roadsBuffer;

    /// <summary>
    /// 道路の侵入可能点
    /// </summary>
    private List<EntryPoint> entryPoints;
    public IList<EntryPoint> EntryPoints { get { return entryPoints; } }

    private bool renderd;


    /// <summary>
    /// 破棄
    /// </summary>
    private void OnDisable()
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
    private void Start()
    {
        Road02[] roads = InitializeComputeBuffer();
        InitializeEntryPoints(roads);
        renderd = false;
    }



    /// <summary>
    /// 更新処理
    /// </summary>
    private void Update()
    {
        if (renderd) return;
        roadsComputeShader.SetBuffer(0, "Roads", roadsBuffer);
        roadsComputeShader.SetTexture(0, "Result", _renderTexture);
        roadsComputeShader.SetFloat("laneWidth", LANE_WIDTH);
        roadsComputeShader.SetInt("length", roadsBuffer.count);
        roadsComputeShader.Dispatch(0, _renderTexture.width / 8, _renderTexture.height / 8, 1);
        renderd = true;
    }

    /// <summary>
    /// コンピュートバッファの初期化
    /// </summary>
    private Road02[] InitializeComputeBuffer()
    {
        var count = 3;
        roadsBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Road02)));

        // 配列に初期値を代入する
        Road02[] roads = new Road02[count];
        var step = MAP_SIZE / (count+1);
        var x = step;
        for (int i = 0; i < count; i++)
        {
            roads[i] = new Road02(new Vector2(x, 0.0f * MAP_SIZE) , new Vector2(x, 1.0f * MAP_SIZE), new Vector2(2,2));
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

        return roads;
    }

    public bool isInside(Vector2 carPos)
    {
        const float areaHalfSize = 110f;
        return (Mathf.Abs(carPos.x) <= areaHalfSize & Mathf.Abs(carPos.y) <= areaHalfSize);
    }

    private Vector2 ToWorldPos(Vector2 local)
    {
        // Planeのmeshサイズは10なので、なんで20なのかよくわからないけど、ぴったり合う
        var scale = 20f * transform.localScale.x / MAP_SIZE;
        // Plane が(0,0)に配置されてる前提だと、座標の起点は -MAP_SIZE/2 にある
        var half = MAP_SIZE / 2;
        return new Vector2((local.x - half) * scale, (local.y - half) * scale);
    }

    private void InitializeEntryPoints(Road02[] roads)
    {
        entryPoints = new List<EntryPoint>();
        foreach(Road02 road in roads)
        {
            var dir = (road.pos2 - road.pos1).normalized;
            var cross = new Vector2(-dir.y, dir.x); // dirと直交するベクトル
            var offset = cross * LANE_WIDTH / 2;
            var step = cross * LANE_WIDTH;
            // 上りレーン
            for(int i = 0; i< road.lanes.y; i++)
            {
                entryPoints.Add(new EntryPoint(ToWorldPos(road.pos1 + offset), dir));
                offset += step;
            }
            cross *= -1;
            dir *= -1;
            offset = cross * LANE_WIDTH / 2;
            step = cross * LANE_WIDTH;
            // 下りレーン
            for (int i = 0; i < road.lanes.x; i++)
            {
                entryPoints.Add(new EntryPoint(ToWorldPos(road.pos2 + offset), dir));
                offset += step;
            }
        }

        foreach(EntryPoint ep in entryPoints)
        {
            UnityEngine.Debug.Log(string.Format("EntryPos:({0},{1})", ep.pos.x, ep.pos.y));
        }
    }
}