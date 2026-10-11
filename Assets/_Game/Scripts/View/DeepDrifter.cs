using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>
    /// "The far background: enormous silhouettes drifting very slowly. Never explained."
    /// An assembled fish, scaled huge and placed deep behind the sea, crossing it over minutes with a lazy tail beat.
    /// The toon shader's depth fog turns it into a shape you are never quite sure you saw. Clients only.
    /// </summary>
    public sealed class DeepDrifter : MonoBehaviour
    {
        public int seed = 31;
        public float z = 30f, y = -11f, scale = 10f, speed = 0.3f, startX = -20f, direction = 1f, range = 55f;

        Transform _fish;
        FishRig _rig;
        float _x, _phase;

        void Start()
        {
            if (Application.isBatchMode || PartCatalog.I == null || ArtLibrary.I == null) { enabled = false; return; }
            var a = PartCatalog.I.RandomAppearance(new System.Random(seed));
            _fish = FishAssembler.Build(a, transform, "Silhouette").transform;
            _fish.localScale = Vector3.one * scale;
            // one dark ink for every part: the murk turns it into a shape, not a pale cut-out
            var look = ToonLook.Plain(0f);
            look.gloss = 0f;
            ToonLibrary.Apply(_fish.gameObject, new Color(0.03f, 0.06f, 0.09f), Color.black, look, new Color(0.03f, 0.06f, 0.09f));
            _rig = _fish.GetComponent<FishRig>();
            _x = startX;
        }

        void Update()
        {
            if (_fish == null) return;
            float dt = Time.deltaTime;
            _x += speed * direction * dt;
            if (_x > range) _x = -range; else if (_x < -range) _x = range;
            _phase += dt * 1.1f;
            _fish.SetPositionAndRotation(new Vector3(_x, y + Mathf.Sin(_phase * 0.18f) * 1.2f, z),
                                         Quaternion.Euler(0f, direction > 0f ? 0f : 180f, Mathf.Sin(_phase * 0.23f) * 4f));
            if (_rig != null && _rig.tail != null) _rig.tail.localRotation = Quaternion.AngleAxis(Mathf.Sin(_phase) * 16f, _rig.tailAxis) * _rig.tailBase;
        }
    }
}
