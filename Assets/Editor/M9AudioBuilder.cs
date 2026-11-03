using System;
using System.IO;
using Survivor.Data;
using UnityEditor;
using UnityEngine;

namespace Survivor.Editor
{
    public static class M9AudioBuilder
    {
        private const int Rate=22050;
        public static AudioCatalog Create()
        {
            Directory.CreateDirectory("Assets/Audio/Generated");
            Music("Menu",80,8);Music("Battle",120,16);
            string[] ids={"ui","attack","hurt","death","levelup","spawn","elite","boss","coin","stage","boss.defeated","victory","defeat","skill"};
            for(int k=0;k<ids.Length;k++)Sound(ids[k],k);
            AssetDatabase.Refresh();
            foreach(var file in Directory.GetFiles("Assets/Audio/Generated","*.wav"))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(file);var settings=importer.defaultSampleSettings;
                bool music=file.EndsWith("Menu.wav") || file.EndsWith("Battle.wav");
                settings.loadType=music?AudioClipLoadType.CompressedInMemory:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=music?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
                settings.quality=0.8f;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
            }
            const string path="Assets/Data/Flow/AudioCatalog.asset";
            var result=AssetDatabase.LoadAssetAtPath<AudioCatalog>(path);
            if(result==null){result=ScriptableObject.CreateInstance<AudioCatalog>();AssetDatabase.CreateAsset(result,path);}
            result.menuMusic=Clip("Menu");result.battleMusic=Clip("Battle");result.ids=ids;result.clips=new AudioClip[ids.Length];
            for(int i=0;i<ids.Length;i++)result.clips[i]=Clip(ids[i]);EditorUtility.SetDirty(result);AssetDatabase.SaveAssets();return result;
        }
        private static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Generated/"+name+".wav");
        private static double Hertz(int midi)=>440*Math.Pow(2,(midi-69)/12.0);
        private static void Note(float[] samples,double start,double duration,int midi,float gain,bool bright=false)
        {
            double hz=Hertz(midi);int first=(int)(start*Rate),count=(int)(duration*Rate);
            for(int i=0;i<count && first+i<samples.Length;i++)
            {
                double t=(double)i/Rate,p=2*Math.PI*hz*t;
                double envelope=Math.Min(1,t/0.015)*Math.Min(1,(duration-t)/0.06)*Math.Exp(-t/(duration*0.8));
                double wave=Math.Sin(p)+(bright?0.2*Math.Sin(2*p)+0.08*Math.Sin(3*p):0.08*Math.Sin(2*p));
                samples[first+i]+=(float)(wave*envelope*gain);
            }
        }
        private static void Music(string name,int bpm,int bars)
        {
            double beat=60.0/bpm;var samples=new float[(int)(bars*4*beat*Rate)];
            int[] roots={48,44,51,46};int[] phrase={0,7,12,15,12,7,3,7};bool battle=name=="Battle";
            for(int bar=0;bar<bars;bar++)
            {
                int root=roots[bar%4];double start=bar*4*beat;
                Note(samples,start,3.8*beat,root,0.07f);Note(samples,start,3.8*beat,root+7,0.035f);Note(samples,start,3.8*beat,root+15,0.025f);
                for(int step=0;step<8;step++)Note(samples,start+step*beat/2,beat*0.45,root+12+phrase[(step+bar%2*2)%8],battle?0.085f:0.055f,true);
                if(battle)for(int b=0;b<4;b++)
                {
                    Note(samples,start+b*beat,beat*0.22,root-12,0.13f,true);
                    Note(samples,start+(b+0.5)*beat,0.045,96,0.018f,true);
                }
            }
            Write(name,samples);
        }
        private static void Sound(string name,int kind)
        {
            bool outcome=name=="victory" || name=="defeat";double duration=outcome?1.2:name=="boss"?0.9:0.3;
            var samples=new float[(int)(duration*Rate)];
            if(outcome)
            {
                int[] notes=name=="victory"?new[]{60,64,67,72}:new[]{60,56,53,48};
                for(int i=0;i<notes.Length;i++)Note(samples,i*0.18,0.5,notes[i],0.2f,true);
            }
            else
            {
                int midi=name=="coin"?88:name=="hurt"?40:name=="death"?32:name=="boss"?34:name=="elite"?46:60+kind%5*4;
                Note(samples,0,duration*0.8,midi,0.22f,true);
                Note(samples,duration*0.2,duration*0.7,midi+(name=="hurt"?-5:7),0.12f,true);
            }
            Write(name,samples);
        }
        private static void Write(string name,float[] samples)
        {
            string path="Assets/Audio/Generated/"+name+".wav";
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(Rate);writer.Write(Rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);
                foreach(float sample in samples)writer.Write((short)(Mathf.Clamp(sample,-0.95f,0.95f)*short.MaxValue));
            }
        }
    }
}
