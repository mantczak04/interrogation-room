using UnityEngine;

namespace InterrogationRoom.Graphics
{
    /// <summary>
    /// Keeps wall texturing at a constant, isotropic density.
    ///
    /// The graybox walls are scaled cubes, so every face carries the same 0..1 UV
    /// no matter how big the wall actually is. A single tiling value on the shared
    /// material therefore stretched the plaster differently on every segment: the
    /// corridor ran at 448 px/m with a 2.29 x 0.80 m tile (2.9x horizontal stretch)
    /// while the wainscot ran at 7.6x stretch, and density across the level varied
    /// by 12x. That reads as smeared blotches under a grazing light.
    ///
    /// This drives the tiling from the renderer's world size instead, through a
    /// MaterialPropertyBlock so the shared material asset stays untouched and
    /// resizing a wall in the editor re-fits it immediately.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class WallTextureDensity : MonoBehaviour
    {
        [SerializeField, Min(0.05f)]
        [Tooltip("World size of one texture tile, in metres. Lower means a denser, finer pattern.")]
        private float metresPerTile = 1.2f;

        private static MaterialPropertyBlock block;

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            metresPerTile = Mathf.Max(0.05f, metresPerTile);
            Apply();
        }

        private void Apply()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                return;

            Vector3 size = meshRenderer.bounds.size;

            // Walls are thin slabs: the thickness axis is the smallest one and must
            // never become a UV axis, otherwise the tile collapses to a few pixels.
            float width = Mathf.Max(size.x, size.z);
            float height = size.y;
            if (height < Mathf.Min(size.x, size.z))
            {
                // Horizontal surface (floor or ceiling slab): use the two long axes.
                width = Mathf.Max(size.x, size.z);
                height = Mathf.Min(size.x, size.z);
            }

            var tiling = new Vector4(
                Mathf.Max(width / metresPerTile, 0.01f),
                Mathf.Max(height / metresPerTile, 0.01f),
                0f,
                0f);

            block ??= new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(block);
            block.SetVector("_BaseMap_ST", tiling);
            block.SetVector("_BumpMap_ST", tiling);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}
