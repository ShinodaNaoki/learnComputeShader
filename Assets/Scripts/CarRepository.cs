using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

public interface ICar<S, D> where S : struct, ICarStaticInfo where D : struct, ICarDynamicInfo
{
    CarType carType { get; }
    S Static { get; }
    D Dynamic { get; set; }
}

public partial class CarRepository<S,D> where S : struct, ICarStaticInfo where D: struct, ICarDynamicInfo
{

    private Dictionary<CarType, ICarTemplate<S,D>> templates;
    
    /// <summary>
    /// ランダム車種生成時に使う重みテーブル（車種ごとの出やすさ）
    /// </summary>
    public Dictionary<CarType, float> weightTable { get; set; }

    /// <summary>
    /// 車のコンピュートバッファ
    /// </summary>
    public ComputeBuffer StaticInfoBuffer { get; private set; }
    private S[] staticInfos;

    /// <summary>
    /// 車のDriveInfoコンピュートバッファ
    /// </summary>
    public ComputeBuffer DynamicInfoBuffer { get; private set; }
    private D[] dynamicInfos;

    private CarImpl<S,D>[] cars;

    private int nextIndex;

    /// <summary>
    /// 現在の（有効な）車データの数
    /// </summary>
    public int ActiveCars { get { return nextIndex; } }

    /// <summary>
    /// 許容最大車データ数
    /// </summary>
    public int Length { get; private set; }

    public CarRepository(int maxsize, Dictionary<CarType, ICarTemplate<S,D>> templateDict)
    {
        templates = templateDict;
        Length = maxsize;
        InitArrays();
    }

    private void InitArrays()
    {
        nextIndex = 0;
        staticInfos = new S[Length];
        dynamicInfos = new D[Length];
        cars = new CarImpl<S,D>[Length];
        // リストの初期化
        for (int i = 0; i < Length; i++)
        {
            staticInfos[i] = default(S);
            dynamicInfos[i] = default(D);
            cars[i] = new CarImpl<S,D>(this, i, default(CarType));
        }
    }

    public void AssignBuffers()
    {
        ReleaseBuffers(); // 未開放なら解放

        StaticInfoBuffer = new ComputeBuffer(Length, Marshal.SizeOf(typeof(S)));
        DynamicInfoBuffer = new ComputeBuffer(Length, Marshal.SizeOf(typeof(D)));
    }

    /// <summary>
    /// 車種を指定して生成
    /// </summary>
    /// <param name="type"></param>
    /// <param name="pos"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    public ICar<S,D> Create(CarType type, Vector2 pos, Vector2 dir)
    {
        if(nextIndex >= staticInfos.Length)
        {
            throw new IndexOutOfRangeException("Cannot create because the buffer is full. " + staticInfos.Length);
        }
        var temp = templates[type];
        cars[nextIndex].carType = type;

        staticInfos[nextIndex] = temp.MakeStaticInfo();
        dynamicInfos[nextIndex] = temp.MakeDynaminInfo(pos, dir);

        CarImpl<S,D> car = new CarImpl<S,D>(this, nextIndex++, type);

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
    public ICar<S,D> CreateRandomType(Vector2 pos, Vector2 dir)
    {
        CarType ct = GetRandomType();
        return Create(ct, pos, dir);
    }

    private void FetchData()
    {
        StaticInfoBuffer.GetData(staticInfos);
        DynamicInfoBuffer.GetData(dynamicInfos);
    }

    public ICar<S,D>[] GetCars() {
        FetchData();
        return cars;
    }

    public void ApplyData()
    {
        StaticInfoBuffer.SetData(staticInfos);
        DynamicInfoBuffer.SetData(dynamicInfos);
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
            staticInfos[index] = staticInfos[nextIndex];
            dynamicInfos[index] = dynamicInfos[nextIndex];
            cars[index] = cars[nextIndex];
        }
        // 消した分をゼロ埋めする
        staticInfos[nextIndex] = default(S);
        dynamicInfos[nextIndex] = default(D);
        cars[nextIndex].carType = default(CarType);
    }

    public void ReleaseBuffers()
    {
        if(StaticInfoBuffer != null)
        {
            StaticInfoBuffer.Release();
            StaticInfoBuffer = null;
        }
        if(DynamicInfoBuffer != null)
        {
            DynamicInfoBuffer.Release();
            DynamicInfoBuffer = null;
        }
    }

    class CarImpl<S,D> : ICar<S,D> where S : struct, ICarStaticInfo where D : struct, ICarDynamicInfo
    {
        internal int index { get; private set; }
        public CarType carType { get; internal set; }

        private CarRepository<S,D> repository;

        public S Static { get { return repository.staticInfos[index]; } }

        public D Dynamic {
            get { return repository.dynamicInfos[index]; }
            set { repository.dynamicInfos[index] = value; }
        }

        public CarImpl(CarRepository<S,D> repository, int index, CarType cartype)
        {
            this.repository = repository;
            this.index = index;
            this.carType = cartype;
        }
    }
}