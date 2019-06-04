using UnityEngine;
using Car = ICar<Car03s, Car03d>;
using Car_S = Car03s;
using Car_D = Car03d;

/// <summary>
/// 沢山の車を管理するクラス
/// </summary>
public partial class CarsController04 : MonoBehaviour
{
    private const float MAX_FORCAST_COUNT = 25f;

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
        float sina = cross2d(carD.direction, diffPosNml);
        float cosa = dot2d(carD.direction, diffPosNml);
        Quaternion _matrix = new Quaternion(cosa, -sina, sina, cosa);

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
        Vector2 diffVel = carD2.direction * carD2.velocity - carD1.direction * carD1.velocity;

        float dotPosAndVel = dot2d(diffPos, diffVel);
        // 内積が０以下なら衝突の可能性なし！
        if (dotPosAndVel < 0) return;

        float absVel = diffVel.magnitude;
        if (absVel == 0) return; // 接近していない

        float countAssume = dotPosAndVel / absVel;
        if (countAssume > MAX_FORCAST_COUNT) return; // 遠い未来過ぎるので無視

        // 二つの車のサイズを考慮した距離を求める
        var carS1 = cars[id1].Static;
        var carS2 = cars[id2].Static;
        Vector2 diffPosNml = diffPos.normalized;
        float d1, d2;
        GetSafeDistance(carS1, carD1, diffPosNml, out d1);
        GetSafeDistance(carS2, carD2, -diffPosNml, out d2);
        float distance = d1 + d2;

        // 最接近点での距離が二つの車のサイズを考慮した距離以上なら衝突しない
        float crossPosAndVel = Mathf.Abs(cross2d(diffPos, diffVel));
        if (crossPosAndVel > distance) return;


        float t = (dotPosAndVel - distance * 2) / absVel;
        // このままだと近い将来衝突しそう
        Debug.Log(string.Format("#{0}{1}<->{2}{3}:\n t0={4}, vel={5}, dist={6}+{7}, t={8}",
            id1, carD1, id2, carD2, countAssume, absVel, d1,d2, t));
        if (t > timeMin) return; // もっと近い相手が既にいる

        // 最小値更新
        timeMin = t;
        idMin = id2;
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
        Debug.Assert(cars[id].Static.size.z > 0, "Empty!");
        int count = cars.Length;
        int idMin = id;
        float timeMin = 1000; // このままだとあと何回で衝突するか
        for (int i = 0; i < count; i++)
        {
            if (i == id) continue;
            Debug.Assert(cars[i].Static.size.z > 0, "Empty!");
            FindMostDangerCar(cars, id, i, ref timeMin, ref idMin);
        }

        var carD = cars[id].Dynamic;
        var carS = cars[id].Static;

        if (idMin == id)
        { // 衝突の可能性の高い車はない
            if (carD.velocity < carS.idealVelocity)
            {
                carD.velocity = Mathf.Min(carD.velocity + carS.mobility * 5f, carS.idealVelocity);
            }
        }
        else
        {
            if (carD.velocity > 0)
            {
                carD.velocity = Mathf.Max(0, carD.velocity - Mathf.Clamp01(10 / timeMin) * carS.mobility * 5f);
            }
        }

        // それぞれの位置情報に移動ベクトルを加算　(0.28はkm/hをm/sに変換する係数)
        //carD.pos += carD.direction * carD.velocity * Time.deltaTime * 0.28f;
        cars[id].Dynamic = carD;

    }
}