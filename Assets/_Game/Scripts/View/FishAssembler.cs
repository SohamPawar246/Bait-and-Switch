using System.Collections.Generic;
using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>
    /// Builds a fish from 21 appearance bytes using the Blender kit (docs/02, tools/blender/CLAUDE.md contracts):
    /// body with socket_* children; eyes part = root with eye_L/R/C children that move onto socket_eye_L/R/C;
    /// fins at fin_L/fin_R, tail, mouth, special at its socket. Colours: SkinColor tints Fill, GlowColor tints glows.
    /// The model is wrapped in a child turned 180° about Y so the fish faces +X like the sim (Blender +X → Unity −X).
    /// </summary>
    public static class FishAssembler
    {
        public static string Canon(string n)
        {
            int dot = n.IndexOf('.');
            if (dot >= 0) n = n.Substring(0, dot);
            if (n.EndsWith("(Clone)")) n = n.Substring(0, n.Length - 7);
            while (n.EndsWith("_x")) n = n.Substring(0, n.Length - 2);
            return n;
        }

        public static PartDef Part(byte[] a, Slot s)
        {
            var cat = PartCatalog.I;
            if (cat == null) return null;
            byte id = a != null && a.Length > (int)s ? a[(int)s] : (byte)0;
            return cat.Find(s, id) ?? cat.Find(s, 0);
        }

        public static Color Tint(byte[] a, Slot s, Color fallback)
        {
            var p = Part(a, s);
            return p != null && p.cosmetic ? p.tint : fallback;
        }

        public static string SpecialSocket(PartDef special)
        {
            if (special == null || special.prefab == null) return null;
            string n = Canon(special.prefab.name);
            if (n.Contains("hammerhead")) return "socket_front";
            if (n.Contains("tentacles")) return "socket_mouth";
            if (n.Contains("ink_gland")) return "socket_belly";
            return "socket_top";
        }

        public static Dictionary<string, Transform> Sockets(GameObject body)
        {
            var d = new Dictionary<string, Transform>();
            foreach (var t in body.GetComponentsInChildren<Transform>(true))
            {
                string c = Canon(t.name);
                if (c.StartsWith("socket_") && !d.ContainsKey(c)) d[c] = t;
            }
            return d;
        }

        /// <summary>Assemble under parent. Returns the fish root (its localScale is the fish scale).</summary>
        public static GameObject Build(byte[] a, Transform parent, string name = "Fish")
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            model.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var bodyDef = Part(a, Slot.Body);
            if (bodyDef == null || bodyDef.prefab == null)
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.Destroy(fallback.GetComponent<Collider>());
                fallback.transform.SetParent(model, false);
                fallback.transform.localRotation = Quaternion.Euler(0, 0, 90);
                fallback.transform.localScale = Vector3.one * 0.45f;
                root.AddComponent<FishRig>().Bind(model, null, null, null);
                return root;
            }
            var body = Object.Instantiate(bodyDef.prefab, model);
            body.name = "Body";
            var sockets = Sockets(body);

            AttachEyes(Part(a, Slot.Eyes), sockets);
            var fins = Part(a, Slot.Fins);
            var finL = Attach(fins, sockets, "socket_fin_L");
            var finR = Attach(fins, sockets, "socket_fin_R");
            var tail = Attach(Part(a, Slot.Tail), sockets, "socket_tail");
            Attach(Part(a, Slot.Mouth), sockets, "socket_mouth");
            var special = Part(a, Slot.Special);
            Attach(special, sockets, SpecialSocket(special));

            Tune(body.transform, model, tail, finL, finR);

            Color skin = Tint(a, Slot.SkinColor, new Color(1f, 0.55f, 0.48f));
            Color glow = Tint(a, Slot.GlowColor, new Color(0.6f, 0.96f, 0.77f));
            Color eye = Tint(a, Slot.EyeColor, Color.white);
            Color eyeTint = eye == Color.white ? default : eye;
            ToonLibrary.Apply(root, skin, glow, ToonLook.Plain(0.016f), eyeTint);
            // fins and tail: lighter membranes with ribs; body: painted belly gradient + the chosen pattern
            Color membrane = Color.Lerp(skin, Color.white, 0.18f);
            foreach (var part in new[] { finL, finR, tail })
                if (part != null) ToonLibrary.Apply(part, membrane, glow, ToonLook.Fin(), eyeTint);
            var bodyRenderer = body.GetComponent<MeshRenderer>();
            if (bodyRenderer != null)
            {
                var look = ToonLook.Body(PatternIndex(Part(a, Slot.Pattern)), Color.Lerp(skin, UnityEngine.Color.black, 0.55f));
                var src = bodyDef.prefab.GetComponent<MeshRenderer>().sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    // armour plates and ridges: a deeper shade of the fish's own skin, not black ink
                    bool detail = src[i] != null && src[i].name.StartsWith("Detail");
                    dst[i] = detail ? ToonLibrary.Get(Color.Lerp(skin, new Color(0.12f, 0.08f, 0.16f), 0.4f), Color.black, ToonLook.Fin())
                                    : ToonLibrary.Map(src[i], skin, glow, look, eyeTint);
                }
                bodyRenderer.sharedMaterials = dst;
            }
            root.AddComponent<FishRig>().Bind(model, tail != null ? tail.transform : null,
                                              finL != null ? finL.transform : null, finR != null ? finR.transform : null);
            return root;
        }

        /// <summary>
        /// Natural proportions over the raw kit: the tail's joint peg sinks into the body so the fan grows out of it,
        /// the tail is a touch smaller, and the side fins shrink and sweep back toward the tail (foreshortened, angled
        /// off the flank) so they read as fins rather than stickers. Runs before the rig captures its rest pose.
        /// </summary>
        static void Tune(Transform body, Transform model, GameObject tail, GameObject finL, GameObject finR)
        {
            if (tail != null)
            {
                var t = tail.transform;
                Vector3 p = body.InverseTransformPoint(t.position);
                t.position = body.TransformPoint(Vector3.MoveTowards(p, new Vector3(0f, p.y * 0.5f, 0f), 0.09f));
                t.localScale *= 0.9f;
            }
            Vector3 up = model.TransformDirection(Vector3.up);
            Vector3 toTail = model.TransformDirection(Vector3.right);    // the head is model −X, the tail +X
            foreach (var fin in new[] { finL, finR })
            {
                if (fin == null) continue;
                var t = fin.transform;
                t.localScale *= 0.9f;
                float side = Mathf.Sign(model.InverseTransformPoint(t.position).z);
                if (side == 0f) side = 1f;
                // sweep back about the vertical, then droop a little about the length axis
                Quaternion sweep = Quaternion.AngleAxis(20f * side, up) * Quaternion.AngleAxis(-10f * side, toTail);
                t.rotation = sweep * t.rotation;
                // sit the fin base slightly into the flank and back from the gill line
                t.position += toTail * 0.04f - model.TransformDirection(new Vector3(0f, 0.02f, side * 0.03f));
            }
        }

        public static int PatternIndex(PartDef p)
        {
            if (p == null) return 0;
            string n = (p.displayName ?? "").ToLowerInvariant();
            if (n.Contains("stripe")) return 1;
            if (n.Contains("spot")) return 2;
            if (n.Contains("freckle")) return 3;
            if (n.Contains("scar")) return 4;
            return 0;
        }

        public static GameObject Attach(PartDef p, Dictionary<string, Transform> sockets, string socket)
        {
            if (p == null || p.prefab == null || socket == null || !sockets.TryGetValue(socket, out var s)) return null;
            var go = Object.Instantiate(p.prefab, s);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        public static void AttachEyes(PartDef eyes, Dictionary<string, Transform> sockets)
        {
            if (eyes == null || eyes.prefab == null) return;
            var inst = Object.Instantiate(eyes.prefab);
            var kids = new List<Transform>();
            foreach (Transform c in inst.transform) kids.Add(c);
            foreach (var c in kids)
            {
                string key = Canon(c.name);                       // eye_L / eye_R / eye_C
                if (!sockets.TryGetValue("socket_" + key, out var s)) continue;
                // keep the child's own (FBX axis-conversion) rotation relative to the eyes root, like Blender's assembly
                Quaternion rel = c.localRotation;
                c.SetParent(s, false);
                c.localPosition = Vector3.zero;
                c.localRotation = rel;
                c.localScale = Vector3.one;
                c.name = key;
                // pupils are grandchildren in the FBX: the importer leaves them in Blender axes with a 90° X fix-up,
                // while the eyeball mesh is already in Unity axes. Convert them so they sit on the eye's +Y face.
                foreach (Transform pupil in c)
                {
                    Vector3 lp = pupil.localPosition;
                    if (Mathf.Abs(lp.z) > Mathf.Abs(lp.y))
                    {
                        pupil.localPosition = new Vector3(-lp.x, lp.z, -lp.y);
                        pupil.localRotation = Quaternion.identity;
                    }
                }
            }
            Object.Destroy(inst);
        }
    }

    /// <summary>Angler dinghy (boat + hooded figure + hat + rod + lantern) and hooks from the kit.</summary>
    public static class KitBuilder
    {
        public static readonly Color Raincoat = ToonLibrary.Hex("#F2C14E");

        /// <summary>Boat model under parent; its local origin sits on the waterline under the rod tip.</summary>
        public static GameObject Boat(Transform parent, int seat, Color coat, Color paint, int hat = -1)
        {
            var lib = ArtLibrary.I;
            var root = new GameObject("Dinghy");
            root.transform.SetParent(parent, false);
            if (lib == null || lib.boat == null) return root;
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            model.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.localPosition = new Vector3(0.3f, -0.05f, 0f);

            var boat = Object.Instantiate(lib.boat, model);
            ToonLibrary.Apply(boat, paint, Color.white);
            var sockets = FishAssembler.Sockets(boat);

            if (lib.raincoat != null && sockets.TryGetValue("socket_seat", out var seatT))
            {
                var coatGo = Object.Instantiate(lib.raincoat, seatT);
                // the coat's front lands on the boat's camera side: the void and its pupils face the player
                coatGo.transform.localPosition = Vector3.zero;
                coatGo.transform.localRotation = Quaternion.identity;
                coatGo.transform.localScale = Vector3.one * 0.85f;
                ToonLibrary.Apply(coatGo, coat, coat);
                if (lib.hats != null && lib.hats.Length > 0)
                {
                    int h = hat >= 0 ? hat : seat % (lib.hats.Length + 1);
                    if (h < lib.hats.Length && lib.hats[h] != null)
                    {
                        var hatGo = Object.Instantiate(lib.hats[h], coatGo.transform);
                        hatGo.transform.localPosition = new Vector3(0f, 0.93f, 0f);
                        hatGo.transform.localScale = Vector3.one * 0.95f;
                        ToonLibrary.Apply(hatGo, Color.Lerp(coat, Color.black, 0.15f), coat);
                    }
                }
            }
            if (lib.lantern != null && sockets.TryGetValue("socket_bow", out var bow))
            {
                var l = Object.Instantiate(lib.lantern, bow);
                l.transform.localScale = Vector3.one * 1.6f;
                ToonLibrary.Apply(l, Color.white, Raincoat);
            }
            // rod: base in the boat, tip at the sim's rod tip (root origin + rodHeight)
            if (lib.rods != null && lib.rods.Length > 0 && lib.rods[0] != null)
            {
                var c = GameConfig.I;
                var rod = Object.Instantiate(lib.rods[seat % lib.rods.Length], root.transform);
                Vector2 tip = new Vector2(0f, c.rodHeight), baseP = new Vector2(-0.38f, 0.05f);
                Vector2 dir = tip - baseP;
                rod.transform.localPosition = new Vector3(baseP.x, baseP.y, -0.1f);
                rod.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(dir.x, dir.y, 0f));
                rod.transform.localScale = Vector3.one * (dir.magnitude / 1.4f);
                ToonLibrary.Apply(rod, ToonLibrary.Hex("#3B2C22"), Raincoat);
            }
            return root;
        }

        public static GameObject Hook(Transform parent, int hookIndex, int baitIndex, out GameObject bait)
        {
            var lib = ArtLibrary.I;
            var root = new GameObject("Hook");
            root.transform.SetParent(parent, false);
            bait = null;
            if (lib == null || lib.hooks == null || lib.hooks.Length == 0) return root;
            var hook = Object.Instantiate(lib.hooks[Mathf.Abs(hookIndex) % lib.hooks.Length], root.transform);
            hook.transform.localScale = Vector3.one * 1.8f;
            ToonLibrary.Apply(hook, ToonLibrary.Hex("#B9BEC6"), Color.white, 0.006f);
            if (lib.baits != null && lib.baits.Length > 0)
            {
                bait = Object.Instantiate(lib.baits[Mathf.Abs(baitIndex) % lib.baits.Length], root.transform);
                bait.name = "Bait";
                bait.transform.localPosition = new Vector3(0f, -0.2f, 0f);
                bait.transform.localScale = Vector3.one * 1.8f;
                ToonLibrary.Apply(bait, ToonLibrary.Hex("#E07A8A"), ToonLibrary.Hex("#9BF6C4"), 0.006f);
            }
            return root;
        }
    }
}
