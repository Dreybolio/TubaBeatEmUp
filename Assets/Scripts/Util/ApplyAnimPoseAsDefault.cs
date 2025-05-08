using UnityEngine;
using UnityEditor;

public class ApplyAnimPoseAsDefault : MonoBehaviour
{
    [MenuItem("Util/Bake Animation Pose As Default (from Clip)")]
    static void BakePose()
    {
        if (Selection.transforms.Length == 0)
        {
            Debug.LogError("Select at least one GameObject to bake.");
            return;
        }

        // Prompt user to pick animation clip
        AnimationClip clip = EditorGUIUtility.LoadRequired("Assets/Anim/BagpipeBarbarian/BarbarianDefault.anim") as AnimationClip; // replace or use object picker
        float sampleTime = 0f; // change this or expose as needed

        if (clip == null)
        {
            Debug.LogError("No AnimationClip assigned.");
            return;
        }

        // Find a root with an Animator
        Transform root = FindCommonRootWithAnimator(Selection.transforms);
        if (root == null)
        {
            Debug.LogError("No common root with Animator found. Make sure a parent of the selected objects has an Animator.");
            return;
        }

        // Sample animation at time
        AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(root.gameObject, clip, sampleTime);
        AnimationMode.EndSampling();

        // Apply sampled transforms to selected GameObjects
        foreach (var t in Selection.transforms)
        {
            Vector3 worldPos = t.position;
            Quaternion worldRot = t.rotation;
            Vector3 worldScale = t.lossyScale;

            Transform parent = t.parent;
            Undo.SetTransformParent(t, null, "Unparent Temporarily");

            Undo.RecordObject(t, "Apply Sampled Animation Pose");
            t.position = worldPos;
            t.rotation = worldRot;

            t.localScale = Vector3.one;
            Vector3 currentLossy = t.lossyScale;
            t.localScale = new Vector3(
                worldScale.x / currentLossy.x,
                worldScale.y / currentLossy.y,
                worldScale.z / currentLossy.z
            );

            Undo.SetTransformParent(t, parent, "Reparent Object");
        }

        AnimationMode.StopAnimationMode();

        Debug.Log("Applied sampled animation pose to selected objects.");
    }

    static Transform FindCommonRootWithAnimator(Transform[] selection)
    {
        foreach (Transform t in selection)
        {
            Transform current = t;
            while (current != null)
            {
                if (current.GetComponent<Animator>() != null)
                    return current;
                current = current.parent;
            }
        }
        return null;
    }
}