using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEditor.PlayerSettings;

namespace MMSystem
{
    public class PoseDataBase
    {
        private SearchAndSaveType m_SaveType;
        private Transform[] allBones;
        private Transform LFoot;
        private Transform RFoot;
        private Transform Root;

        public void SaveDataType(SearchAndSaveType type) { m_SaveType = type; }

        public bool SaveData()
        {
            return true;
        }

        public void ConvertData(MMSystem.Pose[] m_poses, MMSystem.FeatureVector[] m_features, AnimationClip[] clips, Animator anim, SearchAndSaveType type, Transform lFoot, Transform rFoot, Transform RootBone)
        {
            LFoot = lFoot;
            RFoot = rFoot;
            Root = RootBone;

            allBones = anim.GetComponentsInChildren<Transform>();
            int rootBoneIndex = -1;

            //Finds rootbone index
            for (int i = 0; i < allBones.Length; i++)
            {
                if (allBones[i].name == RootBone.name)
                {
                    rootBoneIndex = i;
                    break;
                }
            }

            if (rootBoneIndex == -1) { return; }

            int poseCount = 0;
            int startingFrame = 0;

            for (int i = 0; i < clips.Length; i++)
            {
                for (float t = 0f; t <= clips[i].length; t += 1f / clips[i].frameRate)
                {
                    //test = t * clips[i].frameRate;
                    clips[i].SampleAnimation(anim.gameObject, t);
                    allBones = anim.GetComponentsInChildren<Transform>();
                    MMSystem.Pose pose = new MMSystem.Pose();
                    

                    pose.m_Time = t;
                    int boneCount = 0;
                    pose.m_Bones = new Bone[allBones.Length - rootBoneIndex];

                    for (int j = rootBoneIndex; j < allBones.Length; j++)
                    {
                        Bone bone = new Bone();
                        bone.m_Name = allBones[j].name;
                        bone.m_BoneWorldPos = allBones[j].position;
                        bone.m_BonePos = allBones[j].localPosition;
                        bone.m_BoneRot = allBones[j].localRotation;
                        pose.m_Bones[boneCount] = bone;
                        boneCount++;
                    }

                    if (poseCount >= m_poses.Length)
                        continue;

                    pose.frame = (int)(clips[i].frameRate * t);

                    pose.rootBone = allBones[rootBoneIndex];

                    if (pose.frame == 0)
                    {
                        startingFrame = poseCount;
                        pose.rootPos = pose.rootBone.position;
                        pose.testHipHeight = pose.rootBone.position.y;

                        pose.rootRot = pose.rootBone.rotation;

                        m_poses[i].deltaPos = Vector3.zero;
                        m_poses[i].deltaRot = Quaternion.identity;
                    }
                    else
                    {
                        pose.rootPos = pose.rootBone.position - m_poses[startingFrame].rootPos;
                        pose.testHipHeight = pose.rootBone.position.y;

                        pose.rootRot = pose.rootBone.rotation;
                    }

                    pose.m_Time = t;
                    pose.m_Clip = clips[i];
                    pose.id = poseCount;
                    pose.frame = (int)(pose.m_Clip.frameRate * pose.m_Time);
                    m_poses[poseCount] = pose;
                    m_features[poseCount].poseIndex = poseCount;

                    poseCount++;
                }
            }

            

            for (int i = 0; i < m_poses.Length; i++)
            {
                m_poses[i].m_FuturePos = new Trajectory[3];

                if (m_poses[i].frame >= (int)(m_poses[i].m_Clip.frameRate * m_poses[i].m_Clip.length))
                    continue;

                Trajectory t = new Trajectory();
                Vector3 pos = new Vector3();

                foreach (var bone in m_poses[i].m_Bones)
                {
                    if (bone.m_Name == RootBone.name)
                    {
                        pos = bone.m_BonePos;
                        break;
                    }
                }

                t.m_FuturePos = new Vector3(pos.x, pos.y, pos.z);
                m_poses[i].m_FuturePos[0] = t;


            }

            for (int i = 0; i < m_poses.Length; i++)
            {
                int frame = 0;

                for (int j = 1; j < 3; j++)
                {
                    frame += (int)((1f / 3f) / (1f / m_poses[i].m_Clip.frameRate));
                    if ((frame + m_poses[i].frame) > (int)(m_poses[i].m_Clip.length * m_poses[i].m_Clip.frameRate))
                    {
                        break;
                    }

                    if ((i + frame) >= m_poses.Length)
                        break;

                    if (m_poses[i].m_Clip.name != m_poses[i + frame].m_Clip.name)
                        break;

                    m_poses[i].m_FuturePos[j].m_FuturePos = m_poses[i + frame].m_FuturePos[0].m_FuturePos;
                }

                m_poses[i].dir = Vector3.Normalize(m_poses[i].m_FuturePos[1].m_FuturePos - m_poses[i].m_FuturePos[0].m_FuturePos);
            }

            //Setting Delta Motion
            foreach (var clip in clips)
            {
                for (int i = 0; i < m_poses.Length; i++)
                {
                    if (clip.name != m_poses[i].m_Clip.name)
                        continue;

                    if (m_poses[i].frame == 0)
                    {
                        //m_poses[i].rootPos = new Vector3(0f, m_poses[i].rootPos.y, 0f);
                        m_poses[i].rootPos = Vector3.zero;
                        m_poses[i].deltaPos = Vector3.zero;

                        m_poses[i].rootRot = Quaternion.identity;
                        m_poses[i].deltaRot = Quaternion.identity;
                        continue;
                    }
                    m_poses[i].deltaPos = m_poses[i].rootPos - m_poses[i - 1].rootPos;
                    m_poses[i].deltaRot = Quaternion.Inverse(m_poses[i - 1].rootRot) * m_poses[i].rootRot;
                }
            }

            for (int i = 0; i < m_features.Length; i++) 
            {
                SetupPos(ref m_features[i], m_poses);
                Trajectory(m_features[i], m_poses);
            }

            SetuptVel(m_features, m_poses);

            

            for (int i = 0; i < m_features.Length; i++) 
            {
                int c = 0;
                m_features[i].m_FeatureVector = new float[6];
                AddVector3ToFeatureVector(m_features[i].m_FeatureVector, m_features[i].m_CurrentLFootVel, ref c);
                AddVector3ToFeatureVector(m_features[i].m_FeatureVector, m_features[i].m_CurrentRFootVel, ref c);

                m_poses[m_features[i].poseIndex].feature = m_features[i];
            }

        }

        private void SetuptVel(FeatureVector[] fv, MMSystem.Pose[] poses) 
        {
            for (int i = 0; i < fv.Length; i++) 
            {
                if ((int)(((poses[fv[i].poseIndex].m_Clip.length * 30) - 2) - (poses[fv[i].poseIndex].m_Time * 30)) <= 0)
                    continue;

                fv[i].m_CurrentLFootVel = new Vector3();
                fv[i].m_CurrentRFootVel = new Vector3();
                fv[i].m_CurrentHipVel = new Vector3();

                fv[i].m_CurrentLFootVel = fv[i + 1].m_CurrentLFootPos - fv[i].m_CurrentLFootPos;
                fv[i].m_CurrentRFootVel = fv[i + 1].m_CurrentRFootPos - fv[i].m_CurrentRFootPos;
                fv[i].m_CurrentHipVel = fv[i + 1].m_CurrentHipPos - fv[i].m_CurrentHipPos;
            }
        }

        private void SetupPos(ref FeatureVector fv, MMSystem.Pose[] poses) 
        {
            foreach (var bone in poses[fv.poseIndex].m_Bones)
            {
                if (bone.m_Name == LFoot.name)
                {
                    fv.m_CurrentLFootPos = bone.m_BoneWorldPos;
                    continue;
                }
                if (bone.m_Name == RFoot.name) 
                {
                    fv.m_CurrentRFootPos = bone.m_BoneWorldPos;
                    continue;
                }
                if (bone.m_Name == Root.name) 
                {
                    fv.m_CurrentHipPos = bone.m_BoneWorldPos;
                    continue;
                }
            }
        }

        private void Trajectory(FeatureVector fv, MMSystem.Pose[] poses) 
        {
            int frameCount;

            if ((int)(((poses[fv.poseIndex].m_Clip.length * 30) - 2) - (poses[fv.poseIndex].m_Time * 30)) > 2)
            {
                
                frameCount = 3;
            }
            else
            {
                frameCount = (int)(((poses[fv.poseIndex].m_Clip.length * 30) - 2) - (poses[fv.poseIndex].m_Time * 30));
            }

            if (frameCount <= 0)
                return;
            
            fv.trajectories = new Trajectory[frameCount];

            for (int i = 0; i < frameCount; i++) 
            {
                fv.trajectories[i] = new Trajectory();
                fv.trajectories[i].m_FuturePos = new Vector3(poses[fv.poseIndex + i].rootPos.x, 0f, poses[fv.poseIndex].rootPos.z);
            }
        }

        private void AddVector3ToFeatureVector(float[] fv, Vector3 v, ref int i) 
        {
            fv[i++] = v.x;
            fv[i++] = v.y;
            fv[i++] = v.z;
        }

        private void AddQuaternianToFeatureVector(float[] fv, Quaternion q, ref int i)
        {

        }

        private void AddVector2ToFeatureVector(float[] fv, Vector2 v, ref int i)
        {

        }

    }
}