using UnityEditor;
using System.Collections.Generic;
using System.Linq;

//================================================================================
// class DefineSymbolSwitcher
//================================================================================
public static class DefineSymbolSwitcher
{
    //================================================================================
    // constant
    //================================================================================
    // 設定対象のBuildTargetGroupテーブル
    private static readonly BuildTargetGroup[] _targetGroupTbl =
    {
    BuildTargetGroup.Standalone,
    BuildTargetGroup.Android,
    BuildTargetGroup.iOS,
    // ...
    };
    // ツールメニューのタイトル
    public const string MENU_TITLE = "Debug/CarDriving/";
    // 現在選択中のプラットフォームグループを取得
    //private static BuildTargetGroup CurrentPlatform = EditorUserBuildSettings.selectedBuildTargetGroup;

    //--------------------------------------------------------------------------------
    // 共通定義
    //--------------------------------------------------------------------------------
    public const string CPU_DRIVE_EMULATE = "CPU_DRIVING";

    //--------------------------------------------------------------------------------
    // DEBUG
    //--------------------------------------------------------------------------------
    [MenuItem(MENU_TITLE + "CPU mode")]
    public static void Add_FW_DEBUG()
    {
        foreach (var group in _targetGroupTbl)
        {
            addSymbol(group, CPU_DRIVE_EMULATE);
        }
    }
    //--------------------------------------------------------------------------------
    // OFF
    //--------------------------------------------------------------------------------
    [MenuItem(MENU_TITLE + "GPU mode")]
    public static void Add_FW_RELEASE()
    {
        foreach (var group in _targetGroupTbl)
        {
            deleteSymbol(group, CPU_DRIVE_EMULATE);
        }
    }

    //--------------------------------------------------------------------------------
    // 指定したシンボルを削除
    //--------------------------------------------------------------------------------
    private static void deleteSymbol(BuildTargetGroup group, string str_target)
    {
        var sds_list = getSymbolList(group);
        if (sds_list.Contains(str_target))
        {
            sds_list.Remove(str_target);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", sds_list.ToArray()));
        }
    }

    //--------------------------------------------------------------------------------
    // 指定したシンボルを追加
    //--------------------------------------------------------------------------------
    private static void addSymbol(BuildTargetGroup group, string str_target)
    {
        var sds_list = getSymbolList(group);
        if (!sds_list.Contains(str_target))
        {
            sds_list.Insert(0, str_target);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", sds_list.ToArray()));
        }
    }

    //--------------------------------------------------------------------------------
    // シンボルをリストで取得
    //--------------------------------------------------------------------------------
    private static List<string> getSymbolList(BuildTargetGroup group)
    {
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').Select(s => s.Trim()).ToList();
    }
}