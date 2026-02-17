using UnityEngine;

/// <summary>
/// Debug tool to diagnose why an avatar is invisible.
/// Add this to any GameObject and click the context menu options.
/// </summary>
public class AvatarDebugger : MonoBehaviour
{
    [Header("Avatar to Debug")]
    public string avatarName = "6925dbb9bcfe438b18d485f4";
    public GameObject avatarObject;

    [Header("Fix Options")]
    public Vector3 forcePosition = new Vector3(0, 0, 3);
    public Vector3 forceScale = new Vector3(1, 1, 1);

    private void Start()
    {
        DiagnoseAvatar();
    }

    [ContextMenu("1. Find and Diagnose Avatar")]
    public void DiagnoseAvatar()
    {
        Debug.Log("========== AVATAR DIAGNOSIS ==========");

        // Find avatar
        if (avatarObject == null)
        {
            avatarObject = GameObject.Find(avatarName);
        }

        if (avatarObject == null)
        {
            Debug.LogError($"❌ Avatar '{avatarName}' NOT FOUND in scene!");
            Debug.Log("Try: Search in Hierarchy for the avatar name");
            return;
        }

        Debug.Log($"✓ Avatar found: {avatarObject.name}");

        // Check active state
        Debug.Log($"\n--- ACTIVE STATE ---");
        Debug.Log($"GameObject.activeSelf: {avatarObject.activeSelf}");
        Debug.Log($"GameObject.activeInHierarchy: {avatarObject.activeInHierarchy}");
        
        if (!avatarObject.activeInHierarchy)
        {
            Debug.LogError("❌ Avatar is DISABLED! Enable it in Hierarchy.");
        }

        // Check Transform
        Debug.Log($"\n--- TRANSFORM ---");
        Debug.Log($"Position: {avatarObject.transform.position}");
        Debug.Log($"Local Position: {avatarObject.transform.localPosition}");
        Debug.Log($"Rotation: {avatarObject.transform.eulerAngles}");
        Debug.Log($"Scale: {avatarObject.transform.localScale}");
        Debug.Log($"Lossy Scale: {avatarObject.transform.lossyScale}");

        if (avatarObject.transform.localScale == Vector3.zero)
        {
            Debug.LogError("❌ Avatar scale is ZERO! It's invisible because it has no size.");
        }

        if (avatarObject.transform.localScale.x < 0.01f)
        {
            Debug.LogError("❌ Avatar scale is VERY SMALL! Increase scale.");
        }

        // Check Layer
        Debug.Log($"\n--- LAYER ---");
        Debug.Log($"Layer: {avatarObject.layer} ({LayerMask.LayerToName(avatarObject.layer)})");

        // Check Renderers
        Debug.Log($"\n--- RENDERERS ---");
        var renderers = avatarObject.GetComponentsInChildren<Renderer>(true);
        Debug.Log($"Total Renderers: {renderers.Length}");

        int enabledCount = 0;
        int visibleCount = 0;
        int materialIssues = 0;

        foreach (var r in renderers)
        {
            if (r.enabled) enabledCount++;
            if (r.isVisible) visibleCount++;
            
            // Check materials
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null)
                {
                    materialIssues++;
                    Debug.LogWarning($"  ⚠ {r.name}: Missing material!");
                }
            }

            // Log each renderer
            string status = r.enabled ? "✓" : "✗";
            Debug.Log($"  {status} {r.name}: enabled={r.enabled}, visible={r.isVisible}, bounds={r.bounds.center}");
        }

        Debug.Log($"\nEnabled: {enabledCount}/{renderers.Length}");
        Debug.Log($"Currently Visible: {visibleCount}/{renderers.Length}");
        
        if (materialIssues > 0)
        {
            Debug.LogError($"❌ {materialIssues} missing materials found!");
        }

        if (enabledCount == 0)
        {
            Debug.LogError("❌ ALL RENDERERS ARE DISABLED! Enable them.");
        }

        // Check SkinnedMeshRenderers specifically
        Debug.Log($"\n--- SKINNED MESH RENDERERS ---");
        var skinnedRenderers = avatarObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in skinnedRenderers)
        {
            bool hasMesh = smr.sharedMesh != null;
            bool hasBones = smr.bones != null && smr.bones.Length > 0;
            bool hasRoot = smr.rootBone != null;
            
            Debug.Log($"  {smr.name}:");
            Debug.Log($"    - Has Mesh: {hasMesh} {(hasMesh ? $"({smr.sharedMesh.name})" : "❌ NO MESH!")}");
            Debug.Log($"    - Has Bones: {hasBones} ({smr.bones?.Length ?? 0} bones)");
            Debug.Log($"    - Has Root Bone: {hasRoot}");
            Debug.Log($"    - Enabled: {smr.enabled}");
            Debug.Log($"    - Bounds: {smr.bounds}");

            if (!hasMesh)
            {
                Debug.LogError($"❌ {smr.name} has NO MESH assigned!");
            }
        }

        // Check Camera
        Debug.Log($"\n--- CAMERA CHECK ---");
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = FindObjectOfType<Camera>();
        }

        if (cam != null)
        {
            Debug.Log($"Camera: {cam.name}");
            Debug.Log($"Camera Position: {cam.transform.position}");
            Debug.Log($"Camera Forward: {cam.transform.forward}");
            Debug.Log($"Camera Near/Far: {cam.nearClipPlane} / {cam.farClipPlane}");
            Debug.Log($"Camera Culling Mask: {cam.cullingMask}");

            // Check if avatar is in view
            Vector3 viewportPoint = cam.WorldToViewportPoint(avatarObject.transform.position);
            Debug.Log($"Avatar in Viewport: {viewportPoint}");
            
            bool inFrustum = viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                            viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                            viewportPoint.z > 0;
            
            if (!inFrustum)
            {
                Debug.LogWarning("⚠ Avatar is OUTSIDE camera view!");
                
                float distance = Vector3.Distance(cam.transform.position, avatarObject.transform.position);
                Debug.Log($"Distance to camera: {distance}m");
                
                if (viewportPoint.z < 0)
                {
                    Debug.LogError("❌ Avatar is BEHIND the camera!");
                }
            }
            else
            {
                Debug.Log("✓ Avatar should be in camera view");
            }

            // Check if layer is culled
            int avatarLayerMask = 1 << avatarObject.layer;
            if ((cam.cullingMask & avatarLayerMask) == 0)
            {
                Debug.LogError($"❌ Camera is NOT rendering layer '{LayerMask.LayerToName(avatarObject.layer)}'!");
            }
        }

        Debug.Log("\n========== END DIAGNOSIS ==========");
    }

    [ContextMenu("2. Force Enable All Renderers")]
    public void ForceEnableAllRenderers()
    {
        if (avatarObject == null) avatarObject = GameObject.Find(avatarName);
        if (avatarObject == null) { Debug.LogError("Avatar not found!"); return; }

        var renderers = avatarObject.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = true;
        }
        Debug.Log($"Enabled {renderers.Length} renderers");
    }

    [ContextMenu("3. Force Activate Avatar")]
    public void ForceActivateAvatar()
    {
        if (avatarObject == null) avatarObject = GameObject.Find(avatarName);
        if (avatarObject == null) { Debug.LogError("Avatar not found!"); return; }

        avatarObject.SetActive(true);
        
        // Also activate all parents
        Transform parent = avatarObject.transform.parent;
        while (parent != null)
        {
            parent.gameObject.SetActive(true);
            parent = parent.parent;
        }
        
        Debug.Log("Avatar and all parents activated");
    }

    [ContextMenu("4. Move Avatar to Origin")]
    public void MoveAvatarToOrigin()
    {
        if (avatarObject == null) avatarObject = GameObject.Find(avatarName);
        if (avatarObject == null) { Debug.LogError("Avatar not found!"); return; }

        avatarObject.transform.position = Vector3.zero;
        avatarObject.transform.rotation = Quaternion.identity;
        Debug.Log("Avatar moved to world origin (0,0,0)");
    }

    [ContextMenu("5. Move Avatar In Front of Camera")]
    public void MoveAvatarInFrontOfCamera()
    {
        if (avatarObject == null) avatarObject = GameObject.Find(avatarName);
        if (avatarObject == null) { Debug.LogError("Avatar not found!"); return; }

        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        if (cam == null) { Debug.LogError("No camera found!"); return; }

        Vector3 newPos = cam.transform.position + cam.transform.forward * 3f;
        newPos.y -= 1f; // Slightly below eye level
        
        avatarObject.transform.position = newPos;
        avatarObject.transform.LookAt(cam.transform);
        avatarObject.transform.rotation = Quaternion.Euler(0, avatarObject.transform.eulerAngles.y + 180, 0);

        Debug.Log($"Avatar moved to: {newPos}");
    }

    [ContextMenu("6. Reset Avatar Scale")]
    public void ResetAvatarScale()
    {
        if (avatarObject == null) avatarObject = GameObject.Find(avatarName);
        if (avatarObject == null) { Debug.LogError("Avatar not found!"); return; }

        avatarObject.transform.localScale = forceScale;
        Debug.Log($"Avatar scale set to: {forceScale}");
    }

    [ContextMenu("7. Set Avatar Layer to Default")]
    public void SetAvatarLayerToDefault()
    {
        if (avatarObject == null) avatarObject = GameObject.Find(avatarName);
        if (avatarObject == null) { Debug.LogError("Avatar not found!"); return; }

        SetLayerRecursively(avatarObject, 0); // 0 = Default layer
        Debug.Log("Avatar and all children set to Default layer");
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    [ContextMenu("8. Create Test Cube at Avatar Position")]
    public void CreateTestCubeAtAvatarPosition()
    {
        if (avatarObject == null) avatarObject = GameObject.Find(avatarName);
        if (avatarObject == null) { Debug.LogError("Avatar not found!"); return; }

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "DEBUG_AvatarPosition";
        cube.transform.position = avatarObject.transform.position;
        cube.transform.localScale = Vector3.one * 0.5f;
        cube.GetComponent<Renderer>().material.color = Color.red;

        Debug.Log($"Red cube created at avatar position: {avatarObject.transform.position}");
        Debug.Log("If you can see the red cube but not the avatar, the avatar meshes are the problem.");
    }

    private void OnDrawGizmos()
    {
        if (avatarObject == null) return;

        // Draw sphere at avatar position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(avatarObject.transform.position, 1f);

        // Draw line to camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(avatarObject.transform.position, cam.transform.position);
        }
    }
}
