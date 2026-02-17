#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using ReadyPlayerMe.Core;
using System.IO;

/// <summary>
/// Editor tool to download and save a Ready Player Me avatar as a permanent scene object.
/// Access via menu: Tools > Ready Player Me > Download Avatar to Scene
/// </summary>
public class AvatarDownloaderEditor : EditorWindow
{
    private string avatarId = "6925dbb9bcfe438b18d485f4";
    private Vector3 spawnPosition = new Vector3(68.3f, 24f, 44.5f);
    private Vector3 scale = new Vector3(2.5f, 2.5f, 2.5f);
    private float yRotation = 180f;
    private bool isLoading = false;
    private float loadProgress = 0f;
    private string loadStatus = "";
    private AvatarObjectLoader avatarLoader;

    [MenuItem("Tools/Ready Player Me/Download Avatar to Scene")]
    public static void ShowWindow()
    {
        var window = GetWindow<AvatarDownloaderEditor>("RPM Avatar Downloader");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        GUILayout.Label("Ready Player Me Avatar Downloader", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool downloads a Ready Player Me avatar and adds it permanently to your scene.",
            MessageType.Info);

        GUILayout.Space(10);

        // Avatar ID input
        GUILayout.Label("Avatar Settings", EditorStyles.boldLabel);
        avatarId = EditorGUILayout.TextField("Avatar ID", avatarId);
        
        GUILayout.Space(5);
        
        // Position settings
        spawnPosition = EditorGUILayout.Vector3Field("Position", spawnPosition);
        yRotation = EditorGUILayout.FloatField("Y Rotation", yRotation);
        scale = EditorGUILayout.Vector3Field("Scale", scale);

        GUILayout.Space(10);

        // Buttons
        EditorGUI.BeginDisabledGroup(isLoading);
        
        if (GUILayout.Button("Download Avatar to Scene", GUILayout.Height(30)))
        {
            DownloadAvatar();
        }

        if (GUILayout.Button("Position at Scene View Camera", GUILayout.Height(25)))
        {
            if (SceneView.lastActiveSceneView != null)
            {
                var cam = SceneView.lastActiveSceneView.camera;
                spawnPosition = cam.transform.position + cam.transform.forward * 3f;
                spawnPosition.y -= 1.5f;
            }
        }
        
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        // Progress
        if (isLoading)
        {
            EditorGUILayout.LabelField("Status:", loadStatus);
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), loadProgress, $"{loadProgress * 100:F0}%");
            
            if (GUILayout.Button("Cancel"))
            {
                avatarLoader?.Cancel();
                isLoading = false;
            }
        }

        GUILayout.Space(10);

        // Help
        EditorGUILayout.HelpBox(
            "After downloading:\n" +
            "1. The avatar will appear in your scene\n" +
            "2. Press Ctrl+S to save the scene\n" +
            "3. The avatar is now permanent!",
            MessageType.None);
    }

    private void DownloadAvatar()
    {
        if (string.IsNullOrEmpty(avatarId))
        {
            EditorUtility.DisplayDialog("Error", "Please enter an Avatar ID", "OK");
            return;
        }

        // Delete existing avatar if present
        GameObject existing = GameObject.Find(avatarId);
        if (existing != null)
        {
            if (EditorUtility.DisplayDialog("Avatar Exists", 
                $"An avatar named '{avatarId}' already exists. Delete it?", "Yes", "No"))
            {
                DestroyImmediate(existing);
            }
            else
            {
                return;
            }
        }

        isLoading = true;
        loadProgress = 0f;
        loadStatus = "Starting download...";

        string avatarUrl = $"https://models.readyplayer.me/{avatarId}.glb";
        Debug.Log($"[AvatarDownloader] Downloading from: {avatarUrl}");

        avatarLoader = new AvatarObjectLoader();
        avatarLoader.OnCompleted += OnAvatarLoaded;
        avatarLoader.OnFailed += OnAvatarFailed;
        avatarLoader.OnProgressChanged += OnProgress;
        avatarLoader.LoadAvatar(avatarUrl);
    }

    private void OnProgress(object sender, ProgressChangeEventArgs args)
    {
        loadProgress = args.Progress;
        loadStatus = args.Operation;
        Repaint();
    }

    private void OnAvatarLoaded(object sender, CompletionEventArgs args)
    {
        isLoading = false;
        loadStatus = "Complete!";
        loadProgress = 1f;

        GameObject avatar = args.Avatar;
        
        // Position and rotate
        avatar.transform.position = spawnPosition;
        avatar.transform.rotation = Quaternion.Euler(0, yRotation, 0);
        avatar.transform.localScale = scale;

        // Add AudioSource
        var audioSource = avatar.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // Add AvatarController
        var controller = avatar.AddComponent<AvatarController>();
        SetupAvatarController(avatar, controller, audioSource);

        // Mark scene as dirty so it can be saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        // Select the avatar
        Selection.activeGameObject = avatar;

        // Log success
        Debug.Log($"[AvatarDownloader] ✓ Avatar downloaded and added to scene!");
        Debug.Log($"[AvatarDownloader] Position: {avatar.transform.position}");
        Debug.Log($"[AvatarDownloader] IMPORTANT: Press Ctrl+S to save the scene!");

        EditorUtility.DisplayDialog("Success!", 
            "Avatar downloaded and added to scene!\n\n" +
            "IMPORTANT: Press Ctrl+S to save the scene so the avatar persists.",
            "OK");

        Repaint();
    }

    private void SetupAvatarController(GameObject avatar, AvatarController controller, AudioSource audioSource)
    {
        // Set AudioSource
        var audioField = typeof(AvatarController).GetField("audioSource",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        audioField?.SetValue(controller, audioSource);

        // Find face mesh
        SkinnedMeshRenderer faceMesh = null;
        var renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>();
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
            Debug.Log($"[AvatarDownloader] Face mesh: {faceMesh.name} ({faceMesh.sharedMesh.blendShapeCount} blendshapes)");
        }

        // Set up bones using Animator
        var animator = avatar.GetComponent<Animator>();
        if (animator != null && animator.isHuman)
        {
            SetBone(controller, "headBone", animator.GetBoneTransform(HumanBodyBones.Head));
            SetBone(controller, "spineBone", animator.GetBoneTransform(HumanBodyBones.Spine));
            SetBone(controller, "chestBone", animator.GetBoneTransform(HumanBodyBones.Chest));
            SetBone(controller, "shoulderBone", animator.GetBoneTransform(HumanBodyBones.RightShoulder));
            SetBone(controller, "elbowBone", animator.GetBoneTransform(HumanBodyBones.RightLowerArm));
            SetBone(controller, "wristBone", animator.GetBoneTransform(HumanBodyBones.RightHand));
            SetBone(controller, "handSway", animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
            SetBone(controller, "handDown", animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));
        }
    }

    private void SetBone(AvatarController controller, string fieldName, Transform bone)
    {
        if (bone == null) return;
        var field = typeof(AvatarController).GetField(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        field?.SetValue(controller, bone);
    }

    private void OnAvatarFailed(object sender, FailureEventArgs args)
    {
        isLoading = false;
        loadStatus = "Failed!";

        Debug.LogError($"[AvatarDownloader] Failed: {args.Message}");
        
        EditorUtility.DisplayDialog("Download Failed", 
            $"Failed to download avatar.\n\nError: {args.Message}\n\nCheck your internet connection and avatar ID.",
            "OK");

        Repaint();
    }

    private void OnDestroy()
    {
        if (avatarLoader != null)
        {
            avatarLoader.OnCompleted -= OnAvatarLoaded;
            avatarLoader.OnFailed -= OnAvatarFailed;
            avatarLoader.OnProgressChanged -= OnProgress;
        }
    }
}
#endif
