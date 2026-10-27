#!/bin/bash
# Unity Project Setup for Arcanepunk Life Sim
# Run this in an empty folder to create the Unity project structure

set -e

PROJECT_NAME="ArcanepunkLifeSim"
UNITY_VERSION="2022.3.0f1"

echo "🔮 Setting up Arcanepunk Unity project..."

# Create directory structure
mkdir -p Assets/Plugins
mkdir -p Assets/Scripts/Core
mkdir -p Assets/Scripts/Systems
mkdir -p Assets/Scripts/UI
mkdir -p Assets/Resources/Data
mkdir -p Assets/Scenes
mkdir -p Assets/Prefabs
mkdir -p Assets/Editor
mkdir -p ProjectSettings

# Copy data files to Resources
echo "📦 Copying data files..."
cp -r /mnt/c/Users/Admin/AppData/Local/Temp/arcanepunk/*.json Assets/Resources/Data/ 2>/dev/null || true

# Copy C# scripts
echo "📝 Copying C# scripts..."
cp /mnt/c/Users/Admin/AppData/Local/Temp/arcanepunk/*.cs Assets/Scripts/Core/ 2>/dev/null || true
cp /mnt/c/Users/Admin/AppData/Local/Temp/arcanepunk/*System*.cs Assets/Scripts/Systems/ 2>/dev/null || true

# Create Assembly Definitions
cat > Assets/Scripts/Core/Arcanepunk.Core.asmdef << 'EOF'
{
    "name": "Arcanepunk.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": []
}
EOF

cat > Assets/Scripts/Systems/Arcanepunk.Systems.asmdef << 'EOF'
{
    "name": "Arcanepunk.Systems",
    "references": ["Arcanepunk.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": []
}
EOF

# Create minimal ProjectSettings
cat > ProjectSettings/ProjectSettings.asset << 'EOF'
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  productName: Arcanepunk Life Sim
  companyName: Arcanepunk Studios
  defaultScreenWidth: 1080
  defaultScreenHeight: 1920
  targetDevice: 3
  targetResolution: 1080x1920
  targetPixelDensity: 320
  bundleVersion: 1.0.0
  bundleIdentifier: com.arcanepunk.lifesim
  defaultInterfaceOrientation: 1
  supportedInterfaceOrientations: 4
  useOnDemandResources: false
  accelerometerFrequency: 60
  locationUsageDescription: 
  microphoneUsageDescription: 
  cameraUsageDescription: 
  bluetoothPeripheralUsageDescription: 
  appleDeveloperTeamID: 
  iOSManualSigningProvisioningProfileID: 
  iOSManualSigningProvisioningProfileType: 0
  iOSSigningTeamID: 
  iOSBuildNumber: 0
  iOSURLSchemes: []
  iOSBackgroundModes: 0
  metalAPIValidation: 0
  metalForceHardShadows: 0
  metalRenderPipelineCompatibility: 0
  metalFrameCaptureEnabled: 0
  appleEnableAutomaticSigning: 0
  iOSSdkVersion: 
  tvOSSdkVersion: 
  visionOSSdkVersion: 
  androidMinSdkVersion: 24
  androidTargetSdkVersion: 34
  androidTargetArchitectures: 4
  androidBundleVersionCode: 1
  androidMinifyDebug: false
  androidMinifyRelease: false
  androidProguardUserFile: 
  androidProguardFiles: 
  androidUseCustomKeystore: false
  androidKeystoreName: 
  androidKeystorePass: 
  androidKeyaliasName: 
  androidKeyaliasPass: 
  androidTVCompatibility: false
  androidEnableBanner: false
  androidIsGame: true
  androidBanner: {fileID: 0}
  androidIntentFilters: []
  androidSplashScreenScale: 0
  androidSplashScreenBackgroundColor: {r: 0.16, g: 0.16, b: 0.16, a: 1}
  androidSplashScreenImage: {fileID: 0}
  androidSplashScreenOverlayOpacity: 1
  androidAdaptiveIconForeground: {fileID: 0}
  androidAdaptiveIconBackground: {fileID: 0}
  androidAdaptiveIconMonochrome: {fileID: 0}
  androidResValueFiles: []
  androidGradleTemplate: 
  androidMainTemplate: 
  androidApplicationTheme: 
  androidManifestTemplate: 
  androidLibraryManifestTemplate: 
  androidAbis: []
  androidFeatures: []
  androidPermissions: []
  aotOptions: 
  stripEngineCode: true
  iPhoneStrippingLevel: 1
  iPhoneScriptCallOptimization: 0
  forceInternetPermission: false
  forceSDCardPermission: false
  createWallpaper: false
  createWallpaperAI: false
  defaultScreenOrientationAndroid: 4
  defaultScreenOrientationIPhone: 1
  supportedInterfaceOrientationsIPhone: 4
  supportedInterfaceOrientationsIPad: 4
  use32BitDisplayBuffer: false
  disableDepthAndStencilBuffers: false
  useOSAutorotation: true
  useOSAutorotationAndroid: true
  useOSAutorotationIPhone: true
  useOSAutorotationTVOS: true
  gpuSkinning: true
  graphicsJobs: 2
  graphicsJobMode: 0
  streamingMipmapsActive: true
  streamingMipmapsAddAllCameras: false
  streamingMipmapsMemoryBudget: 512
  streamingMipmapsRenderersPerFrame: 512
  streamingMipmapsMaxLevelReduction: 2
  streamingMipmapsMaxFileIORequests: 16
  lightmapStripping: false
  instancingStripping: true
  lightProbeProxyVolumeStripping: true
  localUserId: 
  activeInputHandling: 1
  visibleBackgroundModules: []
  scriptingBackend: 2
  apiCompatibilityLevel: 6
  scriptingDefineSymbols: 
  additionalIl2CppArgs: 
  enableInternalProfiler: false
  bakeCollisionMeshes: true
  preloadedAssets: []
  virtualRealitySupported: false
  defaultPhysicsMaterial: {fileID: 0}
  physicsGravity: {x: 0, y: -9.81, z: 0}
  physicsDefaultMaterial: {fileID: 0}
  physicsBounceThreshold: 2
  physicsSleepThreshold: 0.005
  physicsDefaultSolverIterations: 6
  physicsDefaultSolverVelocityIterations: 1
  physicsQueriesHitBackfaces: false
  physicsEnableUnifiedHeightmaps: true
  physicsCookingOptions: 0
  physicsLayerCollisionMatrix: []
  physicsAutoSimulation: true
  physicsAutoSyncTransforms: false
  physicsReuseCollisionCallbacks: false
  physicsContactGenerationRate: 10
  physicsContactOffset: 0.01
  physicsDefaultContactOffset: 0.01
  physicsSleepThreshold: 0.005
  physicsMaxDepenetrationVelocity: 1
  physicsSolverType: 0
  physicsCallbacksOnDisable: false
  physicsQueriesHitTriggers: true
  particleSystemMaxParticleCount: 16384
  particleSystemMaxMeshIndexCount: 65535
  particleSystemMaxTrailsPerParticle: 3
  particleSystemTrailsMaxTextureSlots: 8
  particleSystemMeshIndexFormat: 0
  terrainMaxMeshTrees: 10000
  terrainDetailResolution: 256
  terrainDetailResolutionPerPatch: 16
  terrainHeightmapResolution: 513
  terrainBaseMapDistance: 1000
  terrainTreeLODDistance: 300
  terrainBillboardStart: 50
  terrainFadeLength: 5
  terrainMaxMeshTrees: 10000
  terrainWindSpeed: 0.5
  terrainWindWaveSize: 0.5
  terrainWindWaveBending: 0.5
  terrainGrassDensity: 0.5
  terrainGrassBillboardStart: 5
  terrainGrassFadeLength: 2
  terrainDetailDistance: 250
  terrainDetailDensity: 1
  terrainTreeDistance: 1000
  terrainBillboardStart: 50
  terrainFadeLength: 5
  navMeshSettings: {fileID: 0}
  lightmapSettings: {fileID: 0}
  renderingSettings: {fileID: 0}
  colorSpace: 1
  tierSettings: []
  activeColorSpace: 1
  msaaSampleCount: 0
  shaderPrecisionModel: 0
  defaultRenderingPath: 0
  mobileRenderingPath: 0
  renderingPath: 0
  renderingQuality: 3
  customRenderPipeline: {fileID: 0}
  transparencySortMode: 0
  transparencySortAxis: {x: 0, y: 0, z: 1}
  defaultTextureCompression: 0
  textureCompressionPerPlatform: []
  lightmapEncodingQuality: 1
  lightmapsMode: 0
  lightmapParameters: []
  lightProbes: {fileID: 0}
  reflectionProbes: {fileID: 0}
  shadowResolution: 1
  shadowProjection: 0
  shadowCascades: 1
  shadowDistance: 150
  shadowNearPlaneOffset: 2
  shadowmaskMode: 0
  haloStrength: 0.5
  flareFadeSpeed: 3
  flareStrength: 1
  haloTexture: {fileID: 0}
  lensFlareTexture: {fileID: 0}
  defaultCookie: {fileID: 0}
  defaultReflectionMode: 0
  defaultReflectionResolution: 128
  reflectionBounces: 1
  reflectionIntensity: 1
  customReflectionTexture: {fileID: 0}
  customReflectionTextureHDR: {fileID: 0}
  skyboxMaterial: {fileID: 0}
  sunSource: {fileID: 0}
  sunIntensity: 1
  sunBounceIntensity: 1
  environmentLightingMode: 0
  environmentLightingSource: 0
  environmentLightingIntensity: 1
  environmentReflectionMode: 0
  environmentReflectionResolution: 128
  environmentReflectionBounces: 1
  environmentReflectionIntensity: 1
  customEnvironmentTexture: {fileID: 0}
  customEnvironmentTextureHDR: {fileID: 0}
  probeVolumeSettings: {fileID: 0}
  probeVolumeGlobalSettings: {fileID: 0}
  probeVolumePerSceneSettings: []
  cookieSize: 256
  globalIlluminationSettings: {fileID: 0}
  tierSettings: []
  activeColorSpace: 1
  msaaSampleCount: 0
  shaderPrecisionModel: 0
  defaultRenderingPath: 0
  mobileRenderingPath: 0
  renderingPath: 0
  renderingQuality: 3
  customRenderPipeline: {fileID: 0}
  transparencySortMode: 0
  transparencySortAxis: {x: 0, y: 0, z: 1}
  defaultTextureCompression: 0
  textureCompressionPerPlatform: []
  lightmapEncodingQuality: 1
  lightmapsMode: 0
  lightmapParameters: []
  lightProbes: {fileID: 0}
  reflectionProbes: {fileID: 0}
  shadowResolution: 1
  shadowProjection: 0
  shadowCascades: 1
  shadowDistance: 150
  shadowNearPlaneOffset: 2
  shadowmaskMode: 0
  haloStrength: 0.5
  flareFadeSpeed: 3
  flareStrength: 1
  haloTexture: {fileID: 0}
  lensFlareTexture: {fileID: 0}
  defaultCookie: {fileID: 0}
  defaultReflectionMode: 0
  defaultReflectionResolution: 128
  reflectionBounces: 1
  reflectionIntensity: 1
  customReflectionTexture: {fileID: 0}
  customReflectionTextureHDR: {fileID: 0}
  skyboxMaterial: {fileID: 0}
  sunSource: {fileID: 0}
  sunIntensity: 1
  sunBounceIntensity: 1
  environmentLightingMode: 0
  environmentLightingSource: 0
  environmentLightingIntensity: 1
  environmentReflectionMode: 0
  environmentReflectionResolution: 128
  environmentReflectionBounces: 1
  environmentReflectionIntensity: 1
  customEnvironmentTexture: {fileID: 0}
  customEnvironmentTextureHDR: {fileID: 0}
  probeVolumeSettings: {fileID: 0}
  probeVolumeGlobalSettings: {fileID: 0}
  probeVolumePerSceneSettings: []
  cookieSize: 256
  globalIlluminationSettings: {fileID: 0}
EOF

# Create Editor script for data validation
cat > Assets/Editor/ValidateArcanepunkData.cs << 'EOF'
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class ValidateArcanepunkData : EditorWindow
{
    [MenuItem("Arcanepunk/Validate Data")]
    public static void ValidateData()
    {
        string dataPath = "Assets/Resources/Data";
        if (!Directory.Exists(dataPath))
        {
            Debug.LogError("Data folder not found!");
            return;
        }

        var files = Directory.GetFiles(dataPath, "*.json");
        int totalFiles = files.Length;
        int validFiles = 0;

        foreach (var file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                var obj = JsonUtility.FromJson<object>(json);
                if (obj != null)
                {
                    validFiles++;
                    Debug.Log($"✓ {Path.GetFileName(file)}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ {Path.GetFileName(file)}: {e.Message}");
            }
        }

        Debug.Log($"Validation complete: {validFiles}/{totalFiles} files valid");
    }

    [MenuItem("Arcanepunk/Create Test Scene")]
    public static void CreateTestScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Create GameManager
        var go = new GameObject("GameManager");
        go.AddComponent<ArcanepunkGameManager>();
        
        // Save scene
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        Debug.Log("Test scene created at Assets/Scenes/Main.unity");
    }

    [MenuItem("Arcanepunk/Generate Realm Prefabs")]
    public static void GenerateRealmPrefabs()
    {
        // Would generate prefabs for each realm
        Debug.Log("Realm prefab generation not yet implemented");
    }
}
EOF

echo "✅ Unity project structure created!"
echo ""
echo "Next steps:"
echo "1. Open Unity Hub"
echo "2. Add project: $(pwd)"
echo "3. Open with Unity $UNITY_VERSION"
echo "4. Run: Arcanepunk > Validate Data"
echo "5. Run: Arcanepunk > Create Test Scene"
echo ""
echo "Data files location: Assets/Resources/Data/"
echo "Scripts location: Assets/Scripts/"