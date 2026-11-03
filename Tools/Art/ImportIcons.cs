string path="Assets/Art/Generated/Icons.png";
System.IO.Directory.CreateDirectory("Assets/Art/Generated");
System.IO.File.Copy("Documentation/ArtSources/Icons-v1.png",path,true);
UnityEditor.AssetDatabase.ImportAsset(path);
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType=UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode=UnityEditor.SpriteImportMode.Multiple;
importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.filterMode=UnityEngine.FilterMode.Bilinear;
importer.npotScale=UnityEditor.TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
importer.SaveAndReimport();
importer.GetSourceTextureWidthAndHeight(out int width,out int height);
if(width!=1536 || height!=1024) throw new System.InvalidOperationException("Unexpected icon atlas dimensions");
var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories(); factory.Init();
var provider=factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
var edit=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if(edit==null || !edit.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite)) throw new System.InvalidOperationException("Importer cannot safely slice sprites");
string[] names={"bolt","orbit","blast","homing","pulse","bloom","magnet","echo","thorns","trail","storm","sanctuary","stat.health","stat.attack","stat.speed","stat.move","stat.defense","coins","experience","pause"};
int[] xs={48,344,636,928,1220}; int[] tops={8,262,514,766};
var old=provider.GetSpriteRects(); var rects=new UnityEditor.SpriteRect[names.Length];
var pairs=new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
for(int i=0;i<names.Length;i++)
{
 var previous=System.Array.Find(old,r=>r.name==names[i]);
 var id=previous==null?UnityEditor.GUID.Generate():previous.spriteID;
 rects[i]=new UnityEditor.SpriteRect { name=names[i],spriteID=id,rect=new UnityEngine.Rect(xs[i%5],height-tops[i/5]-252,284,252),alignment=UnityEngine.SpriteAlignment.Center,pivot=new UnityEngine.Vector2(0.5f,0.5f) };
 pairs.Add(new UnityEditor.SpriteNameFileIdPair(names[i],id));
}
var nameProvider=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if(nameProvider==null) throw new System.InvalidOperationException("Stable sprite identifiers are unsupported");
provider.SetSpriteRects(rects); nameProvider.SetNameFileIdPairs(pairs); provider.Apply(); importer.SaveAndReimport();
var assets=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
var flow=UnityEditor.AssetDatabase.LoadAssetAtPath<Survivor.Data.FlowConfig>("Assets/Data/Flow/FlowConfig.asset");
flow.iconIds=names; flow.icons=new UnityEngine.Sprite[names.Length];
for(int i=0;i<names.Length;i++) foreach(var asset in assets) if(asset is UnityEngine.Sprite sprite && sprite.name==names[i]) flow.icons[i]=sprite;
UnityEditor.EditorUtility.SetDirty(flow); UnityEditor.AssetDatabase.SaveAssets();
return new { count=flow.icons.Length,sourceWidth=width,sourceHeight=height };

