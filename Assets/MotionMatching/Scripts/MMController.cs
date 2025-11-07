using UnityEngine;
using Unity.VisualScripting;
using UnityEditor;
using MMSystem;
using UnityEditor.Animations;
using UnityEngine.UIElements;

public class MMController : MonoBehaviour
{
    private PoseDataBase m_PDB;
    private PoseDataConverter m_PDC = new PoseDataConverter();

    [SerializeField] private AnimationClip clip;
    [SerializeField] private Animator anim;
    [SerializeField] private SearchAndSaveType m_SST;

    private void OnValidate()
    {
        //AnimationClip Data grabbing
        /*if (clip != null)
        {
            
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            AnimationClipCurveData[] animationCurvesData = new AnimationClipCurveData[bindings.Length];
            Transform[] allBones = anim.GetComponentsInChildren<Transform>();

            for (int i = 0; i < animationCurvesData.Length; i++) 
            {
                animationCurvesData[i] = new AnimationClipCurveData(bindings[i]);
                animationCurvesData[i].curve = AnimationUtility.GetEditorCurve(clip, bindings[i]);
            }            
        }*/

        switch (m_SST)
        {
            case SearchAndSaveType.Regular:
                {
                    m_PDC.ConvertData(m_SST);
                    break;
                }
            case SearchAndSaveType.Split:
                {
                    m_PDC.ConvertData(m_SST);
                    break;
                }
            case SearchAndSaveType.Context:
                {
                    m_PDC.ConvertData(m_SST);
                    break;
                }
            case SearchAndSaveType.Heirarchy:
                {
                    m_PDC.ConvertData(m_SST);
                    break;
                }
            default: break;
        }
    }

}
