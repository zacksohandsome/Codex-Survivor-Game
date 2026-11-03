using System.Linq;
using NUnit.Framework;
using Survivor.Data;
using UnityEditor;
using UnityEngine;

namespace Survivor.Tests
{
    public class PresentationAssetTests
    {
        [Test] public void SixCharacterSpritesHaveIndependentSafeAtlasRectsAndAllProfilesAreBound()
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Generated/Characters.png").OfType<Sprite>().ToArray();
            Assert.That(sprites.Length,Is.EqualTo(6));Assert.That(sprites.Select(s=>s.name).Distinct().Count(),Is.EqualTo(6));
            foreach(var sprite in sprites){Assert.That(sprite.rect.width,Is.EqualTo(512));Assert.That(sprite.rect.height,Is.EqualTo(512));Assert.That(sprite.pivot,Is.EqualTo(new Vector2(256,256)));}
            foreach(var name in new[]{"Melee","Ranged","Bomber","Elite","Boss"})
            {
                var definition=AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Data/Battle/"+name+".asset");
                Assert.That(definition.sprite.name,Is.EqualTo(name));Assert.That(definition.color,Is.EqualTo(Color.white));
            }
            var hero=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab").GetComponent<Survivor.Player.PlayerActor>();
            Assert.That(hero.body.sprite.name,Is.EqualTo("Hero"));
        }
        [Test] public void OriginalMusicAndAllFourteenEventsAreImportedWithDistinctOutcomes()
        {
            var flow=AssetDatabase.LoadAssetAtPath<FlowConfig>("Assets/Data/Flow/FlowConfig.asset");var audio=flow.audio;
            Assert.That(audio.menuMusic.length,Is.EqualTo(24).Within(0.1));Assert.That(audio.battleMusic.length,Is.EqualTo(32).Within(0.1));
            Assert.That(audio.ids.Distinct().Count(),Is.EqualTo(14));foreach(var key in audio.ids)Assert.That(audio.Get(key),Is.Not.Null,key);
            Assert.That(audio.Get("victory"),Is.Not.EqualTo(audio.Get("defeat")));Assert.That(audio.Get("missing"),Is.Null);
        }
    }
}
