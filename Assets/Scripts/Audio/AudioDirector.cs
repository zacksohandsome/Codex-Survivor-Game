using System.Collections.Generic;
using Survivor.Core;
using Survivor.Data;
using UnityEngine;

namespace Survivor.Audio
{
    public sealed class AudioDirector : MonoBehaviour
    {
        private AudioCatalog catalog;
        private AudioSource current,previous,ui;
        private AudioSource[] combat;
        private readonly Dictionary<string,float> lastPlayed=new Dictionary<string,float>();
        private AudioClip theme;
        private float fade=1,musicVolume=0.6f,sfxVolume=0.7f;
        private bool musicPaused;
        public AudioSource CurrentMusic=>current;
        public bool MusicPaused=>musicPaused;
        public bool UiPlaying=>ui!=null && ui.isPlaying;
        public int ActiveCombatSounds { get {int count=0;if(combat!=null)foreach(var source in combat)if(source.isPlaying)count++;return count;} }
        public void Initialize(AudioCatalog data,float music,float sfx)
        {
            catalog=data;current=Source("MusicA",true);previous=Source("MusicB",true);ui=Source("UI",false);
            combat=new AudioSource[8];for(int i=0;i<combat.Length;i++)combat[i]=Source("Combat"+i,false);
            SetVolumes(music,sfx);
        }
        private AudioSource Source(string name,bool loop)
        {
            var root=new GameObject(name);root.transform.SetParent(transform,false);
            var source=root.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;return source;
        }
        public void SetVolumes(float music,float sfx)
        {
            musicVolume=Mathf.Clamp01(music);sfxVolume=Mathf.Clamp01(sfx);
            if(current!=null){current.volume=musicVolume*fade;previous.volume=musicVolume*(1-fade);ui.volume=sfxVolume;foreach(var source in combat)source.volume=sfxVolume;}
        }
        public void ChangeState(GameState state)
        {
            if(state==GameState.Pause || state==GameState.LevelUp)
            {musicPaused=true;current.Pause();previous.Pause();StopCombat();return;}
            musicPaused=false;current.UnPause();previous.UnPause();
            AudioClip desired=catalog==null?null:state==GameState.MainMenu || state==GameState.WeaponSelection?catalog.menuMusic:
                state==GameState.Battle || state==GameState.IntroCountdown?catalog.battleMusic:null;
            if(desired!=theme)
            {
                theme=desired;var old=current;current=previous;previous=old;current.Stop();current.clip=desired;current.volume=0;
                if(desired!=null)current.Play();fade=0;
            }
            if(state==GameState.Victory || state==GameState.Defeat || state==GameState.Result)StopCombat();
        }
        private void Update()
        {
            if(current==null || musicPaused)return;
            fade=Mathf.Min(1,fade+Time.unscaledDeltaTime/0.8f);current.volume=musicVolume*fade;previous.volume=musicVolume*(1-fade);
            if(fade==1 && previous.isPlaying)previous.Stop();
        }
        public bool PlayUi(string key)
        {var clip=catalog==null?null:catalog.Get(key);if(clip==null)return false;ui.clip=clip;ui.Play();return true;}
        public bool PlayCombat(string key,float minimumInterval=0.08f)
        {
            if(musicPaused)return false;var clip=catalog==null?null:catalog.Get(key);if(clip==null)return false;
            float now=Time.unscaledTime;if(lastPlayed.TryGetValue(key,out var last) && now-last<minimumInterval)return false;
            foreach(var source in combat)if(!source.isPlaying){source.clip=clip;source.volume=sfxVolume;source.Play();lastPlayed[key]=now;return true;}
            return false;
        }
        private void StopCombat(){if(combat!=null)foreach(var source in combat)source.Stop();}
    }
}
