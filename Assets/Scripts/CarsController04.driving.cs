using UnityEngine;
using Car = ICar<Car03s, Car03d>;
using Car_S = Car03s;
using Car_D = Car03d;

#if CPU_DRIVING
/// <summary>
/// 沢山の車を管理するクラス
/// </summary>
public partial class CarsController04 : MonoBehaviour
{
    private const float MAX_FORCAST_COUNT = 20f;

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

    // 指定方向に最も張り出した頂点の距離から、最大衝突距離を計算する
    private void GetSafeDistance(Car_S carS, Car_D carD, Vector2 diffPosNml, out float distance)
    {
        // Y軸回転の行列を作る
        Quaternion _matrix = Quaternion.FromToRotation(diffPosNml, carD.direction);

        // 車の四方の頂点を評価して、最大値を保存する
        Vector2 vert = new Vector2(carS.size.x, carS.size.z) * 0.5f; // (x,y)
                                                                     // dir は (1,0) が進行方向になっているので、回転後のy座標が diffPosNml との内積に等しい
        distance = rotate2d(vert, _matrix).y;
        vert *= -1; // (-x,-y)
        distance = Mathf.Max(distance, rotate2d(vert, _matrix).y);
        vert.x *= -1; // (x,-y)
        distance = Mathf.Max(distance, rotate2d(vert, _matrix).y);
        vert *= -1; // (-x,y)
        distance = Mathf.Max(distance, rotate2d(vert, _matrix).y);
    }

    // 最も衝突の可能性の高い車のid(index)を返す
    private void FindMostDangerCar(Car[] cars, int id1, int id2, ref float timeMin, ref int idMin)
    {
        if (timeMin <= 0) return; // 時すでに遅し

        var carD1 = cars[id1].Dynamic;
        var carD2 = cars[id2].Dynamic;
        // 相対位置ベクトル
        Vector2 diffPos = carD2.pos - carD1.pos;
        // 相対速度ベクトル
        Vector2 diffVel = carD1.direction * carD1.velocity - carD2.direction * carD2.velocity;

        float dotPosAndVel = dot2d(diffPos, diffVel);
        // 内積が０以下なら衝突の可能性なし！
        if (dotPosAndVel < 0)
        {
            return;
        }

        // 背後から接近してくるものは回避しない（相手任せ）
        if (dot2d(diffPos, carD1.direction) < 0)
        {
            return;
        }

        float absVel = diffVel.magnitude;
        if (absVel < 0.001f) return; // 接近していない

        float countAssume = dotPosAndVel / absVel;
        if (countAssume > MAX_FORCAST_COUNT)
        {
            return; // 遠い未来過ぎるので無視
        }

        // 二つの車のサイズを考慮した距離を求める
        var carS1 = cars[id1].Static;
        var carS2 = cars[id2].Static;
        Vector2 diffPosNml = diffPos.normalized;
        float d1, d2;
        GetSafeDistance(carS1, carD1, diffPosNml, out d1);
        GetSafeDistance(carS2, carD2, -diffPosNml, out d2);
        float distance = d1 + d2;

        // 最接近点での距離が二つの車のサイズを考慮した距離以上なら衝突しない
        float crossPosAndVel = Mathf.Abs(cross2d(diffPos, diffVel.normalized));
        if (crossPosAndVel > distance)
        {
            return;
        }

        // どちらかが高速で移動しているなら停止距離には余裕を持つ
        distance += Mathf.Max(carD2.velocity, carD1.velocity) * 0.28f;

        float t = Mathf.Max(0, (dotPosAndVel - distance) / absVel);
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
            string.Format("<color='grey'>{0}<->{1}:\n</color> t0={2:0.0}, v={3:0.0}, dist={4:0.0}+{5:0.0}, t={6:0.0}({7:0.0})",
                DebugStr(id1, cars[id1]), DebugStr(id2, cars[id2]), countAssume, absVel, d1, d2, t, timeMin));

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
                carD.velocity = Mathf.Max(0, carD.velocity - carS.mobility * 50f / Mathf.Max(0.1f, timeMin));

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
            "<color='#{1}'>■</color>{0}({2:0.0},{3:0.0})⇒({4:0.0},{5:0.0})",
                    id, ColorUtility.ToHtmlStringRGB(car.Static.color),
                    car.Dynamic.pos.x, car.Dynamic.pos.y,
                    v.x, v.y
        );
    }
}
#endif
