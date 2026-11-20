using UnityEngine;

namespace FlameBlame.Npc
{
    /// Positions the parts of a toy-proportioned NPC. Unit-space layout is normalised so the helmet top lands exactly on `height`.
    public sealed class ToyNpcShape : MonoBehaviour
    {
        [SerializeField] Transform shape;
        [SerializeField] Transform torso, head, helmet, brim, badge;
        [SerializeField] Transform[] legs, arms, gloves, eyes, pupils, stripes;
        const float LegH = .20f, TorsoW = .46f, TorsoH = .46f;

        public void Bind(Transform root, Transform torsoT, Transform headT, Transform helmetT, Transform brimT, Transform badgeT,
            Transform[] legT, Transform[] armT, Transform[] gloveT, Transform[] eyeT, Transform[] pupilT, Transform[] stripeT)
        {
            shape = root; torso = torsoT; head = headT; helmet = helmetT; brim = brimT; badge = badgeT;
            legs = legT; arms = armT; gloves = gloveT; eyes = eyeT; pupils = pupilT; stripes = stripeT;
        }

        /// headRatio = face width / torso width (the helmet is larger on top).
        public void Apply(float height, float headRatio)
        {
            float d = headRatio * TorsoW;
            float torsoY = LegH + TorsoH * .5f - .02f;
            torso.localPosition = new Vector3(0, torsoY, 0); torso.localScale = new Vector3(TorsoW, TorsoH, TorsoW * .85f);
            float headY = LegH + TorsoH - .02f + d * .30f;
            head.localPosition = new Vector3(0, headY, 0); head.localScale = Vector3.one * d;
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1f : 1f;
                legs[i].localPosition = new Vector3(s * .11f, LegH * .5f, 0); legs[i].localScale = new Vector3(.15f, LegH * .5f, .17f);
                float eye = d * .42f;
                eyes[i].localPosition = new Vector3(s * d * .21f, headY + d * .08f, d * .40f); eyes[i].localScale = Vector3.one * eye;
                pupils[i].localPosition = eyes[i].localPosition + new Vector3(s * -d * .02f, -d * .02f, eye * .46f); pupils[i].localScale = Vector3.one * eye * .5f;
                arms[i].localPosition = new Vector3(s * (TorsoW * .52f), torsoY - .02f, .02f);
                arms[i].localRotation = Quaternion.Euler(0, 0, s * 18f); arms[i].localScale = new Vector3(.13f, .17f, .13f);
                gloves[i].localPosition = arms[i].localPosition + arms[i].localRotation * new Vector3(0, -.19f, 0); gloves[i].localScale = Vector3.one * .15f;
            }
            helmet.localPosition = new Vector3(0, headY + d * .27f, -d * .02f); helmet.localScale = new Vector3(d * 1.15f, d * .78f, d * 1.15f);
            brim.localPosition = new Vector3(0, headY + d * .17f, d * .04f); brim.localScale = new Vector3(d * 1.5f, .012f, d * 1.5f);
            badge.localPosition = new Vector3(0, headY + d * .36f, d * .54f); badge.localScale = new Vector3(d * .38f, d * .26f, .02f);
            for (int i = 0; i < stripes.Length; i++)
            {
                stripes[i].localPosition = new Vector3(0, LegH + TorsoH * (.30f + i * .17f), 0);
                stripes[i].localScale = new Vector3(TorsoW * .985f, .012f, TorsoW * .85f * .985f);
            }
            float top = helmet.localPosition.y + d * .78f * .5f;
            shape.localScale = Vector3.one * (height / top);
        }
    }
}
