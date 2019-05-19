using UnityEngine;


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

/**
 * 車種テンプレート
 */
public interface ICarTemplate<S,D> where S: ICarStaticInfo where D: ICarDynamicInfo
{
    // ICarStaticInfo の生成
    S MakeStaticInfo();
    
    // ICarDynamicInfo の生成
    D MakeDynaminInfo(Vector2 pos, Vector2 dir);
}
