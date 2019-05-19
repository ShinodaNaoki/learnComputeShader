using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class AnalyseParticleStruct : ScriptableObject
{
    [Multiline(20), Header("ParticleSystem.Particle（Resetで更新）"), Tooltip("structのshader用記述")]
    public string dumpText;


    private void Awake()
    {
        dumpText = DumpStruct<ParticleSystem.Particle>();
    }

    private void Start()
    {
        dumpText = DumpStruct<ParticleSystem.Particle>();
    }

    private void OnEnabled()
    {
        dumpText = DumpStruct<ParticleSystem.Particle>();
    }

    private void Reset()
    {
        dumpText = DumpStruct<ParticleSystem.Particle>();
    }

    static Dictionary<Type, string> _typeMap = new Dictionary<Type, string>()
    {
        {typeof(Vector2), "float2" },
        {typeof(Vector3), "float3" },
        {typeof(Vector4), "float4" },
        {typeof(UInt32), "uint" },
        {typeof(Single), "float" },
        {typeof(Color32), "uint" },
    };

    static string DumpStruct<T>() where T:struct
    {
        Type t = typeof(T);
        var sb = new StringBuilder("struct ");
        sb.Append(t.Name);
        sb.Append(" {");
        SortedList<int, FieldInfo> sortedList = new SortedList<int, FieldInfo>();
        foreach(var fi in t.GetFields(BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic))
        {
            int offset = Marshal.OffsetOf(typeof(T), fi.Name).ToInt32();
            sortedList.Add(offset, fi);
        }
        foreach(var fi in sortedList.Values)
        {
            sb.AppendLine();            
            sb.AppendFormat("\t{0} {1};", ToTypeName(fi), fi.Name);
        }
        sb.AppendLine();
        sb.Append("}");
        return sb.ToString();
    }

    static string ToTypeName(FieldInfo fi)
    {
        string ret;
        if (_typeMap.TryGetValue(fi.FieldType, out ret))
        {
            return ret;
        }
        return fi.FieldType.Name;
    }
}
