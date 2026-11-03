using UnityEngine;

namespace Survivor.Visuals
{
    public sealed class CharacterMotion : MonoBehaviour
    {
        private SpriteRenderer body;
        private Vector3 baseScale;
        private float phase, attack;
        public Vector3 BaseScale=>baseScale;
        public void Initialize(SpriteRenderer renderer,float worldHeight)
        {
            body=renderer; phase=attack=0;
            float size=renderer.sprite==null?1:renderer.sprite.bounds.size.y;
            baseScale=Vector3.one*(worldHeight/Mathf.Max(0.01f,size));
            body.transform.localScale=baseScale; body.transform.localPosition=Vector3.zero; body.transform.localRotation=Quaternion.identity;
        }
        public void Strike(Vector2 direction)
        { attack=0.2f;if(Mathf.Abs(direction.x)>0.01f)body.flipX=direction.x<0; }
        public void Tick(float delta,Vector2 movement)
        {
            bool moving=movement.sqrMagnitude>0.0001f;phase+=delta*(moving?12:3);attack=Mathf.Max(0,attack-delta);
            if(moving && Mathf.Abs(movement.x)>0.001f)body.flipX=movement.x<0;
            float stride=Mathf.Sin(phase), recoil=Mathf.Sin(attack/0.2f*Mathf.PI);
            body.transform.localPosition=new Vector3(0,Mathf.Abs(stride)*(moving?0.055f:0.012f),0);
            body.transform.localScale=Vector3.Scale(baseScale,new Vector3(1+stride*(moving?0.04f:0.012f),1-stride*(moving?0.04f:0.012f),1));
            body.transform.localRotation=Quaternion.Euler(0,0,(moving?stride*3:0)+recoil*(body.flipX?9:-9));
        }
        public void ResetPose()
        { phase=attack=0;if(body==null)return;body.transform.localScale=baseScale;body.transform.localPosition=Vector3.zero;body.transform.localRotation=Quaternion.identity; }
    }
}
