using System.Collections.Generic;
using ArcCreate.Gameplay.Utility;
using UnityEngine;

namespace ArcCreate.Gameplay.Render
{
    public class RenderService : MonoBehaviour, IRenderService
    {
        [SerializeField] private Camera notesCamera;
        [SerializeField] private int layer;

        [Header("Meshes")]
        [SerializeField] private Mesh arctapMesh;
        [SerializeField] private Mesh arctapSfxMesh;
        [SerializeField] private Mesh arctapShadowMesh;
        [SerializeField] private Mesh tapMesh;
        [SerializeField] private Mesh holdMesh;
        [SerializeField] private Mesh connectionLineMesh;
        [SerializeField] private Mesh heightIndicatorMesh;
        [SerializeField] private Mesh arcCapMesh;

        [Header("Materials")]
        [SerializeField] private Material baseArctapMaterial;
        [SerializeField] private Material baseTapMaterial;
        [SerializeField] private Material baseHoldMaterial;
        [SerializeField] private Material baseArcCapMaterial;
        [SerializeField] private Material arctapShadowMaterial;
        [SerializeField] private Material heightIndicatorMaterial;
        [SerializeField] private Material connectionLineMaterial;

        private readonly List<Material> generatedMaterials = new List<Material>();

        // Floor
        private readonly Dictionary<Texture, InstancedRendererPool> tapDrawers = new Dictionary<Texture, InstancedRendererPool>();
        private readonly Dictionary<Texture, InstancedRendererPool> holdDrawers = new Dictionary<Texture, InstancedRendererPool>();
        private InstancedRendererPool connectionLineDrawer;

        // Arc & trace
        private InstancedRendererPool arcSegmentDrawer;
        private InstancedRendererPool arcHeadDrawer;
        private InstancedRendererPool arcSegmentHighlightDrawer;
        private InstancedRendererPool arcHeadHighlightDrawer;
        private readonly Dictionary<Texture, InstancedRendererPool> arcCapDrawers = new Dictionary<Texture, InstancedRendererPool>();
        private InstancedRendererPool arcShadowDrawer;
        private InstancedRendererPool traceSegmentDrawer;
        private InstancedRendererPool traceHeadDrawer;
        private InstancedRendererPool traceShadowDrawer;
        private InstancedRendererPool heightIndicatorDrawer;

        // Arctap
        private readonly Dictionary<Texture, InstancedRendererPool> arctapDrawers = new Dictionary<Texture, InstancedRendererPool>();
        private readonly Dictionary<Texture, InstancedRendererPool> arctapSfxDrawers = new Dictionary<Texture, InstancedRendererPool>();
        private InstancedRendererPool arctapShadowDrawer;

        public bool IsLoaded { get; private set; }

        public Mesh TapMesh => tapMesh;

        public Mesh HoldMesh => holdMesh;

        public Mesh ArcTapMesh => arctapMesh;

        public void DrawTap(Texture texture, Matrix4x4 matrix, Color color, bool selected)
        {
            if (!tapDrawers.ContainsKey(texture))
            {
                Material newTap = Instantiate(baseTapMaterial);
                newTap.mainTexture = texture;
                generatedMaterials.Add(newTap);
                tapDrawers.Add(texture, new InstancedRendererPool(
                    newTap,
                    tapMesh,
                    true));
            }

            tapDrawers[texture].RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, 0, 0, 0));
        }

        public void DrawHold(Texture texture, Matrix4x4 matrix, Color color, bool selected, float from)
        {
            if (!holdDrawers.ContainsKey(texture))
            {
                Material newHold = Instantiate(baseHoldMaterial);
                newHold.mainTexture = texture;
                generatedMaterials.Add(newHold);
                holdDrawers.Add(texture, new InstancedRendererPool(
                    newHold,
                    holdMesh,
                    true));
            }

            holdDrawers[texture].RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, from, 0, 0));
        }

        public void DrawConnectionLine(Matrix4x4 matrix, Color color)
        {
            connectionLineDrawer.RegisterInstance(matrix, color);
        }

        public void DrawArcSegment(int colorId, bool highlight, Matrix4x4 matrix, Color color, bool selected, float redValue, float y)
        {
            (Color high, Color low) = Services.Skin.GetArcColor(colorId);
            color *= Color.Lerp(Color.Lerp(low, high, (y - 1) / 4.5f), Color.red, redValue);
            Vector4 properties = new Vector4(selected ? 1 : 0, 0, 0, 0);

            if (highlight)
            {
                arcSegmentHighlightDrawer.RegisterInstance(matrix, color, properties);
            }
            else
            {
                arcSegmentDrawer.RegisterInstance(matrix, color, properties);
            }
        }

        public void DrawTraceSegment(Matrix4x4 matrix, Color color, bool selected)
        {
            traceSegmentDrawer.RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, 0, 0, 0));
        }

        public void DrawArcShadow(Matrix4x4 matrix, Color color)
        {
            arcShadowDrawer.RegisterInstance(matrix, color);
        }

        public void DrawTraceShadow(Matrix4x4 matrix, Color color)
        {
            traceShadowDrawer.RegisterInstance(matrix, color);
        }

        public void DrawArcHead(int colorId, bool highlight, Matrix4x4 matrix, Color color, bool selected, float redValue, float y)
        {
            (Color high, Color low) = Services.Skin.GetArcColor(colorId);
            color *= Color.Lerp(Color.Lerp(low, high, (y - 1) / 4.5f), Color.red, redValue);
            Vector4 properties = new Vector4(selected ? 1 : 0, 0, 0, 0);

            if (highlight)
            {
                arcHeadHighlightDrawer.RegisterInstance(matrix, color, properties);
            }
            else
            {
                arcHeadDrawer.RegisterInstance(matrix, color, properties);
            }
        }

        public void DrawTraceHead(Matrix4x4 matrix, Color color, bool selected)
        {
            traceHeadDrawer.RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, 0, 0, 0));
        }

        public void DrawArcCap(Texture texture, Matrix4x4 matrix, Color color)
        {
            if (!arcCapDrawers.ContainsKey(texture))
            {
                Material newArcCap = Instantiate(baseArcCapMaterial);
                newArcCap.mainTexture = texture;
                generatedMaterials.Add(newArcCap);
                arcCapDrawers.Add(texture, new InstancedRendererPool(
                    newArcCap,
                    arcCapMesh,
                    false));
            }

            arcCapDrawers[texture].RegisterInstance(matrix, color);
        }

        public void DrawHeightIndicator(Matrix4x4 matrix, Color color)
        {
            heightIndicatorDrawer.RegisterInstance(matrix, color);
        }

        public void DrawArcTap(bool sfx, Texture texture, Matrix4x4 matrix, Color color, bool selected)
        {
            var drawer = sfx ? arctapSfxDrawers : arctapDrawers;
            if (!drawer.ContainsKey(texture))
            {
                Material newArctap = Instantiate(baseArctapMaterial);
                newArctap.mainTexture = texture;
                generatedMaterials.Add(newArctap);
                drawer.Add(texture, new InstancedRendererPool(
                    newArctap,
                    sfx ? arctapSfxMesh : arctapMesh,
                    true));
            }

            drawer[texture].RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, 0, 0, 0));
        }

        public void DrawArcTapShadow(Matrix4x4 matrix, Color color)
        {
            arctapShadowDrawer.RegisterInstance(matrix, color);
        }

        public void UpdateRenderers()
        {
            foreach (var pair in holdDrawers)
            {
                pair.Value.Draw(notesCamera, layer);
            }

            connectionLineDrawer.Draw(notesCamera, layer);
            foreach (var pair in tapDrawers)
            {
                pair.Value.Draw(notesCamera, layer);
            }

            traceShadowDrawer.Draw(notesCamera, layer);
            arcShadowDrawer.Draw(notesCamera, layer);

            traceSegmentDrawer.Draw(notesCamera, layer);
            traceHeadDrawer.Draw(notesCamera, layer);
            arctapShadowDrawer.Draw(notesCamera, layer);

            heightIndicatorDrawer.Draw(notesCamera, layer);
            foreach (var pair in arcCapDrawers)
            {
                pair.Value.Draw(notesCamera, layer);
            }

            arcSegmentDrawer.Draw(notesCamera, layer);
            arcSegmentHighlightDrawer.Draw(notesCamera, layer);

            foreach (var pair in arctapDrawers)
            {
                pair.Value.Draw(notesCamera, layer);
            }

            foreach (var pair in arctapSfxDrawers)
            {
                pair.Value.Draw(notesCamera, layer);
            }

            arcHeadDrawer.Draw(notesCamera, layer);
            arcHeadHighlightDrawer.Draw(notesCamera, layer);
        }

        public void SetTraceMaterial(Material material)
        {
            traceSegmentDrawer?.Dispose();
            traceSegmentDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetSegmentMesh(true),
                true);

            traceHeadDrawer?.Dispose();
            traceHeadDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetHeadMesh(true),
                true);

            UpdateLoadedState();
        }

        public void SetShadowMaterial(Material material)
        {
            arcShadowDrawer?.Dispose();
            arcShadowDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetShadowMesh(false),
                false);

            traceShadowDrawer?.Dispose();
            traceShadowDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetShadowMesh(true),
                false);

            UpdateLoadedState();
        }

        public void SetArcMaterial(Material normal, Material highlight)
        {
            arcSegmentDrawer?.Dispose();
            arcHeadDrawer?.Dispose();
            arcSegmentHighlightDrawer?.Dispose();
            arcHeadHighlightDrawer?.Dispose();

            arcSegmentDrawer = new InstancedRendererPool(
                normal,
                ArcMeshGenerator.GetSegmentMesh(false),
                true);

            arcHeadDrawer = new InstancedRendererPool(
                normal,
                ArcMeshGenerator.GetHeadMesh(false),
                true);

            arcSegmentHighlightDrawer = new InstancedRendererPool(
                highlight,
                ArcMeshGenerator.GetSegmentMesh(false),
                true);

            arcHeadHighlightDrawer = new InstancedRendererPool(
                highlight,
                ArcMeshGenerator.GetHeadMesh(false),
                true);

            UpdateLoadedState();
        }

        public void SetTextures(Texture heightIndicator, Texture arctapShadow)
        {
            heightIndicatorDrawer?.Dispose();
            arctapShadowDrawer?.Dispose();

            Material height = Instantiate(heightIndicatorMaterial);
            height.mainTexture = heightIndicator;
            generatedMaterials.Add(height);
            heightIndicatorDrawer = new InstancedRendererPool(
                height,
                heightIndicatorMesh,
                false);

            Material shadow = Instantiate(arctapShadowMaterial);
            shadow.mainTexture = arctapShadow;
            generatedMaterials.Add(shadow);
            arctapShadowDrawer = new InstancedRendererPool(
                shadow,
                arctapShadowMesh,
                false);

            UpdateLoadedState();
        }

        private void UpdateLoadedState()
        {
            IsLoaded = connectionLineDrawer != null
                    && arcSegmentDrawer != null
                    && arcHeadDrawer != null
                    && arcSegmentHighlightDrawer != null
                    && arcHeadHighlightDrawer != null
                    && traceSegmentDrawer != null
                    && traceHeadDrawer != null
                    && arcShadowDrawer != null
                    && traceShadowDrawer != null
                    && connectionLineDrawer != null
                    && heightIndicatorDrawer != null
                    && arctapShadowDrawer != null;
        }

        private void Awake()
        {
            connectionLineDrawer = new InstancedRendererPool(
                connectionLineMaterial,
                connectionLineMesh,
                false);

            UpdateLoadedState();
        }

        private void OnDestroy()
        {
            foreach (Material material in generatedMaterials)
            {
                Destroy(material);
            }

            generatedMaterials.Clear();

            foreach (var pair in holdDrawers)
            {
                pair.Value.Dispose();
            }

            connectionLineDrawer.Dispose();
            foreach (var pair in tapDrawers)
            {
                pair.Value.Dispose();
            }

            traceShadowDrawer.Dispose();
            arcShadowDrawer.Dispose();

            traceSegmentDrawer.Dispose();
            traceHeadDrawer.Dispose();
            arctapShadowDrawer.Dispose();

            heightIndicatorDrawer.Dispose();
            foreach (var pair in arcCapDrawers)
            {
                pair.Value.Dispose();
            }

            arcSegmentDrawer.Dispose();
            arcSegmentHighlightDrawer.Dispose();

            foreach (var pair in arctapDrawers)
            {
                pair.Value.Dispose();
            }

            foreach (var pair in arctapSfxDrawers)
            {
                pair.Value.Dispose();
            }

            arcHeadDrawer.Dispose();
            arcHeadHighlightDrawer.Dispose();
        }
    }
}