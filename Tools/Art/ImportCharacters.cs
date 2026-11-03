string path="Assets/Art/Generated/Characters.png";
System.IO.File.Copy("Documentation/ArtSources/Characters-v1.png",path,true);
UnityEditor.AssetDatabase.ImportAsset(path);
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType=UnityEditor.TextureImporterType.Sprite; importer.spriteImportMode=UnityEditor.SpriteImportMode.Multiple;
importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=UnityEditor.TextureImporterNPOTScale.None;
importer.maxTextureSize=2048;importer.spritePixelsPerUnit=256;importer.SaveAndReimport();
importer.GetSourceTextureWidthAndHeight(out int w,out int h);
if(w!=1536 || h!=1024)throw new System.InvalidOperationException("Unexpected character atlas dimensions");
var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();
var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
var capability=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if(capability==null || !capability.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite))throw new System.InvalidOperationException("Importer cannot safely slice sprites");
var names=new[]{"Hero","Melee","Ranged","Bomber","Elite","Boss"};var old=provider.GetSpriteRects();var rects=new UnityEditor.SpriteRect[6];
var pairs=new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
for(int i=0;i<6;i++)
{
 var previous=System.Array.Find(old,r=>r.name==names[i]);var id=previous==null?UnityEditor.GUID.Generate():previous.spriteID;
 rects[i]=new UnityEditor.SpriteRect{name=names[i],spriteID=id,rect=new UnityEngine.Rect(i%3*512,(1-i/3)*512,512,512),alignment=UnityEngine.SpriteAlignment.Center,pivot=new UnityEngine.Vector2(0.5f,0.5f)};
 pairs.Add(new UnityEditor.SpriteNameFileIdPair(names[i],id));
}
var nameProvider=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if(nameProvider==null)throw new System.InvalidOperationException("Stable sprite identifiers unsupported");
provider.SetSpriteRects(rects);nameProvider.SetNameFileIdPairs(pairs);provider.Apply();importer.SaveAndReimport();
return new {sprites=6,width=w,height=h};
