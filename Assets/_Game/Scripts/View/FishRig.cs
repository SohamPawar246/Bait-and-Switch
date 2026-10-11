using UnityEngine;

namespace BaitSwitch.View
{
    /// <summary>
    /// The animatable joints of an assembled fish (no Update of its own: SeaView drives every rig in one loop).
    /// Axes are captured in each part's parent space at build time, so a tail always beats about the body's vertical
    /// axis and fins flap about its length, whatever orientation the Blender sockets were exported with.
    /// </summary>
    public sealed class FishRig : MonoBehaviour
    {
        public Transform model, tail, finL, finR;
        public Quaternion modelBase, tailBase, finLBase, finRBase;
        public Vector3 modelPos, tailAxis, finAxisL, finAxisR;

        // runtime state, owned by SeaView
        [System.NonSerialized] public float yaw, pitch, phase, speed, bubbleT;
        [System.NonSerialized] public Vector2 last;
        [System.NonSerialized] public bool right = true, init;

        public void Bind(Transform model, Transform tail, Transform finL, Transform finR)
        {
            this.model = model; this.tail = tail; this.finL = finL; this.finR = finR;
            modelBase = model.localRotation;
            modelPos = model.localPosition;
            Vector3 up = model.TransformDirection(Vector3.up), length = model.TransformDirection(Vector3.right);
            if (tail != null) { tailBase = tail.localRotation; tailAxis = tail.parent.InverseTransformDirection(up).normalized; }
            if (finL != null) { finLBase = finL.localRotation; finAxisL = finL.parent.InverseTransformDirection(length).normalized; }
            if (finR != null) { finRBase = finR.localRotation; finAxisR = finR.parent.InverseTransformDirection(length).normalized; }
        }
    }
}
