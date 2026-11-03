using System;

namespace Survivor.Settings
{
    [Serializable]
    public struct DisplayChoice : IEquatable<DisplayChoice>
    {
        public int width, height;
        public bool fullscreen;
        public DisplayChoice(int width, int height, bool fullscreen) { this.width=width; this.height=height; this.fullscreen=fullscreen; }
        public bool Equals(DisplayChoice other) => width==other.width && height==other.height && fullscreen==other.fullscreen;
        public override bool Equals(object other) => other is DisplayChoice choice && Equals(choice);
        public override int GetHashCode() => width ^ (height<<12) ^ (fullscreen?1:0);
    }
    public sealed class DisplayPreview
    {
        public DisplayChoice Saved { get; private set; }
        public DisplayChoice Applied { get; private set; }
        private DisplayChoice original;
        public bool Pending { get; private set; }
        public double Remaining { get; private set; }
        public DisplayPreview(DisplayChoice current) { Saved=Applied=current; }
        public static bool ValidSize(int width,int height)
            => width==1280 && height==720 || width==1600 && height==900 || width==1920 && height==1080 || width==2560 && height==1440;
        public bool Begin(DisplayChoice choice,bool supported)
        {
            if(Pending || !supported || !ValidSize(choice.width,choice.height)) return false;
            original=Applied; Applied=choice; Pending=true; Remaining=15; return true;
        }
        public bool Confirm(Func<DisplayChoice,bool> save)
        {
            if(!Pending) return false;
            if(!save(Applied)) { Cancel(); return false; }
            Saved=Applied; Pending=false; Remaining=0; return true;
        }
        public bool Cancel()
        {
            if(!Pending) return false;
            Applied=original; Pending=false; Remaining=0; return true;
        }
        public bool Tick(double seconds)
        {
            if(!Pending || seconds<0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return false;
            Remaining=Math.Max(0,Remaining-seconds);
            return Remaining==0 && Cancel();
        }
    }
}
