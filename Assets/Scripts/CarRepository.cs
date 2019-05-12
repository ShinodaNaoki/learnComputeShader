using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

public interface IDriveInfo
{
    /// <summary>
    /// 速度
    /// </summary>
    float velocity { get; set; }

    /// <summary>
    /// 運転者の理想速度、最大速度に影響
    /// </summary>
    float idealVelocity { get; set; }

    /// <summary>
    /// 機動性(加減速効率、旋回半径などに影響)
    /// </summary>
    float mobility { get; set; }
}

/// <summary>
/// 車種
/// </summary>
public enum CarType
{
    Hidden,
    Normal,
    Senior,
    Sports,
    Bus,
    TruckM,
    TruckL
}

public interface ICar<T> where T : struct, IDriveInfo
{
    CarType carType { get; }
    CarDrawInfo DrawInfo { get; }
    T DriveInfo { get; set; }
}

public class CarRepository<T> where T: struct, IDriveInfo
{
    class CarTemplate
    {
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

        public CarTemplate(float sx, float sy, float sz, Color col, float mob, float speed)
        {
            size = new Vector3(sx, sy, sz);
            color = col;
            mobility = mob;
            idealVelocity = speed;
        }
    }

    static Dictionary<CarType, CarTemplate> template;

    static CarRepository()
    {
        // 車種ごとの雛形を準備
        template = new Dictionary<CarType, CarTemplate>()
        {
            {CarType.Hidden, new CarTemplate(0, 0, 0, Color.black, 0f, 0) },
            {CarType.Normal, new CarTemplate(2, 1, 4, Color.white, 1.0f, 80) },
            {CarType.Senior, new CarTemplate(1.5f, 1, 2.5f, new Color(0.1f,0.8f,0.1f), 1.0f, 40) },
            {CarType.Sports, new CarTemplate(2, 0.75f, 4, Color.red, 1.0f, 120) },
            {CarType.Bus,    new CarTemplate(2.4f, 2f, 7f, Color.yellow, 0.85f, 60) },
            {CarType.TruckM, new CarTemplate(2.4f, 2f, 7f, Color.cyan, 0.85f, 80) },
            {CarType.TruckL, new CarTemplate(2.5f, 2f, 9, new Color(0.1f,0.1f,1), 0.7f, 100) },
        };
    }
    
    /// <summary>
    /// ランダム車種生成時に使う重みテーブル（車種ごとの出やすさ）
    /// </summary>
    public Dictionary<CarType, float> weightTable { get; set; }

    /// <summary>
    /// 車のコンピュートバッファ
    /// </summary>
    public ComputeBuffer DrawInfoBuffer { get; private set; }
    private CarDrawInfo[] drawInfos;

    /// <summary>
    /// 車のDriveInfoコンピュートバッファ
    /// </summary>
    public ComputeBuffer DriveInfoBuffer { get; private set; }
    private T[] driveInfos;

    private CarImpl<T>[] cars;

    private int nextIndex;

    /// <summary>
    /// 現在の（有効な）車データの数
    /// </summary>
    public int ActiveCars { get { return nextIndex; } }

    /// <summary>
    /// 許容最大車データ数
    /// </summary>
    public int Length { get; private set; }

    public CarRepository(int maxsize)
    {
        Length = maxsize;
        InitArrays();
    }

    private void InitArrays()
    {
        nextIndex = 0;
        drawInfos = new CarDrawInfo[Length];
        driveInfos = new T[Length];
        cars = new CarImpl<T>[Length];
        // リストの初期化
        for (int i = 0; i < Length; i++)
        {
            drawInfos[i] = default(CarDrawInfo);
            driveInfos[i] = default(T);
            cars[i] = new CarImpl<T>(this, i, default(CarType));
        }
    }

    public void AssignBuffers()
    {
        ReleaseBuffers(); // 未開放なら解放

        DrawInfoBuffer = new ComputeBuffer(Length, Marshal.SizeOf(typeof(CarDrawInfo)));
        DriveInfoBuffer = new ComputeBuffer(Length, Marshal.SizeOf(typeof(T)));
    }

    /// <summary>
    /// 車種を指定して生成
    /// </summary>
    /// <param name="type"></param>
    /// <param name="pos"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    public ICar<T> Create(CarType type, Vector2 pos, Vector2 dir)
    {
        if(nextIndex >= drawInfos.Length)
        {
            throw new IndexOutOfRangeException("Cannot create because the buffer is full. " + drawInfos.Length);
        }
        CarTemplate temp = template[type];
        cars[nextIndex].carType = type;

        // CarDrawInfo の初期化
        CarDrawInfo draw = new CarDrawInfo();
        draw.direction = dir.normalized;
        draw.pos = pos;
        draw.color = temp.color;
        draw.size = temp.size;
        drawInfos[nextIndex] = draw;
        // CarDriveInfo の初期化
        T drive = new T();
        drive.velocity = drive.idealVelocity = temp.idealVelocity;
        drive.mobility = temp.mobility;
        driveInfos[nextIndex] = drive;

        CarImpl<T> car = new CarImpl<T>(this, nextIndex++, type);

        return car;
    }

    private CarType GetRandomType()
    {
        if(weightTable == null)
        {
            var length = Enum.GetValues(typeof(CarType)).Length;
            return (CarType)UnityEngine.Random.Range(1, length);
        }
        var rand = UnityEngine.Random.value;
        var accum = 0f;
        foreach(var pair in weightTable)
        {
            accum += pair.Value;
            if (rand <= accum) return pair.Key;
        }
        return CarType.Normal;
    }

    /// <summary>
    /// ランダムな車種で生成（重みテーブルがあれば使う）
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    public ICar<T> CreateRandomType(Vector2 pos, Vector2 dir)
    {
        CarType ct = GetRandomType();
        return Create(ct, pos, dir);
    }

    private void FetchData()
    {
        DrawInfoBuffer.GetData(drawInfos);
        DriveInfoBuffer.GetData(driveInfos);
    }

    public ICar<T>[] GetCars() {
        FetchData();
        return cars;
    }

    public void ApplyData()
    {
        DrawInfoBuffer.SetData(drawInfos);
        DriveInfoBuffer.SetData(driveInfos);
    }

    /// <summary>
    /// 車を安全に削除する
    /// </summary>
    /// <param name="index"></param>
    public void Remove(int index)
    {
        if (index == -1) return;

        nextIndex--;
        if (nextIndex >= 0)
        { 
            // 末尾のデータと入れ替えて穴埋めする
            drawInfos[index] = drawInfos[nextIndex];
            driveInfos[index] = driveInfos[nextIndex];
            cars[index] = cars[nextIndex];
        }
        // 消した分をゼロ埋めする
        drawInfos[nextIndex] = default(CarDrawInfo);
        driveInfos[nextIndex] = default(T);
        cars[nextIndex].carType = default(CarType);
    }

    public void ReleaseBuffers()
    {
        if(DrawInfoBuffer != null)
        {
            DrawInfoBuffer.Release();
            DrawInfoBuffer = null;
        }
        if(DriveInfoBuffer != null)
        {
            DriveInfoBuffer.Release();
            DriveInfoBuffer = null;
        }
    }

    class CarImpl<T> : ICar<T> where T : struct, IDriveInfo
    {
        internal int index { get; private set; }
        public CarType carType { get; internal set; }

        private CarRepository<T> repository;

        public CarDrawInfo DrawInfo { get { return repository.drawInfos[index]; } }

        public T DriveInfo {
            get { return repository.driveInfos[index]; }
            set { repository.driveInfos[index] = value; }
        }

        public CarImpl(CarRepository<T> repository, int index, CarType cartype)
        {
            this.repository = repository;
            this.index = index;
            this.carType = cartype;
        }
    }
}