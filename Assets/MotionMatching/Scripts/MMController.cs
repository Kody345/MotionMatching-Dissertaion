using MMSystem;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using TreeEditor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

public class MMController : MonoBehaviour
{
    private PoseDataBase m_PDB = new PoseDataBase();

    [SerializeField] private AnimationClip clip;
    [SerializeField] private AnimationClip[] clips;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform m_RootBone;
    [SerializeField] private Transform m_LFootBone;
    [SerializeField] private Transform m_RFootBone;
    [SerializeField] private SearchAndSaveType m_SST;
    [SerializeField] private GameObject cube;
    [SerializeField] private float _PoseFavourWeight;

    private MMSystem.Pose pose = new MMSystem.Pose();
    private Transform[] bones;
    private MMSystem.Pose[] m_Poses;
    private FeatureVector[] m_Features;
    private int num;
    private Coroutine testCo;
    private int i = 0;
    private bool test = false;

    private void OnValidate()
    {
        //AnimationClip Data grabbing
        if (clip != null)
        {
            float frame = clip.length * clip.frameRate;
            float frames = 0f;

            for (int i = 0; i < clips.Length; i++)
            {
                frames += clips[i].frameRate * clips[i].length;
            }

            m_Poses = new MMSystem.Pose[(int)frames];
            m_Features = new MMSystem.FeatureVector[(int)frames];

            m_PDB.ConvertData(m_Poses, m_Features, clips, anim, m_SST, m_LFootBone, m_RFootBone, m_RootBone);

        }

        /*foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones))) 
        {
            if (bone == HumanBodyBones.LastBone)
                continue;

            Transform t = anim.GetBoneTransform(bone);
        }*/
    }

    private void Start()
    {
        anim.enabled = false;
        bones = anim.GetComponentsInChildren<Transform>();
        pose.id = 1000000000;
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(AnimImplement(Vector3.zero));
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            //MovementImplement(Vector3.back);
            if (testCo == null)
                testCo = StartCoroutine(AnimImplement(Vector3.back));
        }
        if (Input.GetKey(KeyCode.A))
        {
            if (testCo == null)
                testCo = StartCoroutine(AnimImplement(Vector3.right));
        }
        if (Input.GetKey(KeyCode.D))
        {
            if (testCo == null)
                testCo = StartCoroutine(AnimImplement(Vector3.left));
        }
        if (Input.GetKey(KeyCode.S))
        {
            if (testCo == null)
                testCo = StartCoroutine(AnimImplement(Vector3.forward));
        }

    }

    IEnumerator AnimImplement(Vector3 dir)
    {
        float interval = 1f / 30f;
        var timer = new Stopwatch();
        timer.Start();
        float nextTime = 0;
        MMSystem.FeatureVector vector = new MMSystem.FeatureVector();

        float dist = 100000000f;

        /*int i = 0;
        while (true)
        {
            if (i >= m_Poses.Length)
                i = 0;
            foreach (var bone in bones)
            {

                if (m_Poses[i].rootBone.name == bone.name)
                {
                    bone.position += m_Poses[i].deltaPos;
                    //bone.position = new Vector3(bone.position.x, bone.position.y0.9566182f, bone.position.z);
                    continue;
                }

                for (int j = 0; j < m_Poses[i].m_Bones.Length; j++)
                {
                    if (m_Poses[i].m_Bones[j].m_Name != bone.name)
                        continue;

                    bone.localPosition = m_Poses[i].m_Bones[j].m_BonePos;
                    bone.localRotation = m_Poses[i].m_Bones[j].m_BoneRot;
                }
            }

            i++;
            nextTime += interval;
            float wait = nextTime - (float)timer.Elapsed.TotalSeconds;

            if (wait > 0f)
            {
                yield return new WaitForSecondsRealtime(wait);
                testCo = null;
            }
            else
            {
                yield return null;
                testCo = null;
            }

        }*/

        /*for (int i = 0; i < m_Poses.Length; i++)
        {

            if (Vector3.Dot(m_Poses[i].dir, dir) < 0.7f)
            {
                continue;
            }

            if (pose.id == m_Poses[i].id)
            {
                continue;
            }

            if (pose.id > m_Poses[m_Poses.Length - 1].id)
            {
                pose = m_Poses[i];
                num = i + 1;
                break;
            }

            

            pose = m_Poses[num];
            num++;
            if (num >= m_Poses.Length)
                num = 0;
        }

        foreach (var bone in bones)
        {
            if (pose.rootBone.name == bone.name)
            {
                bone.position += pose.deltaPos;
                continue;
            }

            for (int i = 0; i < pose.m_Bones.Length; i++)
            {

                if (pose.m_Bones[i].m_Name == bone.name)
                {
                    bone.localPosition = pose.m_Bones[i].m_BonePos;
                    bone.localRotation = pose.m_Bones[i].m_BoneRot;
                }
            }
        }*/

        if (pose.id > 1000)
        {
            pose = m_Poses[0];
        }

        if (i >= m_Poses.Length)
            i = 0;

        /*foreach (var bone in bones)
        {

            if (m_Poses[i].rootBone.name == bone.name)
            {
                bone.rotation = m_Poses[i].rootRot;
                //bone.localPosition += m_Poses[i].deltaPos;

                //bone.position += m_Poses[i].rootPos - pose.rootPos;

                bone.position = new Vector3(bone.position.x + m_Poses[i].deltaPos.x, m_Poses[i].testHipHeight, bone.position.z + m_Poses[i].deltaPos.z);
                //bone.position += new Vector3(m_Poses[i].deltaPos.x, m_Poses[i].testHipHeight, m_Poses[i].deltaPos.z);

                //bone.position = new Vector3(bone.position.x, m_Poses[i].testHipHeight, bone.position.z);
                //gameObject.transform.localPosition += m_Poses[i].deltaPos; // -- Also works
                UnityEngine.Debug.Log("Root Bone");
                continue;
            }

            for (int j = 0; j < m_Poses[i].m_Bones.Length; j++)
            {
                if (m_Poses[i].m_Bones[j].m_Name != bone.name)
                    continue;

                bone.localPosition = m_Poses[i].m_Bones[j].m_BonePos;
                bone.localRotation = m_Poses[i].m_Bones[j].m_BoneRot;
            }
        }*/

        //vector.featureVector =  new[] { m_LFootBone.localPosition, m_RFootBone.localPosition, m_RootBone.localPosition, dir * 2f}.SelectMany(v => new[] { v.x, v.y, v.z }).ToArray();
        vector = pose.feature;

        for (int i = 0; i < m_Features.Length; i++)
        {
            int c = 0;
            m_Features[i].m_FeatureVector = new float[15];
            AddVector3ToFeatureVector(vector.m_FeatureVector, vector.m_CurrentLFootVel, ref c);
            AddVector3ToFeatureVector(vector.m_FeatureVector, vector.m_CurrentRFootVel, ref c);
            AddVector3ToFeatureVector(vector.m_FeatureVector, vector.m_CurrentLFootPos, ref c);
            AddVector3ToFeatureVector(vector.m_FeatureVector, vector.m_CurrentRFootPos, ref c);
            AddVector3ToFeatureVector(vector.m_FeatureVector, vector.m_CurrentHipVel, ref c);
            //AddVector3ToFeatureVector(vector.m_FeatureVector, dir * 5f, ref c);

            //    for (int j = 0; j < 3; j++)
            //    {
            //        AddVector3ToFeatureVector(vector.m_FeatureVector, dir, ref c);
            //    }
        }

        foreach (var p in m_Poses)
        {
            //if (Vector3.Dot(m_Poses[vector.poseIndex].dir, dir) < 0.5f)
            //    continue;

            float c = Cost(vector, p.feature);

            if (dist > c)
            {
                dist = c;
                i = p.feature.poseIndex;
            }
        }

        UnityEngine.Debug.Log($"Total: {i}");

        foreach (var bone in bones)
        {
            if (m_Poses[i].rootBone.name == bone.name)
            {
                bone.rotation = m_Poses[i].rootRot;
                bone.position = new Vector3(bone.position.x + m_Poses[i].deltaPos.x, m_Poses[i].testHipHeight, bone.position.z + m_Poses[i].deltaPos.z);
                continue;
            }

            for (int j = 0; j < m_Poses[i].m_Bones.Length; j++)
            {
                if (m_Poses[i].m_Bones[j].m_Name != bone.name)
                    continue;

                bone.localPosition = m_Poses[i].m_Bones[j].m_BonePos;
                bone.localRotation = m_Poses[i].m_Bones[j].m_BoneRot;
            }
        }

        pose = m_Poses[i];
        nextTime += interval;
        float wait = nextTime - (float)timer.Elapsed.TotalSeconds;

        //i++;

        if (wait > 0f)
        {
            yield return new WaitForSecondsRealtime(wait);
            //yield return null;
            testCo = null;
        }
        else
        {
            yield return null;
            testCo = null;
        }

    }

    private float Cost(FeatureVector currentPose, FeatureVector testingPose)
    {
        test = true;
        float total = 0f;

        int currentIndex = currentPose.poseIndex;
        int testingIndex = testingPose.poseIndex;
        float diff = testingIndex - currentIndex;

        if (diff <= 0 && diff >= -8)
        {
            diff = 50 / _PoseFavourWeight;
        }
        else if (diff > 5)
        {
            diff = 25 / _PoseFavourWeight;
        }
        else
        {
            diff = 0;
        }

        for (int j = 0; j < currentPose.m_FeatureVector.Length; j++)
        {
            //UnityEngine.Debug.Log($"INdex: {j} Total: {total} Extra Cost: {diff} Current: {currentPose.m_FeatureVector[j]} Testing: {testingPose.m_FeatureVector[j]}");
            total += Mathf.Sqrt((currentPose.m_FeatureVector[j] - testingPose.m_FeatureVector[j]) * (currentPose.m_FeatureVector[j] - testingPose.m_FeatureVector[j]));
        }

        total += diff;

        UnityEngine.Debug.Log($"T: {total} Extra Cost: {diff} Diff : {testingIndex - currentIndex} Current: {currentIndex} Testing: {testingIndex}");

        return total;
    }

    private void AddVector3ToFeatureVector(float[] fv, Vector3 v, ref int i)
    {
        fv[i++] = v.x;
        fv[i++] = v.y;
        fv[i++] = v.z;
    }
}
