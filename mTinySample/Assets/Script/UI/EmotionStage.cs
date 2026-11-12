using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// mTiny's emotions and special-tile stories, played on a card that sits
// above the whole game (an overlay canvas), so the 3D robot or the board
// can never cover it. Every scene is built from small pictures and moved
// with keyframes in code, the same way the approved mockups were made.
public class EmotionStage : MonoBehaviour
{
    [Serializable]
    public class FaceSet
    {
        public string key;
        public Sprite[] frames;
    }

    [Header("Parts")]
    public RectTransform card;
    public Image cardBack;
    public RectTransform content;
    public Image titleChip;
    public TMP_Text title;
    public TMP_FontAsset font;

    [Header("Art")]
    [Tooltip("Face sheets: happy, sad, angry, surprise, dizzy, sleepy, playful.")]
    public FaceSet[] faces;
    [Tooltip("Hats, buildings, props and effects, found by name.")]
    public Sprite[] art;

    [Header("Timing")]
    public float emotionSeconds = 3f;
    public float scenarioSeconds = 5f;

    public bool IsPlaying { get; private set; }

    private const float W = 600f, H = 400f;
    private static readonly Color[] Confetti =
    {
        Hex("3DBE6B"), Hex("4A9DFF"), Hex("FFC93C"), Hex("FF7EB6"), Hex("9B6BFF"), Hex("FF8A5B")
    };

    private readonly List<Actor> actors = new List<Actor>();
    private readonly List<Action<float>> updaters = new List<Action<float>>();
    private Dictionary<string, Sprite> artByName;
    private float D;
    private int order;

    // =========================================================
    // PUBLIC
    // =========================================================

    public IEnumerator Play(EmotionType emotion)
    {
        switch (emotion)
        {
            case EmotionType.Emergency: return Run(true, "Hospital", BuildHospital);
            case EmotionType.Polices: return Run(true, "Police", BuildPolice);
            case EmotionType.Crossing: return Run(true, "Crossing", BuildCrossing);
            case EmotionType.Theft: return Run(true, "Theft", BuildTheft);
            case EmotionType.Sad: return Run(false, "Sad", BuildSad);
            case EmotionType.Angry: return Run(false, "Angry", BuildAngry);
            case EmotionType.Surprise: return Run(false, "Surprised", BuildSurprise);
            case EmotionType.Dizzy: return Run(false, "Dizzy", BuildDizzy);
            case EmotionType.Sleepy: return Run(false, "Sleepy", BuildSleepy);
            case EmotionType.Playful: return Run(false, "Playful", BuildPlayful);
            default: return Run(false, "Happy", BuildHappy);
        }
    }

    public IEnumerator PlayBumped()
    {
        return Run(true, "Bumped", BuildBumped);
    }

    public void Hide()
    {
        IsPlaying = false;
        Clear();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        IsPlaying = false;
        Clear();
    }

    // =========================================================
    // PLAYBACK
    // =========================================================

    // Editor previews: build a clip and freeze it at time t (seconds)
    public void ShowFrame(EmotionType? emotion, float t)
    {
        IEnumerator run = emotion.HasValue ? Play(emotion.Value) : PlayBumped();
        run.MoveNext();
        Evaluate(Mathf.Min(t, D));
        IsPlaying = false;
        if (card != null) card.localScale = Vector3.one;
    }

    private void Prepare(bool scenario, string label, Action build)
    {
        D = scenario ? scenarioSeconds : emotionSeconds;

        gameObject.SetActive(true);
        Clear();

        if (title != null)
        {
            title.text = label;
            title.color = Ink.linear;
        }
        if (cardBack != null) cardBack.color = Hex("EAF6FF");

        build();
        SortByZ();
    }

    private IEnumerator Run(bool scenario, string label, Action build)
    {
        Prepare(scenario, label, build);

        IsPlaying = true;
        float t = 0f;
        Evaluate(0f);

        // Card springs in
        float pop = 0f;
        while (t < D && IsPlaying)
        {
            float dt = Time.deltaTime;
            t += dt;
            pop += dt;
            if (card != null) card.localScale = Vector3.one * Mathf.LerpUnclamped(0.82f, 1f, PopIn.EaseOutBack(Mathf.Clamp01(pop / 0.28f)));
            Evaluate(Mathf.Min(t, D));
            yield return null;
        }

        // Card shrinks away
        float out_ = 0f;
        while (out_ < 0.18f && IsPlaying)
        {
            out_ += Time.deltaTime;
            if (card != null) card.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, out_ / 0.18f);
            yield return null;
        }

        IsPlaying = false;
        Clear();
        gameObject.SetActive(false);
    }

    private void Evaluate(float t)
    {
        for (int i = 0; i < actors.Count; i++) actors[i].Apply(t);
        for (int i = 0; i < updaters.Count; i++) updaters[i](t);
    }

    private void Clear()
    {
        actors.Clear();
        updaters.Clear();
        order = 0;
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject old = content.GetChild(i).gameObject;
            old.transform.SetParent(null, false);
            if (Application.isPlaying) Destroy(old);
            else DestroyImmediate(old);
        }
        if (card != null) card.localScale = Vector3.one;
    }

    private void SortByZ()
    {
        var list = new List<RectTransform>();
        foreach (RectTransform c in content) list.Add(c);
        list.Sort((a, b) =>
        {
            ZTag za = a.GetComponent<ZTag>(), zb = b.GetComponent<ZTag>();
            int c = (za ? za.z : 0).CompareTo(zb ? zb.z : 0);
            return c != 0 ? c : (za ? za.order : 0).CompareTo(zb ? zb.order : 0);
        });
        for (int i = 0; i < list.Count; i++) list[i].SetSiblingIndex(i);
    }

    // =========================================================
    // ACTORS AND KEYFRAMES
    // =========================================================

    public enum Ease { InOut, Out, In, Linear, Body }

    public class Key
    {
        public float t;
        public float? x, y, r, sx, sy, o;
        public Ease e = Ease.InOut;
    }

    private struct State
    {
        public float x, y, r, sx, sy, o;
    }

    private class ZTag : MonoBehaviour
    {
        public int z, order;
    }

    private class Actor
    {
        public RectTransform rt;
        public CanvasGroup group;
        public Image image;
        public Vector2 basePos;
        private float[] times;
        private State[] states;
        private Ease[] eases;

        public void SetKeys(List<Key> keys, float total)
        {
            keys.Sort((a, b) => a.t.CompareTo(b.t));
            State s = new State { sx = 1, sy = 1, o = 1 };
            var ts = new List<float>();
            var ss = new List<State>();
            var es = new List<Ease>();
            foreach (Key k in keys)
            {
                if (k.x.HasValue) s.x = k.x.Value;
                if (k.y.HasValue) s.y = k.y.Value;
                if (k.r.HasValue) s.r = k.r.Value;
                if (k.sx.HasValue) s.sx = k.sx.Value;
                if (k.sy.HasValue) s.sy = k.sy.Value;
                if (k.o.HasValue) s.o = k.o.Value;
                ts.Add(Mathf.Clamp(k.t, 0f, total));
                ss.Add(s);
                es.Add(k.e);
            }
            times = ts.ToArray();
            states = ss.ToArray();
            eases = es.ToArray();
        }

        public void Apply(float t)
        {
            if (times == null || times.Length == 0) return;
            State s;
            if (t <= times[0]) s = states[0];
            else if (t >= times[times.Length - 1]) s = states[states.Length - 1];
            else
            {
                int i = 0;
                while (i < times.Length - 2 && t >= times[i + 1]) i++;
                float span = times[i + 1] - times[i];
                float u = span <= 0f ? 1f : Mathf.Clamp01((t - times[i]) / span);
                u = EaseValue(eases[i], u);
                State a = states[i], b = states[i + 1];
                s = new State
                {
                    x = Mathf.LerpUnclamped(a.x, b.x, u),
                    y = Mathf.LerpUnclamped(a.y, b.y, u),
                    r = Mathf.LerpUnclamped(a.r, b.r, u),
                    sx = Mathf.LerpUnclamped(a.sx, b.sx, u),
                    sy = Mathf.LerpUnclamped(a.sy, b.sy, u),
                    o = Mathf.LerpUnclamped(a.o, b.o, u)
                };
            }
            rt.anchoredPosition = basePos + new Vector2(s.x, -s.y);
            rt.localEulerAngles = new Vector3(0f, 0f, -s.r);
            rt.localScale = new Vector3(s.sx, s.sy, 1f);
            if (group != null) group.alpha = Mathf.Clamp01(s.o);
        }
    }

    private static float EaseValue(Ease e, float u)
    {
        switch (e)
        {
            case Ease.Linear: return u;
            case Ease.In: return Bezier(0.42f, 0f, 1f, 1f, u);
            case Ease.Out: return Bezier(0f, 0f, 0.58f, 1f, u);
            case Ease.Body: return Bezier(0.3f, 0.7f, 0.4f, 1f, u);
            default: return Bezier(0.42f, 0f, 0.58f, 1f, u);
        }
    }

    // CSS-style cubic-bezier timing
    private static float Bezier(float x1, float y1, float x2, float y2, float x)
    {
        float lo = 0f, hi = 1f, t = x;
        for (int i = 0; i < 18; i++)
        {
            float cx = 3 * x1 * t * (1 - t) * (1 - t) + 3 * x2 * t * t * (1 - t) + t * t * t;
            if (cx < x) lo = t; else hi = t;
            t = (lo + hi) * 0.5f;
        }
        return 3 * y1 * t * (1 - t) * (1 - t) + 3 * y2 * t * t * (1 - t) + t * t * t;
    }

    private static Key K(float t, float? x = null, float? y = null, float? r = null, float? s = null,
        float? sx = null, float? sy = null, float? o = null, Ease e = Ease.InOut)
    {
        return new Key { t = t, x = x, y = y, r = r, sx = s ?? sx, sy = s ?? sy, o = o, e = e };
    }

    // A CSS @keyframes row: every value is given, missing ones are the identity
    private static Key C(float pct, float dur, float delay, float x = 0, float y = 0, float r = 0,
        float sx = 1, float sy = 1, float? o = null, Ease e = Ease.InOut)
    {
        return new Key { t = delay + pct / 100f * dur, x = x, y = y, r = r, sx = sx, sy = sy, o = o, e = e };
    }

    private void Anim(Actor a, params Key[] keys)
    {
        a.SetKeys(new List<Key>(keys), D);
        a.Apply(0f);
    }

    private void Anim(Actor a, List<Key> keys)
    {
        a.SetKeys(keys, D);
        a.Apply(0f);
    }

    // ---------------------------------------------------------
    // Building blocks. Positions use the mockup's 600 x 400 card,
    // measured from the top-left, with y going down.
    // ---------------------------------------------------------

    private Sprite Art(string name)
    {
        if (artByName == null)
        {
            artByName = new Dictionary<string, Sprite>();
            if (art != null) foreach (Sprite s in art) if (s != null) artByName[s.name] = s;
        }
        Sprite found;
        return artByName.TryGetValue(name, out found) ? found : null;
    }

    private Sprite Face(string key, int index)
    {
        if (faces != null)
        {
            foreach (FaceSet f in faces)
            {
                if (f != null && f.key == key && f.frames != null && f.frames.Length > 0)
                    return f.frames[Mathf.Clamp(index, 0, f.frames.Length - 1)];
            }
        }
        return null;
    }

    private Actor Make(string name, RectTransform parent, float l, float t, float w, float h, Vector2 origin, int z)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent != null ? parent : content, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(origin.x, 1f - origin.y);
        rt.sizeDelta = new Vector2(w, h);
        var a = new Actor { rt = rt, group = go.GetComponent<CanvasGroup>() };
        a.group.blocksRaycasts = false;
        a.group.interactable = false;
        a.basePos = new Vector2(l + origin.x * w, -(t + origin.y * h));
        rt.anchoredPosition = a.basePos;
        if (parent == null || parent == content)
        {
            var tag = go.AddComponent<ZTag>();
            tag.z = z;
            tag.order = order++;
        }
        actors.Add(a);
        return a;
    }

    private static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

    private Actor Img(string sprite, float l, float t, float w, float h, Color? tint = null,
        RectTransform parent = null, Vector2? origin = null, int z = 0)
    {
        Actor a = Make(sprite, parent, l, t, w, h, origin ?? Mid, z);
        a.image = a.rt.gameObject.AddComponent<Image>();
        a.image.sprite = sprite != null ? Art(sprite) : null;
        a.image.color = tint ?? Color.white;
        a.image.raycastTarget = false;
        if (a.image.sprite != null && a.image.sprite.border != Vector4.zero) a.image.type = Image.Type.Sliced;
        return a;
    }

    private Actor Box(Color color, float l, float t, float w, float h, RectTransform parent = null,
        Vector2? origin = null, int z = 0, bool round = false)
    {
        Actor a = Img(round ? "fx_rrect" : null, l, t, w, h, color, parent, origin, z);
        if (round) a.image.pixelsPerUnitMultiplier = 48f / Mathf.Max(8f, Mathf.Min(w, h));
        return a;
    }

    private Actor Group(float l, float t, float w, float h, RectTransform parent = null, Vector2? origin = null, int z = 0)
    {
        return Make("group", parent, l, t, w, h, origin ?? Mid, z);
    }

    private TMP_Text Label(Actor a, string text, float size, Color color)
    {
        var go = new GameObject("text", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(a.rt, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color.linear;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void Faces(Image img, params (float t, Sprite s)[] seq)
    {
        if (img == null || seq.Length == 0) return;
        updaters.Add(t =>
        {
            Sprite pick = seq[0].s;
            foreach (var p in seq) if (t >= p.t) pick = p.s;
            if (pick != null && img.sprite != pick) img.sprite = pick;
        });
    }

    // =========================================================
    // SHARED EFFECTS
    // =========================================================

    private void Sparkle(float cx, float cy, float t, Color c, float size = 26f)
    {
        Actor s = Img("fx_star", cx - size / 2, cy - size / 2, size, size, c, z: 45);
        Anim(s, K(t, o: 0, s: 0, r: 0), K(t + 0.25f, o: 1, s: 1.2f, r: 90), K(t + 0.7f, o: 0, s: 0, r: 200));
    }

    // A twinkle that uses percentages of the whole clip (emotions)
    private void Twinkle(float cx, float cy, float a, float b, float c, Color color, float size = 26f, RectTransform parent = null)
    {
        Actor s = Img("fx_star", cx - size / 2, cy - size / 2, size, size, color, parent, z: 45);
        Anim(s, C(0, D, 0, sx: 0, sy: 0, o: 0), C(a, D, 0, sx: 0, sy: 0, o: 0), C(b, D, 0, r: 90, sx: 1.2f, sy: 1.2f, o: 1),
            C(c, D, 0, r: 180, sx: 0, sy: 0, o: 0), C(100, D, 0, r: 180, sx: 0, sy: 0, o: 0));
    }

    private void SpeechBubble(string text, float l, float t, float from, float to, Color color, bool border = true)
    {
        float size = 22f;
        float w = text.Length * size * 0.56f + 34f, h = size * 1.25f + 14f;
        Actor b = Img(border ? "fx_bubble" : "fx_rrect", l, t, w, h, Color.white, origin: new Vector2(0.5f, 1f), z: 40);
        b.image.pixelsPerUnitMultiplier = 1.4f;
        Label(b, text, size, color);
        Anim(b, K(from, o: 0, s: 0.2f, r: -15), K(from + 0.15f, o: 1, s: 1.2f, r: 5), K(from + 0.3f, s: 1, r: 0),
            K(to, o: 1, s: 1), K(to + 0.2f, o: 0, s: 0.5f));
    }

    private void ConfettiRain(float t, int n)
    {
        for (int i = 0; i < n; i++)
        {
            Actor d = Box(Confetti[i % 6], (i * 47) % 600, -20, 10, 16, z: 45, round: true);
            float tt = t + UnityEngine.Random.value * 0.4f;
            Anim(d, K(tt, o: 0, y: 0), K(tt + 0.1f, o: 1), K(Mathf.Min(D, tt + 1.6f), o: 1, y: 430, r: 300 + UnityEngine.Random.value * 400, e: Ease.Linear));
        }
    }

    private Actor Flash(float t, float t1, Color color, float l, float top, float w, float h, float period, RectTransform parent = null, int z = 0)
    {
        Actor d = Img("fx_glow", l, top, w, h, color, parent, z: z);
        var k = new List<Key> { K(t, o: 0) };
        for (float x = t; x < t1 - 0.001f; x += period)
        {
            k.Add(K(x + 0.01f, o: 0.85f));
            k.Add(K(Mathf.Min(t1, x + period / 2), o: 0.85f));
            k.Add(K(Mathf.Min(t1, x + period / 2 + 0.01f), o: 0f));
        }
        k.Add(K(t1, o: 0));
        Anim(d, k);
        return d;
    }

    private List<Key> Hops(float t0, float t1, float step, float h, float? r = null)
    {
        var k = new List<Key>();
        int n = Mathf.Max(1, Mathf.RoundToInt((t1 - t0) / step));
        step = (t1 - t0) / n;
        for (int i = 0; i < n; i++)
        {
            float t = t0 + i * step;
            k.Add(K(t, y: 0, sx: 1.08f, sy: 0.92f));
            k.Add(K(t + step / 2, y: -h, sx: 0.96f, sy: 1.05f, r: r));
        }
        k.Add(K(t1, y: 0, sx: 1, sy: 1, r: 0));
        return k;
    }

    // Stars spinning on a flat ellipse, like a cartoon "seeing stars"
    private void Orbit(RectTransform parent, float cx, float cy, float rx, float ry, int count, float t, float t1, float size, float period)
    {
        Actor o = Group(cx, cy, 0, 0, parent);
        Anim(o, K(t, o: 0), K(t + 0.15f, o: 1), K(t1, o: 1), K(t1 + 0.2f, o: 0));
        Color[] colors = { Hex("FFC93C"), Hex("4A9DFF"), Hex("FF7EB6"), Hex("3DBE6B") };
        var stars = new List<RectTransform>();
        for (int i = 0; i < count; i++)
        {
            var s = Img("fx_star", -size / 2, -size / 2, size, size, colors[i % colors.Length], o.rt);
            actors.Remove(s);
            stars.Add(s.rt);
        }
        updaters.Add(time =>
        {
            for (int i = 0; i < stars.Count; i++)
            {
                float a = (time / period + (float)i / stars.Count) * Mathf.PI * 2f;
                stars[i].anchoredPosition = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
                stars[i].localScale = Vector3.one * (0.75f + 0.25f * Mathf.Sin(a));
                stars[i].localEulerAngles = new Vector3(0, 0, -time * 360f);
            }
        });
    }

    private Actor Shadow(float l, float top, float w, float h)
    {
        return Img("fx_circle", l, top, w, h, new Color(0.18f, 0.16f, 0.31f, 0.12f));
    }

    // =========================================================
    // EMOTIONS (3 seconds)
    // =========================================================

    private struct Bot
    {
        public Actor box;
        public Image face;
        public float w, h;
    }

    private Bot EmotionBot(string key, int firstFrame)
    {
        Sprite f = Face(key, firstFrame);
        float w = 260f, h = f != null ? w * f.rect.height / f.rect.width : 220f;
        Actor box = Group(170, H - 44 - h, w, h, origin: new Vector2(0.5f, 1f), z: 3);
        Actor face = Img(null, 0, 0, w, h, Color.white, box.rt);
        actors.Remove(face);
        face.image.sprite = f;
        face.image.preserveAspect = true;
        return new Bot { box = box, face = face.image, w = w, h = h };
    }

    private void FaceFrames(Image img, string key, params float[] pairs)
    {
        var seq = new (float, Sprite)[pairs.Length / 2];
        for (int i = 0; i < seq.Length; i++) seq[i] = (pairs[i * 2] * D, Face(key, (int)pairs[i * 2 + 1]));
        Faces(img, seq);
    }

    private void Body(Actor a, float[][] rows)
    {
        var k = new List<Key>();
        foreach (float[] r in rows) k.Add(C(r[0], D, 0, r[1], r[2], r[3], r[4], r[5], e: Ease.Body));
        Anim(a, k);
    }

    private void EmotionBase(string bg, Bot bot)
    {
        if (cardBack != null) cardBack.color = Hex(bg);
        Actor sh = Shadow(200, H - 30 - 24, 200, 24);
        sh.rt.GetComponent<ZTag>().z = 1;
        Anim(sh, C(0, D, 0, sx: 0.2f, sy: 0.2f), C(14, D, 0), C(100, D, 0));
    }

    // Fx positions are measured from the card's centre line
    private const float Cx = 300f;

    private void BuildHappy()
    {
        Bot b = EmotionBot("happy", 0);
        EmotionBase("EAF6FF", b);
        FaceFrames(b.face, "happy", 0, 0, 0.17f, 2);
        Body(b.box, new[]
        {
            new float[] { 0, 0, 0, 0, .2f, .2f }, new float[] { 6, 0, 0, 0, 1.15f, .9f }, new float[] { 10, 0, 0, 0, .95f, 1.06f },
            new float[] { 14, 0, 0, 0, 1, 1 }, new float[] { 24, 0, 0, 0, 1.12f, .86f }, new float[] { 32, 0, -70, -6, .92f, 1.1f },
            new float[] { 40, 0, 0, 0, 1.14f, .84f }, new float[] { 46, 0, 0, 0, 1, 1 }, new float[] { 52, 0, 0, 0, 1.12f, .86f },
            new float[] { 60, 0, -70, 6, .92f, 1.1f }, new float[] { 68, 0, 0, 0, 1.14f, .84f }, new float[] { 74, 0, 0, 0, .98f, 1.03f },
            new float[] { 80, 0, 0, -4, 1, 1 }, new float[] { 86, 0, 0, 4, 1, 1 }, new float[] { 92, 0, 0, 0, 1, 1 }, new float[] { 100, 0, 0, 0, 1, 1 }
        });

        Color blush = Hex("FF9EC4");
        foreach (float lx in new[] { 0.16f, 0.67f })
        {
            Actor c = Img("fx_circle", b.w * lx, b.h * 0.70f, b.w * 0.17f, b.h * 0.10f, blush, b.box.rt);
            Anim(c, C(0, D, 0, sx: .4f, sy: .4f, o: 0), C(20, D, 0, sx: .4f, sy: .4f, o: 0), C(28, D, 0, o: .75f), C(100, D, 0, o: .75f));
        }

        float[][] stars1 = { new float[] { -170, -90 }, new float[] { 170, -80 }, new float[] { -140, 40 } };
        float[][] stars2 = { new float[] { 150, 50 }, new float[] { -40, -150 }, new float[] { 60, -140 } };
        Color[] c1 = { Hex("FFC93C"), Hex("4A9DFF"), Hex("3DBE6B") }, c2 = { Hex("FFC93C"), Hex("9B6BFF"), Hex("FF8A5B") };
        for (int i = 0; i < 3; i++) Twinkle(Cx + stars1[i][0], 200 + stars1[i][1], 20 + i * 2, 28 + i * 2, 42 + i * 2, c1[i]);
        for (int i = 0; i < 3; i++) Twinkle(Cx + stars2[i][0], 200 + stars2[i][1], 50 + i * 2, 58 + i * 2, 72 + i * 2, c2[i]);

        float[] hx = { -80, -10, 60 };
        for (int i = 0; i < 3; i++)
        {
            float x = hx[i], r = i % 2 == 1 ? 15 : -15, dl = i * 0.04f * D;
            Actor h = Img("fx_heart", Cx - 13, 190, 26, 26, Hex("FF7EB6"), z: 4);
            Anim(h, C(0, D, dl, x, 40, 0, .3f, .3f, 0), C(30, D, dl, x, 40, 0, .3f, .3f, 0), C(38, D, dl, x, 0, 0, 1.1f, 1.1f, 1),
                C(70, D, dl, x * 1.15f, -60, r * .5f, 1, 1, 1), C(90, D, dl, x * 1.3f, -120, r, .9f, .9f, 0), C(100, D, dl, x * 1.3f, -120, r, .9f, .9f, 0));
        }

        EmotionConfetti(28);
        EmotionBubble("Yay!", Cx + 70, 28, Ink);
    }

    private void EmotionConfetti(int n)
    {
        for (int i = 0; i < n; i++)
        {
            float l = Cx + (i * 41) % 340 - 170 + UnityEngine.Random.value * 20, dl = UnityEngine.Random.value * 0.1f * D;
            float rr = 300 + UnityEngine.Random.value * 400;
            Actor c = Box(Confetti[i % 6], l, -10, 10, 16, z: 4, round: true);
            Anim(c, C(0, D, dl, 0, -20, 0, o: 0), C(28, D, dl, 0, -20, 0, o: 0), C(32, D, dl, 0, -20 + 460 * 4 / 72f, rr * 4 / 72f, o: 1),
                C(92, D, dl, 0, -20 + 460 * 64 / 72f, rr * 64 / 72f, o: 1), C(100, D, dl, 0, 440, rr, o: 0));
        }
    }

    private void EmotionBubble(string text, float l, float t, Color color)
    {
        float size = 28f, w = text.Length * size * 0.58f + 36f, h = size * 1.2f + 12f;
        Actor b = Img("fx_rrect", l, t, w, h, Color.white, origin: new Vector2(0f, 1f), z: 5);
        b.image.pixelsPerUnitMultiplier = 0.7f;
        Label(b, text, size, color);
        Anim(b, C(0, D, 0, r: -20, sx: .2f, sy: .2f, o: 0), C(30, D, 0, r: -20, sx: .2f, sy: .2f, o: 0), C(36, D, 0, r: 6, sx: 1.2f, sy: 1.2f, o: 1),
            C(40, D, 0, r: -3, o: 1), C(88, D, 0, o: 1), C(96, D, 0, sx: .6f, sy: .6f, o: 0), C(100, D, 0, sx: .6f, sy: .6f, o: 0));
    }

    private void BuildSad()
    {
        Bot b = EmotionBot("sad", 0);
        EmotionBase("DCE6F2", b);
        FaceFrames(b.face, "sad", 0, 0, .18f, 1, .36f, 2, .56f, 3, .76f, 4);
        Body(b.box, new[]
        {
            new float[] { 0, 0, 0, 0, .2f, .2f }, new float[] { 8, 0, 0, 0, 1.08f, .94f }, new float[] { 14, 0, 0, 0, 1, 1 },
            new float[] { 30, 0, 12, 0, 1.05f, .93f }, new float[] { 45, 0, 12, -3, 1.05f, .93f }, new float[] { 60, 0, 14, 3, 1.06f, .92f },
            new float[] { 75, 0, 12, -3, 1.05f, .93f }, new float[] { 90, 0, 14, 0, 1.06f, .92f }, new float[] { 100, 0, 14, 0, 1.06f, .92f }
        });

        Actor cloud = Img("fx_cloud", Cx - 90, 20, 180, 90, Hex("9AA6BF"), z: 4);
        Anim(cloud, C(0, D, 0, -200, o: 0), C(16, D, 0, -200, o: 0), C(30, D, 0, o: 1), C(92, D, 0, o: 1), C(100, D, 0, o: 0));

        for (int i = 0; i < 14; i++)
        {
            float dur = 0.22f * D, dl = (0.3f + ((i * 7) % 10) * 0.06f) * D;
            Actor r = Img("fx_drop", Cx - 70 + (i * 37) % 140, 95, 9, 13, Hex("7FB3E8"), z: 4);
            Anim(r, C(0, dur, dl, o: 0), C(20, dur, dl, 0, 30, o: .9f), C(100, dur, dl, 0, 150, o: 0));
        }

        float[][] eyes = { new float[] { 0.29f, 0.58f }, new float[] { 0.63f, 0.58f } };
        for (int j = 0; j < 2; j++)
        {
            for (int k = 0; k < 4; k++)
            {
                float dur = 0.2f * D, dl = (0.24f + k * 0.17f + j * 0.05f) * D;
                Actor tr = Img("fx_drop", b.w * (eyes[j][0] + j * 0.02f), b.h * eyes[j][1], 14, 20, Hex("4FC3F7"), b.box.rt);
                Anim(tr, C(0, dur, dl, sx: .4f, sy: .4f, o: 0), C(20, dur, dl, 0, 4, o: 1), C(100, dur, dl, 0, 70, sx: .8f, sy: .8f, o: 0));
            }
        }
    }

    private void BuildAngry()
    {
        Bot b = EmotionBot("angry", 0);
        EmotionBase("FDE8E4", b);
        FaceFrames(b.face, "angry", 0, 0, .12f, 1, .25f, 2, .5f, 3, .72f, 4);

        Actor flush = Img("fx_glow", b.w * 0.08f, b.h * 0.30f, b.w * 0.84f, b.h * 0.70f, Hex("FF5A4E"), b.box.rt, new Vector2(0.5f, 1f));
        flush.rt.SetAsFirstSibling();
        Anim(flush, C(0, D, 0, sy: .2f, o: 0), C(20, D, 0, sy: .2f, o: 0), C(40, D, 0, o: .55f), C(85, D, 0, o: .55f), C(100, D, 0, o: .2f));

        Body(b.box, new[]
        {
            new float[] { 0, 0, 0, 0, .2f, .2f }, new float[] { 8, 0, 0, 0, 1.1f, .92f }, new float[] { 14, 0, 0, 0, 1, 1 },
            new float[] { 20, 0, 0, 0, 1.08f, .9f }, new float[] { 24, -7, 0, 0, 1.04f, 1.04f }, new float[] { 27, 7, 0, 0, 1.04f, 1.04f },
            new float[] { 30, -7, 0, 0, 1, 1 }, new float[] { 33, 7, 0, 0, 1, 1 }, new float[] { 36, -5, 0, 0, 1, 1 },
            new float[] { 39, 0, 0, 0, 1.1f, .9f }, new float[] { 44, 0, -30, 0, .95f, 1.08f }, new float[] { 50, 0, 0, 0, 1.15f, .85f },
            new float[] { 55, -7, 0, 0, 1, 1 }, new float[] { 58, 7, 0, 0, 1, 1 }, new float[] { 61, -7, 0, 0, 1, 1 },
            new float[] { 64, 7, 0, 0, 1, 1 }, new float[] { 67, -5, 0, 0, 1, 1 }, new float[] { 70, 0, 0, 0, 1.1f, .9f },
            new float[] { 75, 0, -30, 0, .95f, 1.08f }, new float[] { 81, 0, 0, 0, 1.15f, .85f }, new float[] { 86, 0, 0, 0, 1.04f, 1.04f },
            new float[] { 92, 0, 0, 0, 1, 1 }, new float[] { 100, 0, 0, 0, 1, 1 }
        });

        Actor vein = Img("fx_vein", b.w * 0.76f, b.h * 0.02f, 44, 44, Color.white, b.box.rt);
        Anim(vein, C(0, D, 0, sx: .2f, sy: .2f, o: 0), C(22, D, 0, sx: .2f, sy: .2f, o: 0), C(28, D, 0, sx: 1.4f, sy: 1.4f, o: 1),
            C(32, D, 0, o: 1), C(50, D, 0, sx: 1.3f, sy: 1.3f, o: 1), C(54, D, 0, o: 1), C(70, D, 0, sx: 1.3f, sy: 1.3f, o: 1),
            C(74, D, 0, o: 1), C(92, D, 0, o: 1), C(100, D, 0, o: 0));

        for (int j = 0; j < 2; j++)
        {
            float side = j == 0 ? -1 : 1;
            for (int k = 0; k < 5; k++)
            {
                float dur = 0.3f * D, dl = (0.22f + k * 0.13f + j * 0.06f) * D;
                Actor puff = Img("fx_circle", Cx + side * 120 - 17, 150, 34, 34, Hex("F4F2FA"), z: 4);
                Anim(puff, C(0, dur, dl, sx: .3f, sy: .3f, o: 0), C(25, dur, dl, side * 10, -22, sx: .6f, sy: .6f, o: .95f),
                    C(100, dur, dl, side * 40, -90, sx: 1.4f, sy: 1.4f, o: 0));
            }
        }
    }

    private void BuildSurprise()
    {
        Bot b = EmotionBot("surprise", 0);
        EmotionBase("FFF4D6", b);
        FaceFrames(b.face, "surprise", 0, 0, .14f, 1, .3f, 2, .55f, 3);
        Body(b.box, new[]
        {
            new float[] { 0, 0, 0, 0, .2f, .2f }, new float[] { 6, 0, 0, 0, 1.1f, .92f }, new float[] { 12, 0, 0, 0, 1, 1 },
            new float[] { 18, 0, 0, 0, 1.2f, .75f }, new float[] { 26, 0, -90, 0, .8f, 1.25f }, new float[] { 34, 0, 0, 0, 1.2f, .8f },
            new float[] { 40, 0, 0, 0, .95f, 1.06f }, new float[] { 44, 0, 0, 0, 1, 1 }, new float[] { 50, -3, 0, 0, 1, 1 },
            new float[] { 52, 3, 0, 0, 1, 1 }, new float[] { 54, -3, 0, 0, 1, 1 }, new float[] { 56, 0, 0, 0, 1, 1 },
            new float[] { 66, 0, 0, 0, 1.06f, 1.06f }, new float[] { 72, 0, 0, 0, 1, 1 }, new float[] { 80, 0, 0, 0, 1.06f, 1.06f },
            new float[] { 86, 0, 0, 0, 1, 1 }, new float[] { 100, 0, 0, 0, 1, 1 }
        });

        Bang("!", Cx + 110, 40, 60, 80, Hex("FF8A5B"), true);

        for (int i = 0; i < 10; i++)
        {
            float ang = i * 36f;
            Actor ray = Box(Confetti[i % 6], Cx - 4, 220, 8, 34, z: 4, round: true);
            ray.rt.localEulerAngles = new Vector3(0, 0, -ang);
            Vector2 baseP = ray.basePos;
            RectTransform rt = ray.rt;
            CanvasGroup g = ray.group;
            actors.Remove(ray);
            updaters.Add(t =>
            {
                float p = t / D * 100f, dist, sy, o;
                if (p < 16) { dist = 90; sy = .2f; o = 0; }
                else if (p < 24) { float u = (p - 16) / 8f; dist = Mathf.Lerp(90, 150, u); sy = Mathf.Lerp(.2f, 1, u); o = u; }
                else if (p < 36) { float u = (p - 24) / 12f; dist = Mathf.Lerp(150, 190, u); sy = Mathf.Lerp(1, .3f, u); o = 1 - u; }
                else { dist = 190; sy = .3f; o = 0; }
                float rad = ang * Mathf.Deg2Rad;
                rt.anchoredPosition = baseP + new Vector2(Mathf.Sin(rad) * dist, Mathf.Cos(rad) * dist);
                rt.localScale = new Vector3(1, sy, 1);
                g.alpha = o;
            });
        }

        float[][] sp = { new float[] { -150, -60 }, new float[] { 150, -40 }, new float[] { -110, 60 }, new float[] { 120, 70 }, new float[] { 0, -170 } };
        Color[] cs = { Hex("FFC93C"), Hex("FFC93C"), Hex("4A9DFF"), Hex("9B6BFF"), Hex("FFC93C") };
        for (int i = 0; i < sp.Length; i++) Twinkle(Cx + sp[i][0], 200 + sp[i][1], 45 + i * 2, 53 + i * 2, 67 + i * 2, cs[i]);
    }

    // A round "!" sign that pops up (emotion clip uses percentages)
    private void Bang(string text, float l, float t, float w, float h, Color color, bool emotionTiming, float from = 0, float to = 0, int z = 5)
    {
        Actor bang = Box(color, l, t, w, h, origin: new Vector2(0.5f, 1f), z: z, round: true);
        bang.image.pixelsPerUnitMultiplier = 48f / (w * 0.5f) * 0.5f;
        Label(bang, text, h * 0.72f, Color.white);
        if (emotionTiming)
        {
            Anim(bang, C(0, D, 0, r: -30, sx: .2f, sy: .2f, o: 0), C(16, D, 0, r: -30, sx: .2f, sy: .2f, o: 0),
                C(22, D, 0, r: 10, sx: 1.4f, sy: 1.4f, o: 1), C(27, D, 0, r: -6, o: 1), C(33, D, 0, r: 4, sx: 1.1f, sy: 1.1f, o: 1),
                C(38, D, 0, o: 1), C(85, D, 0, o: 1), C(95, D, 0, sx: .4f, sy: .4f, o: 0), C(100, D, 0, sx: .4f, sy: .4f, o: 0));
        }
        else
        {
            Anim(bang, K(from, o: 0, s: 0.2f, r: -20), K(from + 0.15f, o: 1, s: 1.3f, r: 8), K(from + 0.3f, s: 1, r: 0),
                K(to, o: 1), K(to + 0.2f, o: 0));
        }
    }

    private void BuildDizzy()
    {
        Bot b = EmotionBot("dizzy", 0);
        EmotionBase("EFE8FF", b);
        FaceFrames(b.face, "dizzy", 0, 0, .15f, 1, .4f, 2, .6f, 3, .8f, 4);
        Body(b.box, new[]
        {
            new float[] { 0, 0, 0, 0, .2f, .2f }, new float[] { 8, 0, 0, 0, 1.1f, .92f }, new float[] { 14, 0, 0, 0, 1, 1 },
            new float[] { 24, -18, 0, -12, 1, 1 }, new float[] { 34, 16, -6, 10, 1, 1 }, new float[] { 44, -14, 0, -11, 1, 1 },
            new float[] { 54, 14, -6, 12, 1, 1 }, new float[] { 64, -16, 0, -10, 1, 1 }, new float[] { 74, 12, 0, 9, 1, 1 },
            new float[] { 84, -6, 0, -5, 1, 1 }, new float[] { 92, 0, 0, 0, 1, 1 }, new float[] { 100, 0, 0, 0, 1, 1 }
        });
        Orbit(b.box.rt, b.w * 0.5f, b.h * 0.14f, 105, 34, 4, 0.14f * D, 0.9f * D, 28, 1.1f);
    }

    private void BuildSleepy()
    {
        Bot b = EmotionBot("sleepy", 0);
        EmotionBase("EAF6FF", b);
        Actor night = Box(Hex("3B3F7A"), 0, 0, W, H, z: 2);
        Anim(night, C(0, D, 0, o: 0), C(15, D, 0, o: 0), C(35, D, 0, o: 1), C(100, D, 0, o: 1));
        Actor moon = Img("fx_moon", W - 60 - 60, 30, 60, 60, Color.white, z: 2);
        Anim(moon, C(0, D, 0, o: 0), C(15, D, 0, o: 0), C(35, D, 0, o: 1), C(100, D, 0, o: 1));
        for (int i = 0; i < 9; i++)
        {
            Actor s = Img("fx_star", W * (8 + (i * 97) % 84) / 100f, H * (10 + (i * 53) % 45) / 100f, 12, 12, Hex("FFF6C7"), z: 2);
            float a = 20 + i * 6, bb = 30 + i * 6, c = 55 + i * 5;
            Anim(s, C(0, D, 0, sx: 0, sy: 0, o: 0), C(a, D, 0, sx: 0, sy: 0, o: 0), C(bb, D, 0, r: 90, sx: 1.2f, sy: 1.2f, o: 1),
                C(Mathf.Min(c, 100), D, 0, r: 180, sx: 0, sy: 0, o: 0), C(100, D, 0, r: 180, sx: 0, sy: 0, o: 0));
        }

        FaceFrames(b.face, "sleepy", 0, 0, .15f, 1, .35f, 2, .55f, 3, .75f, 4);
        Body(b.box, new[]
        {
            new float[] { 0, 0, 0, 0, .2f, .2f }, new float[] { 8, 0, 0, 0, 1.06f, .95f }, new float[] { 14, 0, 0, 0, 1, 1 },
            new float[] { 24, 0, 0, 0, 1.04f, .96f }, new float[] { 32, 0, 0, 0, 1, 1 }, new float[] { 42, 0, 10, -10, 1.04f, .96f },
            new float[] { 54, 0, 12, -12, 1, 1 }, new float[] { 57, 0, -14, 3, .96f, 1.06f }, new float[] { 62, 0, 0, 0, 1, 1 },
            new float[] { 72, 0, 0, 0, 1.04f, .96f }, new float[] { 82, 0, 10, 9, 1.04f, .96f }, new float[] { 100, 0, 12, 11, 1.03f, .97f }
        });

        Actor bubble = Img("fx_circle", b.w * 0.55f - 15, b.h * 0.72f - 15, 30, 30, Hex("BFE9FF"), b.box.rt);
        Img("fx_ring", 0, 0, 30, 30, Color.white, bubble.rt);
        Anim(bubble, C(0, D, 0, sx: .1f, sy: .1f, o: 0), C(20, D, 0, sx: .1f, sy: .1f, o: 0), C(30, D, 0, sx: .7f, sy: .7f, o: .9f),
            C(38, D, 0, sx: .4f, sy: .4f, o: .9f), C(46, D, 0, o: .9f), C(52, D, 0, sx: 1.15f, sy: 1.15f, o: .9f),
            C(55, D, 0, sx: 1.6f, sy: 1.6f, o: 0), C(72, D, 0, sx: .1f, sy: .1f, o: 0), C(82, D, 0, sx: .6f, sy: .6f, o: .9f),
            C(90, D, 0, sx: .35f, sy: .35f, o: .9f), C(100, D, 0, sx: .8f, sy: .8f, o: .9f));

        for (int k = 0; k < 5; k++)
        {
            float dur = 0.4f * D, dl = (0.2f + k * 0.15f) * D, size = 26 + (k % 2) * 10;
            Actor z = Group(Cx + 70, 110, 40, 40, z: 4);
            Label(z, "z", size, Hex("BFD4FF"));
            Anim(z, C(0, dur, dl, r: -10, sx: .4f, sy: .4f, o: 0), C(20, dur, dl, 12, -22, 0, .58f, .58f, 1),
                C(100, dur, dl, 60, -110, 15, 1.3f, 1.3f, 0));
        }
    }

    private void BuildPlayful()
    {
        Bot b = EmotionBot("playful", 0);
        EmotionBase("E8F7EE", b);
        FaceFrames(b.face, "playful", 0, 0, .2f, 1, .4f, 2, .6f, 3, .8f, 4);
        Body(b.box, new[]
        {
            new float[] { 0, 0, 0, 0, .2f, .2f }, new float[] { 6, 0, 0, 0, 1.1f, .92f }, new float[] { 12, 0, 0, 0, 1, 1 },
            new float[] { 18, 0, 0, 0, 1.1f, .9f }, new float[] { 26, -60, -45, -12, 1, 1 }, new float[] { 34, -80, 0, 0, 1.1f, .9f },
            new float[] { 40, -80, 0, -6, 1, 1 }, new float[] { 48, 0, -55, 10, 1, 1 }, new float[] { 56, 80, 0, 0, 1.1f, .9f },
            new float[] { 62, 80, 0, 6, 1, 1 }, new float[] { 70, 40, -45, -8, 1, 1 }, new float[] { 78, 0, 0, 0, 1.12f, .88f },
            new float[] { 84, 0, -25, 360, 1, 1 }, new float[] { 90, 0, 0, 360, 1.08f, .92f }, new float[] { 96, 0, 0, 360, 1, 1 },
            new float[] { 100, 0, 0, 360, 1, 1 }
        });

        for (int k = 0; k < 8; k++)
        {
            float side = k % 2 == 1 ? 1 : -1, dur = 0.35f * D, dl = (0.15f + k * 0.08f) * D, x = side * 40;
            Actor n = Img("fx_note", Cx + side * (60 + k * 12), 200, 26, 32, Confetti[k % 6], z: 4);
            Anim(n, C(0, dur, dl, 0, 20, -15, .4f, .4f, 0), C(20, dur, dl, x * .3f, 0, 10, 1, 1, 1),
                C(50, dur, dl, x * .6f, -40, -10, 1, 1, 1), C(100, dur, dl, x, -110, 12, 1, 1, 0));
        }
        Twinkle(Cx - 60, 200 - 170, 84, 92, 100, Hex("FFC93C"));
        Twinkle(Cx + 70, 200 - 160, 86, 94, 100, Hex("FF7EB6"));
        EmotionBubble("Hehe!", Cx + 90, 28, Hex("3DBE6B"));
    }

    // =========================================================
    // SCENARIOS (5 seconds)
    // =========================================================

    private struct Hero
    {
        public Actor outer, inner;
        public Image face;
        public float w, h;
    }

    // mTiny with a hat; x, y keys place the bottom-centre point
    private Hero ScenarioBot(string hat, string face, int z = 20)
    {
        Sprite f = FaceSprite(face);
        float w = 110f, h = f != null ? w * f.rect.height / f.rect.width : 95f;
        Actor outer = Make("mTiny", null, -w / 2, -h, w, h, new Vector2(0.5f, 1f), z);
        outer.basePos = Vector2.zero;
        Actor inner = Group(0, 0, w, h, outer.rt, new Vector2(0.5f, 1f));
        Actor img = Img(null, 0, 0, w, h, Color.white, inner.rt);
        actors.Remove(img);
        img.image.sprite = f;
        img.image.preserveAspect = true;
        if (hat != null)
        {
            Actor hatImg = Img(hat, w * 0.21f, -h * 0.23f - 2, w * 0.58f, w * 0.58f * 0.62f, Color.white, inner.rt);
            actors.Remove(hatImg);
        }
        return new Hero { outer = outer, inner = inner, face = img.image, w = w, h = h };
    }

    // The faces used in the stories, picked from the emotion sheets
    private Sprite FaceSprite(string k)
    {
        switch (k)
        {
            case "H": return Face("happy", 2);
            case "A": return Face("angry", 3);
            case "D": return Face("dizzy", 2);
            case "W": return Face("playful", 1);
            case "L": return Face("playful", 4);
            case "E": return Face("surprise", 2);
            case "Z": return Face("sleepy", 2);
            case "S": return Face("sad", 2);
            default: return Face("happy", 0);
        }
    }

    private void HeroFaces(Image img, params object[] seq)
    {
        var list = new (float, Sprite)[seq.Length / 2];
        for (int i = 0; i < list.Length; i++) list[i] = (Convert.ToSingle(seq[i * 2]), FaceSprite((string)seq[i * 2 + 1]));
        Faces(img, list);
    }

    private List<Key> Join(params object[] parts)
    {
        var all = new List<Key>();
        foreach (object p in parts)
        {
            if (p is Key k) all.Add(k);
            else if (p is List<Key> l) all.AddRange(l);
        }
        return all;
    }

    private Actor Ground(Color color, float y = 330f)
    {
        return Box(color, 0, y, W, H - y);
    }

    private Image Peek(Color bg, float l, float t, float w, float h, string face, float faceW, int z = 11)
    {
        Actor win = Box(bg, l, t, w, h, z: z);
        win.rt.gameObject.AddComponent<RectMask2D>();
        Actor peek = Img(null, (w - faceW) / 2, h, faceW, faceW * 0.86f, Color.white, win.rt);
        peek.image.sprite = FaceSprite(face);
        peek.image.preserveAspect = true;
        peekActor = peek;
        return peek.image;
    }

    private Actor peekActor;

    private void BuildPolice()
    {
        if (cardBack != null) cardBack.color = Hex("EAF6FF");
        Img("fx_cloud", 40, 50, 120, 60, Color.white);
        Ground(Hex("CDEBC4"));
        Box(Hex("E5E7EB"), 0, 330, W, 18);
        Img("sc_station", 350, 108, 230, 222, z: 10);

        Image peekImg = Peek(Hex("BFE3FF"), 506, 214, 52, 44, "W", 46);
        Actor glow = Box(Hex("FFE58A"), 0, 0, 52, 44, peekActor.rt.parent as RectTransform);
        glow.rt.SetAsFirstSibling();
        Anim(glow, K(3.2f, o: 0), K(3.4f, o: 1));
        Anim(peekActor, K(3.4f, y: 0), K(3.7f, y: -40), K(4.1f, y: -36, r: -8), K(4.4f, y: -40, r: 8), K(4.7f, y: -38, r: 0));
        Faces(peekImg, (0f, FaceSprite("W")), (4.0f, FaceSprite("H")));

        Actor door = Box(Hex("2E5AAC"), 438, 238, 54, 92, origin: new Vector2(0f, 0.5f), z: 25);
        Img("fx_circle", 40, 44, 7, 7, Hex("FFC93C"), door.rt);
        Anim(door, K(1.9f, sx: 1, o: 1), K(2.3f, sx: .08f, o: 0), K(2.95f, sx: .08f, o: 0), K(3.25f, sx: 1, o: 1));

        Flash(3.3f, 5f, Hex("FF4D4D"), 340, 108, 70, 60, .4f, z: 9);
        Flash(3.5f, 5f, Hex("3D7BFF"), 520, 108, 70, 60, .4f, z: 9);

        Hero m = ScenarioBot("hat_police", "N", 22);
        Anim(m.outer, K(0, x: -70, y: 330), K(1.5f, x: 290, e: Ease.Linear), K(2.1f, x: 290), K(2.8f, x: 465, s: .85f), K(3.0f, x: 468, o: 0, s: .7f));
        Anim(m.inner, Join(Hops(0, 1.5f, .25f, 18), K(1.6f, y: 0), K(1.75f, y: -40, sx: .92f, sy: 1.1f), K(1.9f, y: 0, sx: 1.1f, sy: .9f),
            K(2.0f, sx: 1, sy: 1), Hops(2.1f, 2.8f, .23f, 14)));
        HeroFaces(m.face, 0f, "N", 1.6f, "H");

        Sparkle(400, 128, 1.75f, Hex("FFC93C"));
        Sparkle(330, 150, 1.85f, Hex("4A9DFF"));
        SpeechBubble("On duty!", 420, 140, 3.8f, 4.8f, Hex("2E5AAC"));
        Sparkle(532, 200, 4.0f, Hex("FFC93C"), 30);
        Sparkle(470, 190, 4.2f, Hex("3DBE6B"));
    }

    private void BuildHospital()
    {
        if (cardBack != null) cardBack.color = Hex("FFF3F5");
        Ground(Hex("D9EFD3"));
        Box(Hex("E5E7EB"), 0, 330, W, 18);
        Img("sc_hospital", 370, 88, 220, 242, z: 10);

        Actor ecg = Img("sc_ecg", 432, 212, 96, 30, z: 11);
        ecg.image.type = Image.Type.Filled;
        ecg.image.fillMethod = Image.FillMethod.Horizontal;
        Image ecgImg = ecg.image;
        updaters.Add(t =>
        {
            if (t < 3.3f) { ecgImg.fillAmount = 0; ecgImg.fillOrigin = 0; }
            else if (t < 3.8f) { ecgImg.fillOrigin = 0; ecgImg.fillAmount = (t - 3.3f) / 0.5f; }
            else if (t < 4.1f) { ecgImg.fillAmount = 1; }
            else if (t < 4.6f) { ecgImg.fillOrigin = 1; ecgImg.fillAmount = 1 - (t - 4.1f) / 0.5f; }
            else ecgImg.fillAmount = 0;
        });

        Actor dl = Box(Hex("8FC7F0"), 448, 250, 34, 80, z: 25);
        Actor dr = Box(Hex("8FC7F0"), 480, 250, 34, 80, z: 25);
        Anim(dl, K(2.5f, x: 0), K(2.8f, x: -30), K(3.2f, x: -30), K(3.45f, x: 0));
        Anim(dr, K(2.5f, x: 0), K(2.8f, x: 30), K(3.2f, x: 30), K(3.45f, x: 0));

        Peek(Hex("FFE58A"), 522, 164, 44, 40, "H", 40);
        Anim(peekActor, K(3.6f, y: 0), K(3.9f, y: -36), K(4.3f, y: -34, r: -8), K(4.6f, y: -36, r: 8), K(4.9f, y: -36, r: 0));

        Actor amb = Img("sc_ambulance", 0, 228, 180, 110, z: 15);
        Anim(amb, K(0, x: -200), K(1.3f, x: 60, e: Ease.Out), K(1.4f, x: 66, r: -2), K(1.55f, x: 60, r: 0), K(3.8f, x: 60, e: Ease.In), K(4.6f, x: -260));
        Flash(0, 3.8f, Hex("3D7BFF"), 52, -6, 36, 30, .3f, amb.rt);

        Hero m = ScenarioBot("hat_nurse", "E", 22);
        Anim(m.outer, K(0, x: 180, y: 320, o: 0, s: .3f), K(1.6f, x: 180, o: 0, s: .3f), K(1.65f, o: 1, e: Ease.Out), K(2.0f, x: 290, y: 330, s: 1),
            K(2.2f, x: 290), K(3.0f, x: 478, s: .85f), K(3.2f, x: 480, o: 0, s: .7f));
        Anim(m.inner, Join(K(1.65f, y: 0), K(1.85f, y: -50, sx: .9f, sy: 1.1f), K(2.0f, y: 0, sx: 1.1f, sy: .9f), K(2.1f, sx: 1, sy: 1), Hops(2.2f, 3.0f, .2f, 14)));
        HeroFaces(m.face, 0f, "E", 2.05f, "N");

        for (int k = 0; k < 7; k++)
        {
            float t = 3.8f + k * .12f;
            Actor h = Img("fx_heart", 430 + (k * 23) % 120, 200, 24, 24, k % 2 == 1 ? Hex("FF7EB6") : Hex("FF4D5E"), z: 30);
            Anim(h, K(t, o: 0, y: 0, s: .3f), K(t + .2f, o: 1, s: 1.1f), K(Mathf.Min(D, t + 1f), o: 0, y: -140, x: k % 2 == 1 ? 20 : -20));
        }
        SpeechBubble("Help is here!", 300, 120, 3.9f, 4.9f, Hex("FF4D5E"));
    }

    private void BuildCrossing()
    {
        if (cardBack != null) cardBack.color = Hex("CDEBC4");
        Box(Hex("E5E7EB"), 186, 0, 40, H);
        Box(Hex("E5E7EB"), 374, 0, 40, H);
        Actor road = Box(Hex("6B7280"), 226, 0, 148, H);
        for (int i = 0; i < 8; i++) Box(Hex("FFC93C"), 72, i * 55 - 10, 4, 28, road.rt);
        for (int i = 0; i < 6; i++) Box(Color.white, 6 + i * 24, 262, 14, 76, road.rt);

        Actor c1 = Img("sc_car", 240, -110, 54, 92, Hex("FF5A5A"), z: 15);
        Anim(c1, K(0.3f, y: 0, e: Ease.Linear), K(1.5f, y: 640));
        Actor c2 = Img("sc_car", 306, 420, 54, 92, Hex("4A9DFF"), z: 15);
        c2.rt.localEulerAngles = new Vector3(0, 0, 180);
        Anim(c2, K(1.1f, y: 0, r: 180, e: Ease.Linear), K(2.3f, y: -640, r: 180));

        Box(Hex("4B5563"), 438, 150, 8, 110, z: 12);
        Actor box = Box(Hex("2E2A4F"), 420, 96, 44, 70, z: 13, round: true);
        Actor red = Img("sc_redman", 7, 6, 30, 26, Color.white, box.rt);
        Actor green = Img("sc_greenman", 7, 36, 30, 28, Color.white, box.rt);
        Anim(red, K(2.55f, o: 1), K(2.6f, o: .15f));
        Anim(green, K(0, o: .15f), K(2.55f, o: .15f), K(2.6f, o: 1, s: 1.2f), K(2.8f, s: 1));
        Actor ring = Img("fx_ring", 412, 118, 60, 60, Hex("3DBE6B"), z: 12);
        Anim(ring, K(0, o: 0), K(2.59f, o: 0, s: .6f), K(2.6f, o: .9f, s: .6f), K(3.2f, o: 0, s: 2));

        Hero m = ScenarioBot("hat_hard", "N");
        Anim(m.outer, K(0, x: -70, y: 330, e: Ease.Out), K(1.0f, x: 150), K(1.3f, x: 150, r: 0), K(1.55f, x: 140, r: -10), K(1.85f, x: 140, r: -10),
            K(2.05f, x: 160, r: 10), K(2.35f, x: 160, r: 10), K(2.5f, x: 150, r: 0), K(2.8f, x: 150, e: Ease.Linear), K(4.0f, x: 500));
        Anim(m.inner, Join(Hops(0, 1.0f, .2f, 12), K(2.65f, y: 0), K(2.8f, y: -24, sx: .94f, sy: 1.06f), K(2.9f, y: 0), Hops(2.9f, 4.0f, .22f, 10),
            K(4.2f, y: -36, sx: .92f, sy: 1.1f), K(4.4f, y: 0, sx: 1.1f, sy: .9f), K(4.5f, sx: 1, sy: 1)));
        HeroFaces(m.face, 0f, "N", 1.4f, "L", 2.6f, "N", 4.1f, "H");

        Actor la = Img("fx_arrow", 70, 210, 40, 30, Hex("2E5AAC"), z: 30);
        Actor ra = Img("fx_arrow", 200, 210, 40, 30, Hex("2E5AAC"), z: 30);
        ra.rt.localEulerAngles = new Vector3(0, 0, 180);
        Anim(la, K(1.45f, o: 0, x: 10), K(1.6f, o: 1, x: 0), K(1.9f, o: 0, x: -10));
        Anim(ra, K(1.95f, o: 0, x: -10, r: 180), K(2.1f, o: 1, x: 0, r: 180), K(2.4f, o: 0, x: 10, r: 180));
        SpeechBubble("Wait for green!", 30, 120, 1.2f, 2.5f, Hex("FF4D4D"));

        Actor ck = Img("fx_check", 530, 150, 56, 56, z: 40);
        Anim(ck, K(4.2f, o: 0, s: .2f, r: -20), K(4.4f, o: 1, s: 1.3f, r: 8), K(4.6f, s: 1, r: 0));
        SpeechBubble("Safe!", 420, 110, 4.25f, 4.95f, Hex("3DBE6B"));
        ConfettiRain(4.2f, 16);
    }

    private void BuildTheft()
    {
        if (cardBack != null) cardBack.color = Hex("2B2F5E");
        if (title != null) title.color = Ink.linear;
        Img("fx_moon", 500, 30, 60, 60);
        for (int i = 0; i < 10; i++) Img("fx_star", (i * 67) % 560 + 20, (i * 37) % 120 + 20, 10, 10, Hex("FFF6C7"));
        Ground(Hex("3A3F6E"));
        Img("sc_shop", 20, 118, 200, 212, z: 10);
        Actor lit = Box(Hex("FFE58A"), 46, 214, 70, 56, z: 11, round: true);
        Anim(lit, K(0, o: 0), K(4.0f, o: 0), K(4.2f, o: 1));

        Actor coon = Img("sc_raccoon", 0, 0, 74, 96, z: 18);
        Anim(coon, K(0, x: 135, y: 238, o: 0), K(0.2f, o: 1), K(0.4f, x: 150, y: 232), K(0.6f, x: 175, y: 238), K(0.8f, x: 200, y: 232),
            K(1.0f, x: 225, y: 238), K(1.2f, x: 245, y: 234), K(1.45f, x: 245, y: 205, s: 1.1f), K(1.6f, x: 245, y: 238, s: 1),
            K(1.75f, x: 230, sx: -1, sy: 1, e: Ease.Linear), K(3.2f, x: -120, y: 238, sx: -1));
        Actor bag = Img("sc_bag", 0, 0, 46, 46, z: 19);
        Anim(bag, K(0, x: 180, y: 236, o: 0), K(0.2f, o: 1), K(0.4f, x: 195, y: 230), K(0.6f, x: 220, y: 236), K(0.8f, x: 245, y: 230),
            K(1.0f, x: 270, y: 236), K(1.2f, x: 290, y: 232), K(1.45f, x: 290, y: 204), K(1.6f, x: 290, y: 236), K(1.75f, x: 200, y: 236, e: Ease.Linear),
            K(2.4f, x: 110, y: 238), K(2.55f, x: 120, y: 250, r: 30), K(2.75f, x: 130, y: 288, r: -10), K(2.9f, x: 132, y: 286, r: 0),
            K(3.5f, x: 132, y: 286), K(3.7f, x: 220, y: 200, s: 1), K(3.75f, x: 220, y: 200, o: 1), K(4.0f, x: 150, y: 250, s: .4f, o: 0));

        Hero m = ScenarioBot("hat_police", "Z");
        Actor beam = Img("fx_beam", m.w / 2 - 230, m.h - 110, 190, 120, Color.white, m.outer.rt);
        beam.rt.SetAsFirstSibling();
        Anim(beam, K(0, o: 0), K(1.3f, o: 0), K(1.45f, o: 1), K(3.1f, o: 1), K(3.3f, o: 0));
        Anim(m.outer, K(0, x: 520, y: 330), K(1.6f, x: 520, e: Ease.Linear), K(3.1f, x: 250), K(3.4f, x: 210), K(3.9f, x: 230), K(4.2f, x: 300));
        Anim(m.inner, Join(K(0, y: 0, sx: 1.03f, sy: .97f), K(0.6f, sx: 1, sy: 1), K(1.2f, sx: 1.03f, sy: .97f), K(1.3f, y: -40, sx: .9f, sy: 1.1f),
            K(1.45f, y: 0, sx: 1.1f, sy: .9f), K(1.55f, sx: 1, sy: 1), Hops(1.6f, 3.1f, .18f, 14), K(3.4f, y: 0), K(3.6f, y: -30, sx: .92f, sy: 1.08f),
            K(3.75f, y: 0), K(4.3f, y: 0), K(4.5f, y: -40, sx: .9f, sy: 1.1f), K(4.7f, y: 0, sx: 1.1f, sy: .9f), K(4.8f, sx: 1, sy: 1)));
        HeroFaces(m.face, 0f, "Z", 1.3f, "E", 1.6f, "A", 3.45f, "H");

        Bang("!", 540, 150, 46, 62, Hex("E53935"), false, 1.3f, 2.0f, 40);
        Flash(1.5f, 3.3f, Hex("FF4D4D"), -60, -40, 420, 480, .36f, z: 5);
        Flash(1.68f, 3.3f, Hex("3D7BFF"), 240, -40, 420, 480, .36f, z: 5);
        SpeechBubble("Stop, thief!", 330, 130, 1.65f, 2.6f, Hex("E53935"));
        SpeechBubble("Got it back!", 250, 120, 4.2f, 4.95f, Hex("3DBE6B"));
        Sparkle(80, 240, 4.2f, Hex("FFC93C"), 30);
        Sparkle(120, 210, 4.35f, Color.white);
        Sparkle(60, 200, 4.5f, Hex("FFC93C"));
    }

    private void BuildBumped()
    {
        if (cardBack != null) cardBack.color = Hex("FFF8E7");
        Actor mat = Ground(Hex("FFE9C2"), 300);
        for (int i = 0; i < 7; i++) Box(Hex("F4D49C"), i * 90, 0, 2, 100, mat.rt);
        Box(Hex("F4D49C"), 0, 40, W, 2, mat.rt);

        Actor tower = Group(420, 180, 56, 150, origin: new Vector2(0.5f, 1f), z: 12);
        Img("sc_blockC", 0, 100, 56, 50, Color.white, tower.rt);
        Img("sc_blockB", 0, 50, 56, 50, Color.white, tower.rt);
        Actor top = Img("sc_blockA", 420, 180, 56, 50, origin: new Vector2(0.5f, 1f), z: 13);
        Anim(tower, K(1.05f, r: 0), K(1.2f, r: 7), K(1.4f, r: -5), K(1.6f, r: 3), K(1.8f, r: -1), K(2.0f, r: 0));
        Anim(top, K(1.05f, x: 0, y: 0, r: 0), K(1.2f, x: 8, y: 0, r: 12), K(1.35f, x: 40, y: -20, r: 50, e: Ease.In), K(1.75f, x: 110, y: 100, r: 90),
            K(1.9f, x: 118, y: 84, r: 90), K(2.05f, x: 122, y: 100, r: 90), K(3.5f, x: 122, y: 100, r: 90), K(3.8f, x: 60, y: -60, r: 20, e: Ease.Out),
            K(4.1f, x: 0, y: 0, r: 0), K(4.2f, sy: .85f), K(4.3f, sy: 1));

        Actor lines = Group(0, 250, 80, 40, z: 10);
        Box(Hex("F4C27A"), 0, 0, 60, 6, lines.rt, round: true);
        Box(Hex("F4C27A"), 20, 16, 40, 6, lines.rt, round: true);
        Box(Hex("F4C27A"), 0, 32, 60, 6, lines.rt, round: true);
        Anim(lines, K(0.2f, x: 0, o: 0), K(0.4f, o: 1, x: 100), K(1.0f, o: 1, x: 260), K(1.1f, o: 0));

        Actor imp = Img("fx_impact", 372, 200, 90, 90, z: 30);
        Anim(imp, K(0, o: 0, s: .1f), K(1.05f, o: 0, s: .1f), K(1.12f, o: 1, s: 1.3f, r: 15), K(1.3f, o: 1, s: 1, r: 0), K(1.5f, o: 0, s: 1.4f));

        Hero m = ScenarioBot("hat_hard", "H");
        Anim(m.outer, K(0, x: -70, y: 330, e: Ease.In), K(1.05f, x: 362), K(1.12f, x: 368, e: Ease.Out), K(1.45f, x: 270, r: -12), K(1.7f, r: 8),
            K(1.95f, r: -8), K(2.2f, r: 7), K(2.45f, r: -6), K(2.7f, r: 5), K(2.95f, r: 0), K(3.05f, x: 264), K(3.12f, x: 276), K(3.19f, x: 264),
            K(3.26f, x: 276), K(3.33f, x: 270), K(4.6f, x: 270));
        Anim(m.inner, K(1.05f, sx: 1, sy: 1), K(1.12f, sx: .78f, sy: 1.12f), K(1.3f, sx: 1.08f, sy: .94f), K(1.45f, sx: 1, sy: 1), K(4.3f, y: 0),
            K(4.5f, y: -44, sx: .9f, sy: 1.1f), K(4.7f, y: 0, sx: 1.1f, sy: .9f), K(4.8f, sx: 1, sy: 1));
        HeroFaces(m.face, 0f, "H", 1.1f, "D", 3.05f, "N", 4.15f, "H");
        Orbit(m.inner.rt, m.w / 2, -m.h * 0.08f, 68, 20, 3, 1.2f, 2.95f, 24, 0.9f);

        SpeechBubble("Bonk!", 330, 130, 1.1f, 2.0f, Hex("FF8A5B"));
        SpeechBubble("Oops!", 150, 140, 2.1f, 3.0f, Hex("9B6BFF"));
        Sparkle(448, 170, 4.15f, Hex("FFC93C"), 30);
        Sparkle(480, 200, 4.25f, Hex("3DBE6B"));
        Sparkle(410, 200, 4.35f, Hex("FF7EB6"));
        SpeechBubble("All fixed!", 360, 90, 4.3f, 4.95f, Hex("3DBE6B"));
    }

    // =========================================================
    // COLOURS
    // =========================================================

    private static readonly Color Ink = Hex("2E2A4F");

    private static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        return c;
    }
}
