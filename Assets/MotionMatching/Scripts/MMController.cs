using UnityEngine;
using Unity.VisualScripting;
using UnityEditor;
using MMSystem;
using UnityEditor.Animations;
using UnityEngine.UIElements;

public class MMController : MonoBehaviour
{
    private PoseDataBase PDB;

    [SerializeField] private AnimationClip clip;
    [SerializeField] private Animator anim;

    private void OnValidate()
    {
        if (clip != null)
        {
            
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            AnimationClipCurveData[] animationCurvesData = new AnimationClipCurveData[bindings.Length];

            for (int i = 0; i < animationCurvesData.Length; i++) 
            {
                animationCurvesData[i] = new AnimationClipCurveData(bindings[i]);
                animationCurvesData[i].curve = AnimationUtility.GetEditorCurve(clip, bindings[i]);
            }

            foreach (var curve in animationCurvesData)
            {
                if (curve.path.Equals(""))
                    continue;
                //int lastSlash = curve.path.LastIndexOf('/');
                //Debug.Log($"Curves Path: {curve.path} Property Name: {curve.propertyName}");
                //Debug.Log($"Curves Path: {(lastSlash >= 0 ? curve.path.Substring(lastSlash + 1) : curve.path)} Property Name: {curve.propertyName}");

            }

            Transform[] allBones = anim.GetComponentsInChildren<Transform>();

            foreach (var transform in allBones) 
            {
                foreach (var curve in animationCurvesData)
                {
                    int lastSlash = curve.path.LastIndexOf('/');
                    if ((lastSlash >= 0 ? curve.path.Substring(lastSlash + 1) : curve.path).Equals(transform.name))
                    {
                        //Debug.Log(transform.name);
                        Debug.Log($"Bone Name: {transform.name} Value: {curve.curve.Evaluate(1)} Test: {curve.propertyName}");
                    }
                    else 
                    {
                        //Debug.Log($"Bsone Name: {transform.name} Value: {transform.localPosition}");
                    }
                }
            }

        }

        //Get bones and curves
        //Convert convert frames to time
        //Get values ad said time
    }

}
