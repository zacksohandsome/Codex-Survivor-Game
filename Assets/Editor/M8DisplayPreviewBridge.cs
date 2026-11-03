using System;
using System.Reflection;
using Survivor.Settings;
using UnityEditor;

namespace Survivor.Editor
{
    [InitializeOnLoad]
    public static class M8DisplayPreviewBridge
    {
        private static int previousIndex=-1;
        static M8DisplayPreviewBridge()
        {
            SettingsController.EditorPreviewRequested+=Apply;
            EditorApplication.playModeStateChanged+=state=>{ if(state==PlayModeStateChange.EnteredEditMode && previousIndex>=0) { var window=GameView(); SelectedIndex(window).SetValue(window,previousIndex); previousIndex=-1; } };
        }
        private static EditorWindow GameView() => EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        private static PropertyInfo SelectedIndex(EditorWindow window) => window.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        private static void Apply(DisplayChoice choice)
        {
            var assembly=typeof(EditorWindow).Assembly; var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance",BindingFlags.Static|BindingFlags.Public).GetValue(null);
            var groupEnum=assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group=sizesType.GetMethod("GetGroup").Invoke(singleton,new[]{Enum.Parse(groupEnum,"Standalone")});
            var groupType=group.GetType(); int total=(int)groupType.GetMethod("GetTotalCount").Invoke(group,null), index=-1;
            for(int i=0;i<total;i++)
            {
                var size=groupType.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
                if((int)size.GetType().GetProperty("width").GetValue(size)==choice.width && (int)size.GetType().GetProperty("height").GetValue(size)==choice.height) { index=i;break; }
            }
            if(index<0)
            {
                var sizeType=assembly.GetType("UnityEditor.GameViewSize"); var modeType=assembly.GetType("UnityEditor.GameViewSizeType");
                var constructor=sizeType.GetConstructor(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new[]{modeType,typeof(int),typeof(int),typeof(string)},null);
                var size=constructor.Invoke(new[]{Enum.Parse(modeType,"FixedResolution"),(object)choice.width,choice.height,"Survivor "+choice.width+"x"+choice.height});
                groupType.GetMethod("AddCustomSize").Invoke(group,new[]{size}); index=total;
            }
            var window=GameView(); var selected=SelectedIndex(window); if(previousIndex<0) previousIndex=(int)selected.GetValue(window);
            selected.SetValue(window,index); window.Repaint();
        }
    }
}
