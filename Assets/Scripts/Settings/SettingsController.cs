using System;
using Survivor.Meta;
using UnityEngine;

namespace Survivor.Settings
{
    public sealed class SettingsController
    {
        public static event Action<DisplayChoice> EditorPreviewRequested;
        private readonly MetaProgression meta;
        private readonly Action<DisplayChoice> apply;
        public DisplayPreview Display { get; }
        public SettingsController(MetaProgression meta,Action<DisplayChoice> apply=null)
        {
            this.meta=meta; this.apply=apply??ApplyDisplay;
            var saved=new DisplayChoice(meta.Data.displayWidth,meta.Data.displayHeight,meta.Data.fullscreen);
            Display=new DisplayPreview(Supported(saved)?saved:BestSupported(saved.fullscreen));
        }
        public void ApplyInitial() => apply(Display.Applied);
        public static bool Supported(DisplayChoice choice)
        {
            if(!DisplayPreview.ValidSize(choice.width,choice.height)) return false;
            if(Application.isEditor) return true;
            if(!choice.fullscreen) return choice.width<=DisplayMainWidth && choice.height<=DisplayMainHeight;
            foreach(var resolution in Screen.resolutions) if(resolution.width==choice.width && resolution.height==choice.height) return true;
            return false;
        }
        private static int DisplayMainWidth => UnityEngine.Display.main.systemWidth;
        private static int DisplayMainHeight => UnityEngine.Display.main.systemHeight;
        public static DisplayChoice BestSupported(bool fullscreen)
        {
            foreach(var size in new[] { new Vector2Int(2560,1440),new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720) })
            { var choice=new DisplayChoice(size.x,size.y,fullscreen); if(Supported(choice)) return choice; }
            return new DisplayChoice(1280,720,false);
        }
        public bool Begin(DisplayChoice choice)
        { if(!Display.Begin(choice,Supported(choice))) return false; apply(Display.Applied); return true; }
        public bool Confirm()
        {
            bool success=Display.Confirm(choice=>meta.SavePreferences(meta.Data.language,meta.Data.musicVolume,meta.Data.sfxVolume,choice.width,choice.height,choice.fullscreen));
            apply(Display.Applied); return success;
        }
        public void Cancel() { if(Display.Cancel()) apply(Display.Applied); }
        public bool Tick(double seconds) { if(!Display.Tick(seconds)) return false; apply(Display.Applied); return true; }
        private static void ApplyDisplay(DisplayChoice choice)
        {
            if(Application.isEditor) EditorPreviewRequested?.Invoke(choice);
            else Screen.SetResolution(choice.width,choice.height,choice.fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        }
    }
}
