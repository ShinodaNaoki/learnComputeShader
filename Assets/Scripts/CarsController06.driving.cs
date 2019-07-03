using UnityEngine;
using Car = ICar<Car03s, Car04d>;
using Car_S = Car03s;
using Car_D = Car04d;

#if CPU_DRIVING
/// <summary>
/// 沢山の車を管理するクラス
/// </summary>
public partial class CarsController06 : MonoBehaviour
{
    private const float MAX_FORCAST_COUNT = 100000f;

    // 2Dベクトル外積
    private float cross2d(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    // 2Dベクトル内積
    private float dot2d(Vector2 a, Vector2 b)
    {
        return a.x * b.x + a.y * b.y;
    }

    // 2Dベクトル回転
    private Vector2 rotate2d(Vector2 v2d, Quaternion mat)
    {
        return mat * v2d;
    }

    // 最も衝突の可能性の高い車のid(index)を返す
    private void FindMostDangerCar(Car[] cars, int id1, int id2, ref float timeMin, ref int idMin)
    {
        if (timeMin <= 0) return; // 時すでに遅し

        var carD1 = cars[id1].Dynamic;
        var carD2 = cars[id2].Dynamic;
        // 別車線は無視
        if (carD1.lane != carD2.lane)
        {
            return;
        }

        // 相対位置ベクトル
        Vector2 diffPos = carD2.pos - carD1.pos;

        // 背後から接近してくるものは回避しない（相手任せ）
        if(dot2d(carD1.direction, diffPos) <= 0) return;

        // 相対速度ベクトル
        float diffVel = (carD1.velocity - carD2.velocity) * 0.28f;
        if (diffVel < 0.001f) return; // 接近していない

        float absPos = diffPos.magnitude;
        float countAssume = absPos / diffVel;
        if (countAssume > 100000f)
        {
            return; // 遠い未来過ぎるので無視
        }

        // 二つの車のサイズを考慮した距離を求める
        // 同一車線なので基本的に両車の長さの半分を足したもの
        var carS1 = cars[id1].Static;
        var carS2 = cars[id2].Static;
        float distance = (carS1.size.z + carS2.size.z) * 0.5f;
        // どちらかが高速で移動しているなら停止距離には余裕を持つ
        distance += Mathf.Max(carD1.velocity, carD2.velocity) * 0.28f;

        float t = Mathf.Max(0, (absPos - distance) / diffVel);
        // このままだと近い将来衝突しそう
        if (t > timeMin)
        {
            return; // もっと近い相手が既にいる
        }        

        // 最小値更新
        timeMin = t;
        idMin = id2;
        if(carD1.direction.y >= 0)
        Debug.Log(
            string.Format("<color='grey'>{0}<->{1}:\n</color> dPos={2:0.0}, dst={3:0.0}, dV={4:0.0} (t={5:0.0})",
                DebugStr(id1, cars[id1]), DebugStr(id2, cars[id2]), absPos, distance, diffVel, t));

    }

    private void DriveAll()
    {
        var cars = factory.GetCars();
        int c = factory.ActiveCars;
        for (int i = 0; i < c; i++)
        {
            Drive(cars, i);
        }
        factory.ApplyData();
    }


    private void Drive(Car[] cars, int id)
    {
        if (cars[id].Static.size.z == 0) return; // 削除済みのデータ

        int count = factory.ActiveCars;
        int idMin = id;
        float timeMin = 1000; // このままだとあと何回で衝突するか
        for (int i = 0; i < count; i++)
        {
            if (i == id) continue;
            if (cars[i].Static.size.z == 0) break; // 削除済みのデータ＝末端に到達
            FindMostDangerCar(cars, id, i, ref timeMin, ref idMin);
        }

        var carD = cars[id].Dynamic;
        var carS = cars[id].Static;

        if (idMin == id)
        { // 衝突の可能性の高い車はない
            if (carD.velocity < carS.idealVelocity)
            {
                var p = carD.velocity;
                carD.velocity = Mathf.Min(carD.velocity + carS.mobility * 1f, carS.idealVelocity);

                if (carD.direction.y >= 0)
                    Debug.Log(string.Format("{0}:\n <color='blue'>vel={1:0.0}</color>",
                    DebugStr(id, cars[id]), carD.velocity - p));

            }
        }
        else
        {
            if (carD.velocity > 0)
            {
                var p = carD.velocity;
                carD.velocity = Mathf.Max(0, carD.velocity - carS.mobility * 2f);

                if (carD.direction.y >= 0)
                {
                    if ((cars[idMin].Dynamic.pos - carD.pos).magnitude < 5)
                    {
                        carD.velocity = 0;
                        Debug.LogErrorFormat(string.Format("{0}<->{1}:\n <color='magenta'>!!CRUSH!!</color>", 
                            DebugStr(id, cars[id]), DebugStr(idMin, cars[idMin])));
                    }
                    else
                    {
                        Debug.Log(string.Format("{0}<->{1}:\n <color='red'>t={2:0.0}, v={3:0.0})</color>",
                            DebugStr(id, cars[id]), DebugStr(idMin, cars[idMin]), timeMin, carD.velocity - p));
                    }
                }
            }
        }

        // それぞれの位置情報に移動ベクトルを加算　(0.28はkm/hをm/sに変換する係数)
        carD.pos += carD.direction * carD.velocity * Time.deltaTime * 0.28f;
        cars[id].Dynamic = carD;

    }

    private string DebugStr(int id, Car car)
    {
        var v = car.Dynamic.direction * car.Dynamic.velocity;
        return string.Format(
            "<color='#{1}'>■</color>{0}@{6}({2:0.0},{3:0.0})⇒({4:0.0},{5:0.0})",
                    id, ColorUtility.ToHtmlStringRGB(car.Static.color),
                    car.Dynamic.pos.x, car.Dynamic.pos.y,
                    v.x, v.y, car.Dynamic.lane
        );
    }
}
#endif
