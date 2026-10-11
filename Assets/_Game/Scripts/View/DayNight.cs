using BaitSwitch.Net;
using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>
    /// Greybox day → dusk → night from the synced round clock: sun colour/intensity, sky and water tint.
    /// The real lighting, fog and caustics arrive with the art pass (step 7).
    /// </summary>
    public sealed class DayNight : MonoBehaviour
    {
        public Light sun;
        public Camera cam;
        public Renderer seaBackdrop;
        public Color daySun = new Color(1f, 0.96f, 0.88f), duskSun = new Color(1f, 0.55f, 0.3f), nightSun = new Color(0.35f, 0.45f, 0.8f);
        public float dayIntensity = 1.1f, duskIntensity = 0.75f, nightIntensity = 0.25f;
        public Color daySea = new Color(0.06f, 0.18f, 0.26f), duskSea = new Color(0.09f, 0.11f, 0.2f), nightSea = new Color(0.02f, 0.03f, 0.07f);
        public Color daySky = new Color(0.02f, 0.06f, 0.1f), nightSky = new Color(0.005f, 0.01f, 0.02f);
        [Header("Sea backdrop shader (top of water / sky)")]
        public Color dayWaterTop = new Color(0.2f, 0.52f, 0.52f), duskWaterTop = new Color(0.45f, 0.32f, 0.3f), nightWaterTop = new Color(0.07f, 0.12f, 0.22f);
        public Color daySkyTop = new Color(0.42f, 0.66f, 0.78f), duskSkyTop = new Color(0.36f, 0.25f, 0.42f), nightSkyTop = new Color(0.02f, 0.03f, 0.08f);
        public Color daySkyLow = new Color(0.93f, 0.92f, 0.82f), duskSkyLow = new Color(1f, 0.6f, 0.35f), nightSkyLow = new Color(0.1f, 0.12f, 0.25f);

        SeaClient _client;
        MaterialPropertyBlock _mpb;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int WaterTop = Shader.PropertyToID("_WaterTop"), WaterBottom = Shader.PropertyToID("_WaterBottom"),
            SkyTop = Shader.PropertyToID("_SkyTop"), SkyHorizon = Shader.PropertyToID("_SkyHorizon"), Stars = Shader.PropertyToID("_Stars"),
            RayStrength = Shader.PropertyToID("_RayStrength"), CausticStrength = Shader.PropertyToID("_CausticStrength"),
            MoonColor = Shader.PropertyToID("_MoonColor"), MoonPos = Shader.PropertyToID("_MoonPos");

        static Color Tri(Color a, Color b, Color c, float k) => k < 1f ? Color.Lerp(a, b, k) : Color.Lerp(b, c, k - 1f);

        static readonly int GSurface = Shader.PropertyToID("_BS_SurfaceY"), GSeabed = Shader.PropertyToID("_BS_SeabedY"),
            GFog = Shader.PropertyToID("_BS_FogAmount"), GCaustics = Shader.PropertyToID("_BS_Caustics"),
            GFogTop = Shader.PropertyToID("_BS_FogTop"), GFogDeep = Shader.PropertyToID("_BS_FogDeep");

        void Awake() { _client = GetComponent<SeaClient>(); }

        void OnDisable() { Shader.SetGlobalFloat(GFog, 0f); Shader.SetGlobalFloat(GCaustics, 0f); }

        void LateUpdate()
        {
            var c = GameConfig.I;
            bool sea = c != null && _client != null && Mirror.NetworkClient.active && UI.AppFlow.SeaOnScreen;
            if (!sea)
            {
                // the menu, workbench and loading line render toon models too: no sea fog there
                Shader.SetGlobalFloat(GFog, 0f);
                Shader.SetGlobalFloat(GCaustics, 0f);
                return;
            }
            float t = (MatchState)_client.Match.state == MatchState.Playing ? _client.MatchTime : 0f;
            // 0 = day, 1 = dusk, 2 = night, blended over 5 s around each boundary.
            float k = Mathf.Clamp01((t - c.duskStart + 2.5f) / 5f) + Mathf.Clamp01((t - c.nightStart + 2.5f) / 5f);
            Color s = k < 1f ? Color.Lerp(daySun, duskSun, k) : Color.Lerp(duskSun, nightSun, k - 1f);
            Color w = k < 1f ? Color.Lerp(daySea, duskSea, k) : Color.Lerp(duskSea, nightSea, k - 1f);
            float i = k < 1f ? Mathf.Lerp(dayIntensity, duskIntensity, k) : Mathf.Lerp(duskIntensity, nightIntensity, k - 1f);
            if (sun != null) { sun.color = s; sun.intensity = i; }
            if (cam != null) cam.backgroundColor = Color.Lerp(daySky, nightSky, k * 0.5f);
            if (seaBackdrop != null)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                seaBackdrop.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColor, w);
                _mpb.SetColor(WaterTop, Tri(dayWaterTop, duskWaterTop, nightWaterTop, k));
                _mpb.SetColor(WaterBottom, w * 0.45f);
                _mpb.SetColor(SkyTop, Tri(daySkyTop, duskSkyTop, nightSkyTop, k));
                _mpb.SetColor(SkyHorizon, Tri(daySkyLow, duskSkyLow, nightSkyLow, k));
                _mpb.SetFloat(Stars, Mathf.Clamp01(k - 1f));
                _mpb.SetFloat(RayStrength, Mathf.Lerp(0.38f, 0.06f, k * 0.5f));
                _mpb.SetFloat(CausticStrength, Mathf.Lerp(0.35f, 0.04f, k * 0.5f));
                _mpb.SetColor(MoonColor, k < 1.5f ? new Color(1f, 0.9f, 0.7f) * (1.2f - k * 0.3f) : new Color(0.85f, 0.9f, 1f));
                _mpb.SetVector(MoonPos, new Vector4(Mathf.Lerp(0.18f, 0.82f, Mathf.Clamp01(t / c.roundSeconds)), 0.92f - 0.06f * Mathf.Sin(Mathf.Clamp01(t / c.roundSeconds) * Mathf.PI), 0, 0));
                seaBackdrop.SetPropertyBlock(_mpb);
            }
            // globals for the toon/FX shaders: depth fog, caustics on bodies, the waterline
            Color top = Tri(dayWaterTop, duskWaterTop, nightWaterTop, k);
            Shader.SetGlobalFloat(GSurface, c.surfaceY);
            Shader.SetGlobalFloat(GSeabed, c.seabedY);
            Shader.SetGlobalFloat(GFog, Mathf.Lerp(0.75f, 1f, k * 0.5f));
            Shader.SetGlobalFloat(GCaustics, Mathf.Lerp(0.9f, 0.12f, k * 0.5f));
            Shader.SetGlobalColor(GFogTop, top);
            Shader.SetGlobalColor(GFogDeep, w * 0.55f);
        }
    }
}
