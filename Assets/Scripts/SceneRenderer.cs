using System;
using UnityEngine;
using Renderer = UnityEngine.Renderer;

[ExecuteInEditMode]
public class SceneRenderer : MonoBehaviour
{
    //public Camera DebugCamera;
    //public Camera ShadowCamera;
    //public RenderTexture ShadowMap;

    public Shader ContourShader;
    public Shader CompositeShader;
    public Shader RenderDnaShader;
    public Shader RenderLipidsShader;
    public Shader RenderProteinsShader;

    public ComputeShader ClearOcclusionFlagsCS;
    public ComputeShader CrossSectionCS;
    public ComputeShader BatchInstancesCS;
    public ComputeShader BrownianMotionCS;
    public ComputeShader FrustrumCullingCS;
    public ComputeShader RopeConstraintsCS;
    public ComputeShader OcclusionCullingCS;
    public ComputeShader ReadPixelCS;

    public RenderTexture MicroscopyTexture;
	public GameObject CanvasFluo;

    /*****/

    private Material _contourMaterial;
    private Material _compositeMaterial;
    private Material _renderDnaMaterial;
    private Material _renderLipidsMaterial;
    public Material RenderProteinsMaterial;

    /*****/

    private Camera _camera;
    private ComputeBuffer _argBuffer;
    private RenderTexture _HiZMap;

    /*****/

    private bool _rightMouseDown = false;
    private Vector2 _mousePos = new Vector2();

    /*****/

    void OnEnable()
    {
        this.hideFlags = HideFlags.None;

        _camera = GetComponent<Camera>();
        _camera.depthTextureMode |= DepthTextureMode.Depth;
        _camera.depthTextureMode |= DepthTextureMode.DepthNormals;

        if (RenderProteinsMaterial == null) RenderProteinsMaterial = new Material(RenderProteinsShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_renderLipidsMaterial == null) _renderLipidsMaterial = new Material(RenderLipidsShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_renderDnaMaterial == null) _renderDnaMaterial = new Material(RenderDnaShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_compositeMaterial == null) _compositeMaterial = new Material(CompositeShader) { hideFlags = HideFlags.HideAndDontSave };
        if (_contourMaterial == null) _contourMaterial = new Material(ContourShader) { hideFlags = HideFlags.HideAndDontSave };

        if (_argBuffer == null)
        {
            _argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.DrawIndirect);
            _argBuffer.SetData( new [] { 0, 1, 0, 0 });
        }
    }

    void OnDisable()
    {
        if (RenderProteinsMaterial != null) DestroyImmediate(RenderProteinsMaterial);
        if (_renderLipidsMaterial != null) DestroyImmediate(_renderLipidsMaterial);
        if (_renderDnaMaterial != null) DestroyImmediate(_renderDnaMaterial);
        if (_compositeMaterial != null) DestroyImmediate(_compositeMaterial);
        if (_contourMaterial != null) DestroyImmediate(_contourMaterial); 
        
        if (_HiZMap != null)
        {
            _HiZMap.Release();
            DestroyImmediate(_HiZMap);
            _HiZMap = null;
        }

        if (_argBuffer != null)
        {
            _argBuffer.Release();
            _argBuffer = null;
        }
    }

    private void OnGUI()
    {
        // Listen mouse click events
        //if (Event.current.type == EventType.MouseDown && Event.current.modifiers == EventModifiers.Control && Event.current.button == 0)
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            _rightMouseDown = true;
            _mousePos = Event.current.mousePosition;
        }
    }

    void SetShaderParams()
    {
        // Contour params
        _contourMaterial.SetInt("_ContourOptions", DisplaySettings.Instance.ContourOptions);
        _contourMaterial.SetFloat("_ContourStrength", DisplaySettings.Instance.ContourStrength);

        // Protein params
        RenderProteinsMaterial.SetInt("_EnableLod", Convert.ToInt32(DisplaySettings.Instance.EnableLod));
        RenderProteinsMaterial.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        RenderProteinsMaterial.SetFloat("_FirstLevelBeingRange", DisplaySettings.Instance.FirstLevelOffset);
        RenderProteinsMaterial.SetVector("_CameraForward", _camera.transform.forward);
        RenderProteinsMaterial.SetMatrix("_LodLevelsInfos", Helper.FloatArrayToMatrix4X4(DisplaySettings.Instance.LodLevels));
        RenderProteinsMaterial.SetMatrix("_FluorescenceColors", Helper.FloatArrayToMatrix4X4(PdbLoader.FluoColors));
        RenderProteinsMaterial.SetBuffer("_ProteinVisibilityFlag", ComputeBufferManager.Instance.ProteinVisibilityFlags);
        RenderProteinsMaterial.SetBuffer("_ProteinFluorescenceFlags", ComputeBufferManager.Instance.ProteinFluorescenceFlags);

        RenderProteinsMaterial.SetBuffer("_ProteinInstanceInfo", ComputeBufferManager.Instance.ProteinInstanceInfos);
        RenderProteinsMaterial.SetBuffer("_ProteinInstancePositions",
            (DisplaySettings.Instance.EnableBrownianMotion) ?
            ComputeBufferManager.Instance.InstanceDisplayPositions : ComputeBufferManager.Instance.ProteinInstancePositions);

        RenderProteinsMaterial.SetBuffer("_ProteinInstanceRotations",
            (DisplaySettings.Instance.EnableBrownianMotion) ?
            ComputeBufferManager.Instance.InstanceDisplayRotations : ComputeBufferManager.Instance.ProteinInstanceRotations);

        RenderProteinsMaterial.SetBuffer("_IngredientColors", ComputeBufferManager.Instance.ProteinColors);
        RenderProteinsMaterial.SetBuffer("_ProteinAtomPositions", ComputeBufferManager.Instance.ProteinAtomPositions);
        RenderProteinsMaterial.SetBuffer("_ProteinClusterPositions", ComputeBufferManager.Instance.ProteinClusterPositions);
        RenderProteinsMaterial.SetBuffer("_ProteinSphereBatchInfos", ComputeBufferManager.Instance.ProteinSphereBatchInfos);

        // Lipid params
        _renderLipidsMaterial.SetInt("_EnableCrossSection", Convert.ToInt32(DisplaySettings.Instance.EnableCrossSection));
        _renderLipidsMaterial.SetVector("_CrossSectionPlane", new Vector4(DisplaySettings.Instance.CrossSectionPlaneNormal.x, DisplaySettings.Instance.CrossSectionPlaneNormal.y, DisplaySettings.Instance.CrossSectionPlaneNormal.z, DisplaySettings.Instance.CrossSectionPlaneDistance));

        _renderLipidsMaterial.SetInt("_EnableLod", Convert.ToInt32(DisplaySettings.Instance.EnableLod));
        _renderLipidsMaterial.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        _renderLipidsMaterial.SetFloat("_FirstLevelBeingRange", DisplaySettings.Instance.FirstLevelOffset);
        _renderLipidsMaterial.SetVector("_CameraForward", _camera.transform.forward);
        _renderLipidsMaterial.SetMatrix("_LodLevelsInfos", Helper.FloatArrayToMatrix4X4(DisplaySettings.Instance.LodLevels));

        _renderLipidsMaterial.SetBuffer("_LipidAtomPositions", ComputeBufferManager.Instance.LipidAtomPositions);
        _renderLipidsMaterial.SetBuffer("_LipidSphereBatchInfos", ComputeBufferManager.Instance.LipidSphereBatchInfos);
        _renderLipidsMaterial.SetBuffer("_LipidInstancePositions", ComputeBufferManager.Instance.LipidInstancePositions);
        _renderLipidsMaterial.SetBuffer("_LipidInstanceCullFlags", ComputeBufferManager.Instance.LipidInstanceCullFlags);

        // DNA/RNA data
        _renderDnaMaterial.SetInt("_NumSteps", DisplaySettings.Instance.NumStepsPerSegment);
		_renderDnaMaterial.SetInt ("_NumSegments", SceneManager.Instance.NumDnaControlPoints);// - 1);
        _renderDnaMaterial.SetInt("_EnableTwist", Convert.ToInt32(DisplaySettings.Instance.EnableTwist));

        _renderDnaMaterial.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        _renderDnaMaterial.SetFloat("_SegmentLength", DisplaySettings.Instance.DistanceContraint);
        _renderDnaMaterial.SetFloat("_TwistFactor", DisplaySettings.Instance.TwistFactor);
        _renderDnaMaterial.SetBuffer("_DnaAtoms", ComputeBufferManager.Instance.DnaAtoms);
        _renderDnaMaterial.SetBuffer("_CurveIngredientsInfos", ComputeBufferManager.Instance.CurveIngredientsInfos);
        _renderDnaMaterial.SetBuffer("_CurveIngredientsColors", ComputeBufferManager.Instance.CurveIngredientsColors);
        _renderDnaMaterial.SetBuffer("_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPointsPositions);
        _renderDnaMaterial.SetBuffer("_DnaControlPointsNormals", ComputeBufferManager.Instance.DnaControlPointsNormals);
        _renderDnaMaterial.SetBuffer("_DnaControlPointsInfos", ComputeBufferManager.Instance.DnaControlPointsInfos);
		
		_renderDnaMaterial.SetFloat("_Scale", DisplaySettings.Instance.Scale);
		_renderDnaMaterial.SetInt("_EnableCrossSection", Convert.ToInt32(DisplaySettings.Instance.EnableCrossSection));
		_renderDnaMaterial.SetVector("_CrossSectionPlane", new Vector4(DisplaySettings.Instance.CrossSectionPlaneNormal.x, DisplaySettings.Instance.CrossSectionPlaneNormal.y, DisplaySettings.Instance.CrossSectionPlaneNormal.z, DisplaySettings.Instance.CrossSectionPlaneDistance));


		// Shadow data
		//RenderSceneMaterial.SetInt("_EnableShadows", Convert.ToInt32(DisplaySettings.Instance.EnableShadows));
        //RenderSceneMaterial.SetTexture("_ShadowMap", ShadowMap);
        //RenderSceneMaterial.SetVector("_ShadowCameraForward", ShadowCamera.transform.forward);
        //RenderSceneMaterial.SetVector("_ShadowCameraWorldPos", ShadowCamera.transform.position);
        //RenderSceneMaterial.SetMatrix("_ShadowCameraViewMatrix", ShadowCamera.worldToCameraMatrix);
        //RenderSceneMaterial.SetMatrix("_ShadowCameraProjMatrix", GL.GetGPUProjectionMatrix(ShadowCamera.projectionMatrix, false));
        //RenderSceneMaterial.SetMatrix("_ShadowCameraViewProjMatrix", GL.GetGPUProjectionMatrix(ShadowCamera.projectionMatrix, false) * ShadowCamera.worldToCameraMatrix);
    }

    private void ComputeDNAStrands()
    {
        if (!DisplaySettings.Instance.EnableDNAConstraints) return;

        int numSegments = SceneManager.Instance.NumDnaSegments;
        int numSegmentPairs1 = (int)Mathf.Ceil(numSegments / 2.0f);
        int numSegmentPairs2 = (int)Mathf.Ceil(numSegments / 4.0f);

        RopeConstraintsCS.SetFloat("_DistanceMin", DisplaySettings.Instance.AngularConstraint);
        RopeConstraintsCS.SetFloat("_DistanceMax", DisplaySettings.Instance.DistanceContraint);
        RopeConstraintsCS.SetInt("_NumControlPoints", SceneManager.Instance.NumDnaControlPoints);

        // Do distance constraints
        RopeConstraintsCS.SetInt("_Offset", 0);
        RopeConstraintsCS.SetBuffer(0, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPointsPositions);
        RopeConstraintsCS.Dispatch(0, (int)Mathf.Ceil(numSegmentPairs1 / 16.0f), 1, 1);

        RopeConstraintsCS.SetInt("_Offset", 1);
        RopeConstraintsCS.SetBuffer(0, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPointsPositions);
        RopeConstraintsCS.Dispatch(0, (int)Mathf.Ceil(numSegmentPairs1 / 16.0f), 1, 1);

        // Do bending constraints
        RopeConstraintsCS.SetInt("_Offset", 0);
        RopeConstraintsCS.SetBuffer(1, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPointsPositions);
        RopeConstraintsCS.Dispatch(1, (int)Mathf.Ceil(numSegmentPairs2 / 16.0f), 1, 1);

        RopeConstraintsCS.SetInt("_Offset", 1);
        RopeConstraintsCS.SetBuffer(1, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPointsPositions);
        RopeConstraintsCS.Dispatch(1, (int)Mathf.Ceil(numSegmentPairs2 / 16.0f), 1, 1);

        RopeConstraintsCS.SetInt("_Offset", 2);
        RopeConstraintsCS.SetBuffer(1, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPointsPositions);
        RopeConstraintsCS.Dispatch(1, (int)Mathf.Ceil(numSegmentPairs2 / 16.0f), 1, 1);

        RopeConstraintsCS.SetInt("_Offset", 3);
        RopeConstraintsCS.SetBuffer(1, "_DnaControlPoints", ComputeBufferManager.Instance.DnaControlPointsPositions);
        RopeConstraintsCS.Dispatch(1, (int)Mathf.Ceil(numSegmentPairs2 / 16.0f), 1, 1);
    }

    private Matrix4x4 _previousFrameInverseViewProjMatrix;

    private void ComputeHiZMap(RenderTexture depthBuffer)
    {
        // Hierachical depth buffer
        if (_HiZMap == null || _HiZMap.width != Screen.width || _HiZMap.height != Screen.height)
        {
            if (_HiZMap != null)
            {
                _HiZMap.Release();
                DestroyImmediate(_HiZMap);
            }

            _HiZMap = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RFloat);
            _HiZMap.enableRandomWrite = true;
            _HiZMap.useMipMap = false;
            _HiZMap.isVolume = true;
            _HiZMap.volumeDepth = 24;
            //_HiZMap.filterMode = FilterMode.Point;
            _HiZMap.wrapMode = TextureWrapMode.Clamp;
            _HiZMap.Create();
        }

        OcclusionCullingCS.SetInt("_ScreenWidth", Screen.width);
        OcclusionCullingCS.SetInt("_ScreenHeight", Screen.height);

        OcclusionCullingCS.SetTexture(0, "_RWHiZMap", _HiZMap);
        OcclusionCullingCS.SetTexture(0, "_DepthBuffer", depthBuffer);
        OcclusionCullingCS.Dispatch(0, (int)Mathf.Ceil(Screen.width / 8.0f), (int)Mathf.Ceil(Screen.height / 8.0f), 1);

        OcclusionCullingCS.SetTexture(1, "_RWHiZMap", _HiZMap);
        for (int i = 1; i < 12; i++)
        {
            OcclusionCullingCS.SetInt("_CurrentLevel", i);
            OcclusionCullingCS.Dispatch(1, (int)Mathf.Ceil(Screen.width / 8.0f), (int)Mathf.Ceil(Screen.height / 8.0f), 1);
        }

        _previousFrameInverseViewProjMatrix = (GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false) * _camera.worldToCameraMatrix).inverse;
    }

    private void ComputeBrownianMotion()
    {
        // Do Brownian motion
        if (DisplaySettings.Instance.EnableBrownianMotion)
        {
            // Do proteins
            //BrownianMotionCS.SetFloat("_Time", Time.time);
            //BrownianMotionCS.SetBuffer(0, "_InstancePositions", ComputeBufferManager.Instance.InstancePositions);
            //BrownianMotionCS.SetBuffer(0, "_InstanceRotations", ComputeBufferManager.Instance.InstanceRotations);
            //BrownianMotionCS.SetBuffer(0, "_InstanceDisplayPositions", ComputeBufferManager.Instance.InstanceDisplayPositions);
            //BrownianMotionCS.SetBuffer(0, "_InstanceDisplayRotations", ComputeBufferManager.Instance.InstanceDisplayRotations);
            //BrownianMotionCS.Dispatch(0, (int)Mathf.Ceil(SceneManager.Instance.NumInstances / 8.0f), 1, 1);

            // Do lipids
            //BrownianMotionCS.Dispatch(0, (int)Mathf.Ceil(SceneManager.Instance.NumInstances / 8.0f), 1, 1);
        }
    }

    private void ComputeCrossSection()
    {
        if (DisplaySettings.Instance.DebugObjectCulling) return;

        CrossSectionCS.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        CrossSectionCS.SetInt("_EnableCrossSection", Convert.ToInt32(DisplaySettings.Instance.EnableCrossSection));
        CrossSectionCS.SetVector("_CrossSectionPlane", new Vector4(DisplaySettings.Instance.CrossSectionPlaneNormal.x, DisplaySettings.Instance.CrossSectionPlaneNormal.y, DisplaySettings.Instance.CrossSectionPlaneNormal.z, DisplaySettings.Instance.CrossSectionPlaneDistance));

        if (SceneManager.Instance.NumProteinInstances > 0)
        {
            // Compute protein cross section 
            CrossSectionCS.SetInt("_UseOffset", 0);
            CrossSectionCS.SetInt("_NumInstances", SceneManager.Instance.NumProteinInstances);
            CrossSectionCS.SetBuffer(0, "_InstanceCullFlags", ComputeBufferManager.Instance.ProteinInstanceCullFlags);
            CrossSectionCS.SetBuffer(0, "_InstancePositions", ComputeBufferManager.Instance.ProteinInstancePositions);
            CrossSectionCS.Dispatch(0, (int)Mathf.Ceil(SceneManager.Instance.NumProteinInstances / 32.0f), 1, 1);
        }

        if (SceneManager.Instance.NumLipidInstances > 0 && DisplaySettings.Instance.ShowMembrane)
        {
            // Compute lipid cross section 
            CrossSectionCS.SetInt("_UseOffset", 1);
            CrossSectionCS.SetInt("_NumInstances", SceneManager.Instance.NumLipidInstances);
            CrossSectionCS.SetBuffer(0, "_InstanceCullFlags", ComputeBufferManager.Instance.LipidInstanceCullFlags);
            CrossSectionCS.SetBuffer(0, "_InstancePositions", ComputeBufferManager.Instance.LipidInstancePositions);
            CrossSectionCS.Dispatch(0, (int)Mathf.Ceil(SceneManager.Instance.NumLipidInstances / 32.0f), 1, 1);
        }
    }

    private void ComputeFrustrumCulling()
    {
        if (DisplaySettings.Instance.DebugObjectCulling) return;
        
        FrustrumCullingCS.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        FrustrumCullingCS.SetFloats("_FrustrumPlanes", Helper.FrustrumPlanesAsFloats(_camera));

        if (SceneManager.Instance.NumProteinInstances > 0)
        {
            // Compute protein frustrum culling
            FrustrumCullingCS.SetInt("_NumInstances", SceneManager.Instance.NumProteinInstances);
            FrustrumCullingCS.SetBuffer(0, "_InstanceCullFlags", ComputeBufferManager.Instance.ProteinInstanceCullFlags);
            FrustrumCullingCS.SetBuffer(0, "_InstancePositions", ComputeBufferManager.Instance.ProteinInstancePositions);
            FrustrumCullingCS.Dispatch(0, (int)Mathf.Ceil(SceneManager.Instance.NumProteinInstances / 32.0f), 1, 1);
        }

        if (SceneManager.Instance.NumLipidInstances > 0 && DisplaySettings.Instance.ShowMembrane)
        {
            // Compute lipids frustrum culling
            FrustrumCullingCS.SetInt("_NumInstances", SceneManager.Instance.NumLipidInstances);
            FrustrumCullingCS.SetBuffer(0, "_InstanceCullFlags", ComputeBufferManager.Instance.LipidInstanceCullFlags);
            FrustrumCullingCS.SetBuffer(0, "_InstancePositions", ComputeBufferManager.Instance.LipidInstancePositions);
            FrustrumCullingCS.Dispatch(0, (int)Mathf.Ceil(SceneManager.Instance.NumLipidInstances / 32.0f), 1, 1);
        }
    }
    
    private void ComputeOcclusionCulling(bool clearOcclusionFlags = false)
    {
        if (_HiZMap == null || DisplaySettings.Instance.DebugObjectCulling) return;

        OcclusionCullingCS.SetInt("_ClearOcclusionFlags", Convert.ToInt32(clearOcclusionFlags));
        OcclusionCullingCS.SetInt("_ScreenWidth", Screen.width);
        OcclusionCullingCS.SetInt("_ScreenHeight", Screen.height);
        OcclusionCullingCS.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        OcclusionCullingCS.SetFloats("_FrustrumPlanes", Helper.FrustrumPlanesAsFloats(_camera));
        OcclusionCullingCS.SetFloats("_CameraViewMatrix", Helper.Matrix4X4ToFloatArray(_camera.worldToCameraMatrix));
        OcclusionCullingCS.SetFloats("_CameraProjMatrix", Helper.Matrix4X4ToFloatArray(GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false)));
        OcclusionCullingCS.SetFloats("_PreviousFrameInverseViewProjMatrix", Helper.Matrix4X4ToFloatArray(_previousFrameInverseViewProjMatrix));

        OcclusionCullingCS.SetTexture(2, "_HiZMap", _HiZMap);

        if (SceneManager.Instance.NumProteinInstances > 0)
        {
            // Do protein occlusion culling
            OcclusionCullingCS.SetInt("_NumInstances", SceneManager.Instance.NumProteinInstances);
            OcclusionCullingCS.SetBuffer(2, "_InstanceCullFlags", ComputeBufferManager.Instance.ProteinInstanceCullFlags);
            OcclusionCullingCS.SetBuffer(2, "_InstancePositions", ComputeBufferManager.Instance.ProteinInstancePositions);
            OcclusionCullingCS.Dispatch(2, (int)Mathf.Ceil(SceneManager.Instance.NumProteinInstances / 32.0f), 1, 1);
        }

        if (SceneManager.Instance.NumLipidInstances > 0 && DisplaySettings.Instance.ShowMembrane)
        {
            // Do lipid occlusion culling
            OcclusionCullingCS.SetInt("_NumInstances", SceneManager.Instance.NumLipidInstances);
            OcclusionCullingCS.SetBuffer(2, "_InstanceCullFlags", ComputeBufferManager.Instance.LipidInstanceCullFlags);
            OcclusionCullingCS.SetBuffer(2, "_InstancePositions", ComputeBufferManager.Instance.LipidInstancePositions);
            OcclusionCullingCS.Dispatch(2, (int)Mathf.Ceil(SceneManager.Instance.NumLipidInstances / 32.0f), 1, 1);
        }
    }
    
    private void ComputeBatching()
    {
        // Do sphere batching
        BatchInstancesCS.SetFloat("_Scale", DisplaySettings.Instance.Scale);
        BatchInstancesCS.SetInt("_EnableLod", Convert.ToInt32(DisplaySettings.Instance.EnableLod));
        BatchInstancesCS.SetInt("_NumLevels", SceneManager.Instance.NumLodLevels);
        BatchInstancesCS.SetInt("_NumInstances", SceneManager.Instance.NumProteinInstances);
        BatchInstancesCS.SetVector("_CameraForward", _camera.transform.forward);
        BatchInstancesCS.SetVector("_CameraPosition", _camera.transform.position);
        BatchInstancesCS.SetFloats("_LodLevelsInfos", DisplaySettings.Instance.LodLevels);

        if (SceneManager.Instance.NumProteinInstances > 0)
        {
            // Do protein batching
            BatchInstancesCS.SetBuffer(0, "_ProteinInstanceInfo", ComputeBufferManager.Instance.ProteinInstanceInfos);
            BatchInstancesCS.SetBuffer(0, "_ProteinInstancePositions", ComputeBufferManager.Instance.ProteinInstancePositions);
            BatchInstancesCS.SetBuffer(0, "_ProteinInstanceCullFlags", ComputeBufferManager.Instance.ProteinInstanceCullFlags);
            BatchInstancesCS.SetBuffer(0, "_ProteinVisibilityFlag", ComputeBufferManager.Instance.ProteinVisibilityFlags);
            BatchInstancesCS.SetBuffer(0, "_IngredientAtomCount", ComputeBufferManager.Instance.ProteinAtomCount);
            BatchInstancesCS.SetBuffer(0, "_IngredientAtomStart", ComputeBufferManager.Instance.ProteinAtomStart);
            BatchInstancesCS.SetBuffer(0, "_IngredientClusterCount", ComputeBufferManager.Instance.ProteinClusterCount);
            BatchInstancesCS.SetBuffer(0, "_IngredientClusterStart", ComputeBufferManager.Instance.ProteinClusterStart);
            BatchInstancesCS.SetBuffer(0, "_ProteinSphereBatchInfos", ComputeBufferManager.Instance.ProteinSphereBatchInfos);
            BatchInstancesCS.Dispatch(0, (int)Mathf.Ceil((float)SceneManager.Instance.NumProteinInstances / 32.0f), 1, 1);

            // Count sphere batches
            ComputeBuffer.CopyCount(ComputeBufferManager.Instance.ProteinSphereBatchInfos, _argBuffer, 0);
        }

        // Do lipid batching
        //...

        //// Debug batched instances
        //int[] batchCount = new int[] { 0 };
        //_argBuffer.GetData(batchCount);
        //Debug.Log("num batches " + batchCount[0]);
    }

	private void ToggleFluorescence(){
		CanvasFluo.SetActive (DisplaySettings.Instance.Fluo);
		CanvasFluo.transform.GetChild (0).gameObject.SetActive (DisplaySettings.Instance.FluoFS);
		CanvasFluo.transform.GetChild (1).gameObject.SetActive (!DisplaySettings.Instance.FluoFS);
	}

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        // Return if no instances to draw
        if (SceneManager.Instance.NumProteinInstances == 0 && SceneManager.Instance.NumLipidInstances == 0 &&
            SceneManager.Instance.NumDnaSegments == 0)
        {
            Graphics.Blit(src, dst); return;
        }

		ToggleFluorescence ();

        ComputeDNAStrands();

        ComputeCrossSection();
        ComputeFrustrumCulling();
        //ComputeOcclusionCulling(false); // Do pre-render occlusion test

        if (SceneManager.Instance.NumProteinInstances > 0)
        {
            ComputeBatching();
        }

        // This resets the append buffer buffer to 0
        Graphics.SetRandomWriteTarget(1, ComputeBufferManager.Instance.ProteinSphereBatchInfos);
        Graphics.Blit(src, dst);
        Graphics.ClearRandomWriteTargets();

        SetShaderParams();

        /*** Start rendering routine ***/

        // Declare temp buffers
        var idBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.RInt);
        var colorBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        var depthBuffer = RenderTexture.GetTemporary(src.width, src.height, 24, RenderTextureFormat.Depth);
        var depthNormalsBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
        var colorCompositeBuffer = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        var depthCompositeBuffer = RenderTexture.GetTemporary(src.width, src.height, 24, RenderTextureFormat.Depth);

        // Clear temp buffers
        Graphics.SetRenderTarget(idBuffer);
        GL.Clear(false, true, new Color(-1, 0, 0, 0));

        Graphics.SetRenderTarget(colorBuffer.colorBuffer, depthBuffer.depthBuffer);
        GL.Clear(true, true, new Color(1, 1, 1, 1));

        Graphics.SetRenderTarget(depthNormalsBuffer);
        GL.Clear(true, true, new Color(0.5f, 0.5f, 0, 0));

        // Set render target
        Graphics.SetRenderTarget(new[] { colorBuffer.colorBuffer, idBuffer.colorBuffer }, depthBuffer.depthBuffer);
        
        // Draw lipids
        if (SceneManager.Instance.NumLipidInstances > 0 && DisplaySettings.Instance.ShowMembrane)
        {
            _renderLipidsMaterial.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Points, SceneManager.Instance.NumLipidInstances);
        }

        // Draw proteins
        if (SceneManager.Instance.NumProteinInstances > 0)
        {
            RenderProteinsMaterial.SetPass(0);
            Graphics.DrawProceduralIndirect(MeshTopology.Points, _argBuffer);
        }
        
        // Draw RNA
        if (SceneManager.Instance.NumDnaSegments > 0 && DisplaySettings.Instance.ShowRNA)
        {
            _renderDnaMaterial.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Points, Mathf.Max(SceneManager.Instance.NumDnaSegments - 2, 0)); // Do not draw first and last segments
        }

        ComputeHiZMap(depthBuffer);

        // Do post-render occlusion test
        ComputeOcclusionCulling(true);

        // Compute edge detection
        _contourMaterial.SetTexture("_IdTexture", idBuffer);
        Graphics.Blit(colorBuffer, colorCompositeBuffer, _contourMaterial, 0);
        Graphics.Blit(colorCompositeBuffer, colorBuffer);

        // Compute final compositing with the rest of the scene
        _compositeMaterial.SetTexture("_ColorTexture", colorBuffer);
        _compositeMaterial.SetTexture("_DepthTexture", depthBuffer);
        Graphics.SetRenderTarget(colorCompositeBuffer.colorBuffer, depthCompositeBuffer.depthBuffer);
        GL.Clear(true, true, new Color(1, 1, 1, 1));
        Graphics.Blit(src, _compositeMaterial, 1);

        // Blit final color buffer to dst buffer
        Graphics.Blit(colorCompositeBuffer, dst);

        // Set final depth buffer to global depth
        Shader.SetGlobalTexture("_CameraDepthTexture", depthCompositeBuffer);
        Shader.SetGlobalTexture("_CameraDepthNormalsTexture ", depthNormalsBuffer); // It is important to set this otherwise AO will show ghosts

        if (_rightMouseDown)
        {
            SceneManager.Instance.SetSelectedElement(ReadPixelId(idBuffer, _mousePos));
            _rightMouseDown = false;
        }

        // Release temp buffers
        RenderTexture.ReleaseTemporary(idBuffer);
        RenderTexture.ReleaseTemporary(colorBuffer);
        RenderTexture.ReleaseTemporary(depthBuffer);
        RenderTexture.ReleaseTemporary(depthNormalsBuffer);
        RenderTexture.ReleaseTemporary(colorCompositeBuffer);
        RenderTexture.ReleaseTemporary(depthCompositeBuffer);
        
        // Debug Hi-Z map
        //_compositeMaterial.SetTexture("_HiZMap", _HiZMap);
        //Graphics.Blit(src, dst, _compositeMaterial, 2);
    }

    private int ReadPixelId(RenderTexture texture, Vector2 coord)
    {
        var outBuffer = new ComputeBuffer(1, sizeof(int));

        ReadPixelCS.SetInts("_Coord", (int)coord.x, Screen.height - (int)coord.y);
        ReadPixelCS.SetTexture(0, "_IdTexture", texture);
        ReadPixelCS.SetBuffer(0, "_OutputBuffer", outBuffer);
        ReadPixelCS.Dispatch(0, 1, 1, 1);

        var pixelId = new [] { 0 };
        outBuffer.GetData(pixelId);
        outBuffer.Release();

        return pixelId[0];
    }
}

