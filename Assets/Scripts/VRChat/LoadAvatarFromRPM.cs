using UnityEngine;
using ReadyPlayerMe.Core;
using System;

/// <summary>
/// Loads a Ready Player Me avatar at runtime.
/// This ensures all meshes and materials are properly loaded.
/// Add this to any GameObject in VR_Room and press Play.
/// </summary>
public class LoadAvatarFromRPM : MonoBehaviour
{
    [Header("Avatar Settings")]
    [Tooltip("The Ready Player Me avatar ID")]
    public string avatarId = "6925dbb9bcfe438b18d485f4";
    
    [Header("Position Settings")]
    [Tooltip("Where to place the avatar")]
    public Vector3 spawnPosition = new Vector3(68.3f, 24f, 44.5f);
    
    [Tooltip("Y rotation (180 = facing camera)")]
    public float yRotation = 180f;
    
    [Tooltip("Scale of the avatar")]
    public Vector3 scale = new Vector3(2.5f, 2.5f, 2.5f);

    [Header("Options")]
    [Tooltip("Delete any existing avatar with the same name")]
    public bool deleteExistingAvatar = true;
    
    [Tooltip("Position in front of main camera instead of spawnPosition")]
    public bool positionInFrontOfCamera = true;
    
    [Tooltip("Distance from camera when using positionInFrontOfCamera")]
    public float distanceFromCamera = 3f;

    [Header("Components to Add")]
    public bool addAvatarController = true;
    public bool addAudioSource = true;

    private AvatarObjectLoader avatarLoader;
    private GameObject loadedAvatar;

    private void Start()
    {
        LoadAvatar();
    }

    [ContextMenu("Load Avatar Now")]
    public void LoadAvatar()
    {
        if (string.IsNullOrEmpty(avatarId))
        {
            Debug.LogError("[LoadAvatarFromRPM] Avatar ID is empty!");
            return;
        }

        // Delete existing avatar if present
        if (deleteExistingAvatar)
        {
            GameObject existing = GameObject.Find(avatarId);
            if (existing != null)
            {
                Debug.Log($"[LoadAvatarFromRPM] Deleting existing avatar: {existing.name}");
                DestroyImmediate(existing);
            }
        }

        // Build the avatar URL
        string avatarUrl = $"https://models.readyplayer.me/{avatarId}.glb";
        Debug.Log($"[LoadAvatarFromRPM] Loading avatar from: {avatarUrl}");

        // Create loader
        avatarLoader = new AvatarObjectLoader();
        avatarLoader.OnCompleted += OnAvatarLoaded;
        avatarLoader.OnFailed += OnAvatarLoadFailed;
        avatarLoader.OnProgressChanged += OnLoadProgress;

        // Start loading
        avatarLoader.LoadAvatar(avatarUrl);
    }

    private void OnLoadProgress(object sender, ProgressChangeEventArgs args)
    {
        Debug.Log($"[LoadAvatarFromRPM] Loading... {args.Progress * 100:F0}% ({args.Operation})");
    }

    private void OnAvatarLoaded(object sender, CompletionEventArgs args)
    {
        Debug.Log($"[LoadAvatarFromRPM] ✓ Avatar loaded successfully!");
        
        loadedAvatar = args.Avatar;
        
        // Position the avatar
        if (positionInFrontOfCamera)
        {
            PositionInFrontOfCamera();
        }
        else
        {
            loadedAvatar.transform.position = spawnPosition;
            loadedAvatar.transform.rotation = Quaternion.Euler(0, yRotation, 0);
        }
        
        // Set scale
        loadedAvatar.transform.localScale = scale;
        
        // Add components
        if (addAudioSource)
        {
            var audioSource = loadedAvatar.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            Debug.Log("[LoadAvatarFromRPM] Added AudioSource");
        }

        if (addAvatarController)
        {
            SetupAvatarController();
        }

        // Log mesh info
        LogMeshInfo();
        
        Debug.Log($"[LoadAvatarFromRPM] Avatar ready at position: {loadedAvatar.transform.position}");
    }

    private void PositionInFrontOfCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = FindObjectOfType<Camera>();
        }

        if (cam != null)
        {
            Vector3 camPos = cam.transform.position;
            Vector3 forward = cam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 newPos = camPos + forward * distanceFromCamera;
            newPos.y = camPos.y - 1.5f; // Below eye level

            loadedAvatar.transform.position = newPos;
            
            // Face the camera
            Vector3 lookDir = camPos - newPos;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                loadedAvatar.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        else
        {
            loadedAvatar.transform.position = spawnPosition;
            loadedAvatar.transform.rotation = Quaternion.Euler(0, yRotation, 0);
        }
    }

    private void SetupAvatarController()
    {
        var controller = loadedAvatar.AddComponent<AvatarController>();
        
        // Find and assign references
        var audioSource = loadedAvatar.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            var audioField = typeof(AvatarController).GetField("audioSource",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            audioField?.SetValue(controller, audioSource);
        }

        // Find face mesh (Renderer_Head or Wolf3D_Head)
        SkinnedMeshRenderer faceMesh = null;
        var renderers = loadedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var r in renderers)
        {
            if (r.name.Contains("Head") && r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0)
            {
                faceMesh = r;
                break;
            }
        }

        if (faceMesh != null)
        {
            var faceMeshField = typeof(AvatarController).GetField("faceMesh",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            faceMeshField?.SetValue(controller, faceMesh);
            Debug.Log($"[LoadAvatarFromRPM] Set faceMesh to: {faceMesh.name} ({faceMesh.sharedMesh.blendShapeCount} blendshapes)");
        }

        // Find bones
        SetupBones(controller);
        
        Debug.Log("[LoadAvatarFromRPM] Added AvatarController");
    }

    private void SetupBones(AvatarController controller)
    {
        var animator = loadedAvatar.GetComponent<Animator>();
        if (animator == null || !animator.isHuman) return;

        // Use Animator to find bones
        try
        {
            SetBoneField(controller, "headBone", animator.GetBoneTransform(HumanBodyBones.Head));
            SetBoneField(controller, "spineBone", animator.GetBoneTransform(HumanBodyBones.Spine));
            SetBoneField(controller, "chestBone", animator.GetBoneTransform(HumanBodyBones.Chest));
            SetBoneField(controller, "shoulderBone", animator.GetBoneTransform(HumanBodyBones.RightShoulder));
            SetBoneField(controller, "elbowBone", animator.GetBoneTransform(HumanBodyBones.RightLowerArm));
            SetBoneField(controller, "wristBone", animator.GetBoneTransform(HumanBodyBones.RightHand));
            SetBoneField(controller, "handSway", animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
            SetBoneField(controller, "handDown", animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));
            
            Debug.Log("[LoadAvatarFromRPM] Set up bone references");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoadAvatarFromRPM] Could not set up some bones: {e.Message}");
        }
    }

    private void SetBoneField(AvatarController controller, string fieldName, Transform bone)
    {
        if (bone == null) return;
        
        var field = typeof(AvatarController).GetField(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        field?.SetValue(controller, bone);
    }

    private void LogMeshInfo()
    {
        var renderers = loadedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
        Debug.Log($"[LoadAvatarFromRPM] Found {renderers.Length} SkinnedMeshRenderers:");
        
        foreach (var r in renderers)
        {
            bool hasMesh = r.sharedMesh != null;
            bool hasMaterial = r.sharedMaterial != null;
            int blendshapes = r.sharedMesh?.blendShapeCount ?? 0;
            
            string status = (hasMesh && hasMaterial) ? "✓" : "✗";
            Debug.Log($"  {status} {r.name}: mesh={hasMesh}, material={hasMaterial}, blendshapes={blendshapes}");
        }
    }

    private void OnAvatarLoadFailed(object sender, FailureEventArgs args)
    {
        Debug.LogError($"[LoadAvatarFromRPM] ✗ Failed to load avatar: {args.Message}");
        Debug.LogError($"[LoadAvatarFromRPM] Failure type: {args.Type}");
        Debug.LogError($"[LoadAvatarFromRPM] URL: {args.Url}");
    }

    private void OnDestroy()
    {
        if (avatarLoader != null)
        {
            avatarLoader.OnCompleted -= OnAvatarLoaded;
            avatarLoader.OnFailed -= OnAvatarLoadFailed;
            avatarLoader.OnProgressChanged -= OnLoadProgress;
        }
    }
}
