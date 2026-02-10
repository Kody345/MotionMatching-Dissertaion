using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MMSystem
{
    public class HierarchyDatabase
    {
        private Transform m_Root;
        private Transform m_LFoot;
        private Transform m_RFoot;

        private Transform[] m_AllBones;
        public void MakeDatabase(Animator anim, Archtype data, Dataset dataHolder, Transform RootBone, Transform LFoot, Transform RFoot)
        {

            if (RootBone == null)
                return;

            if (LFoot == null)
                return;

            if(RFoot == null)
                return;

            m_LFoot = LFoot;
            m_RFoot = RFoot;
            m_Root = RootBone;
            dataHolder.m_Magnitude = -1;

            m_AllBones = anim.GetComponentsInChildren<Transform>();

            int rootBoneIndex = -1;
            rootBoneIndex = System.Array.FindIndex(m_AllBones, b => b.name == m_Root.name);
            List<MMSystem.Pose> _poses = new List<MMSystem.Pose>();

            if (rootBoneIndex == -1)
                return;

            int poseCount = 0;

            dataHolder.m_Name = data.name;

            foreach (var mt in data.m_MotionTypes) 
            {
                //Dictionary<MotionEnum, FeatureNode> motionDic = new Dictionary<MotionEnum, FeatureNode>();
                Dictionary<MotionEnum, FeatureVector[]> motionDic = new Dictionary<MotionEnum, FeatureVector[]>();
                foreach (var m in mt.m_Motions) 
                {
                    List<AnimationClip> clips  = new List<AnimationClip>();
                    AddArrayOfMotionClips(m, clips);
                    float fullTime = 0.0f;
                    int startingframe = 0;

                    List<FeatureVector> _features = new List<FeatureVector>();

                    for (int i = 0; i < clips.Count(); i++)
                    {
                        fullTime += clips[i].length;

                        for (float t = 0.0f; t < clips[i].length; t += 1 / clips[i].frameRate)
                        {
                            int boneCount = 0;
                            clips[i].SampleAnimation(anim.gameObject, t);
                            m_AllBones = anim.GetComponentsInChildren<Transform>();
                            MMSystem.Pose currentPose = new MMSystem.Pose();
                            FeatureVector fv = new FeatureVector();

                            currentPose.m_Bones = new Bone[m_AllBones.Length];
                            for (int j = 0; j < m_AllBones.Length; j++)
                            {

                                Bone bone = new Bone();
                                bone.m_Name = m_AllBones[j].name;
                                bone.m_BoneWorldPos = m_AllBones[j].position;
                                bone.m_BonePos = m_AllBones[j].localPosition;
                                bone.m_BoneRot = m_AllBones[j].localRotation;

                                currentPose.m_Bones[boneCount] = bone;
                                boneCount++;
                            }
                            currentPose.frame = (int)Mathf.Round(clips[i].frameRate * t);
                            currentPose.rootBone = m_AllBones[rootBoneIndex];

                            if (currentPose.frame == 0)
                            {
                                //Re check if this is correct
                                startingframe = poseCount;
                                currentPose.rootPos = currentPose.rootBone.position;
                                currentPose.HipHeight = currentPose.rootBone.position.y;

                                currentPose.rootRot = currentPose.rootBone.rotation;

                                currentPose.deltaPos = Vector3.zero;
                                currentPose.deltaRot = Quaternion.identity;
                            }
                            else
                            {
                                currentPose.rootPos = currentPose.rootBone.position - _poses[startingframe].rootPos;
                                currentPose.HipHeight = currentPose.rootBone.position.y;

                                currentPose.rootRot = currentPose.rootBone.rotation;
                            }

                            currentPose.m_Time = t;
                            currentPose.m_Clip = clips[i];
                            currentPose.id = poseCount;
                            _poses.Add(currentPose);

                            fv.poseIndex = poseCount;

                            SetPositions(currentPose, ref fv);
                            _features.Add(fv);

                            poseCount++;
                        }
                        
                        

                        for (int j = 0; j < _poses.Count; j++)
                        {
                            MMSystem.Pose tempPose = new MMSystem.Pose();
                            tempPose = _poses[j];
                            if (_poses[j].frame == 0)
                            {
                                tempPose.rootPos = Vector3.zero;
                                tempPose.deltaPos = Vector3.zero;

                                tempPose.rootRot = Quaternion.identity;
                                tempPose.deltaRot = Quaternion.identity;
                                _poses[j] = tempPose;
                                continue;
                            }

                            MMSystem.Pose previousPose = new MMSystem.Pose();
                            previousPose = _poses[j - 1];

                            tempPose.deltaPos = tempPose.rootPos - previousPose.rootPos;
                            tempPose.deltaRot = Quaternion.Inverse(previousPose.rootRot) * tempPose.rootRot;

                            _poses[j] = tempPose;

                        }

                        
                    }

                    SetTrajectories(_features, _poses, 6, 12, 18);
                    SetVelocity(_poses, _features);

                    FindMaxMagnitude(_features, ref dataHolder.m_Magnitude);

                    for (int j = 0; j < _features.Count; j++)
                    {
                        //Debug.Log(_features[j].m_CurrentLFootVel);
                        FeatureVector fv = _features[j];
                        fv.m_FeatureVector = new float[27];
                        //fv.trajectories = new Trajectory[3];
                        int count = 0;

                        AddVector3ToFeatureVector(ref fv.m_FeatureVector, fv.m_CurrentLFootPos, ref count);
                        AddVector3ToFeatureVector(ref fv.m_FeatureVector, fv.m_CurrentRFootPos, ref count);
                        AddVector3ToFeatureVector(ref fv.m_FeatureVector, fv.m_CurrentLFootVel, ref count);
                        AddVector3ToFeatureVector(ref fv.m_FeatureVector, fv.m_CurrentRFootVel, ref count);
                        AddVector3ToFeatureVector(ref fv.m_FeatureVector, fv.m_CurrentHipPos, ref count);
                        AddVector3ToFeatureVector(ref fv.m_FeatureVector, fv.m_CurrentHipVel, ref count);

                        for (int i = 0; i < fv.trajectories.Length; i++) 
                        {
                            AddVector3ToFeatureVector(ref fv.m_FeatureVector, fv.trajectories[i].m_FuturePos, ref count);
                        }

                        _features[j] = fv;
                    }

                    NormalizeFeatures(_features, dataHolder.m_Magnitude);

                    FindMaxMagnitude(_features, ref dataHolder.m_Magnitude);

                    if (_features.Count > 0) 
                    {
                        motionDic.Add(m.m_Motion, _features.ToArray());
                    }
                }

                if (motionDic.Count > 0) 
                {
                    dataHolder.m_Features.Add(mt.m_MotionType, motionDic);
                }

            }

            dataHolder.m_Poses = _poses.ToArray();

        }

        private void CreateChildNodes(FeatureNode rootNode, List<FeatureVector> fv) 
        {
            int floatFeatureVectorIndex = 0;
            FeatureNode currentNode = rootNode;

            for (int i = 1; i < fv.Count; i++) 
            {
                bool nullChildNodeFound = true;

                while (nullChildNodeFound) 
                {
                    if(currentNode == null)
                        continue;

                    if (currentNode.m_Feature.m_FeatureVector[floatFeatureVectorIndex] < fv[i].m_FeatureVector[floatFeatureVectorIndex])
                    {
                        if (currentNode.m_RightNode == null)
                        {
                            currentNode.m_RightNode = new FeatureNode();
                            currentNode.m_RightNode.m_Feature = fv[i];
                            break;
                        }
                        else
                        {
                            currentNode = currentNode.m_RightNode;
                        }
                    }
                    else 
                    {
                        if (currentNode.m_LeftNode == null)
                        {
                            currentNode.m_LeftNode = new FeatureNode();
                            currentNode.m_LeftNode.m_Feature = fv[i];
                            break;
                        }
                        else
                        {
                            currentNode = currentNode.m_LeftNode;
                        }
                    }
                        floatFeatureVectorIndex++;
                    if (floatFeatureVectorIndex >= currentNode.m_Feature.m_FeatureVector.Length)
                        floatFeatureVectorIndex = 0;
                }
                currentNode = rootNode;

                
            }
        }

        private FeatureNode CreateTree(List<FeatureVector> fv)
        {
            FeatureNode root = new FeatureNode();
            root.m_Feature = fv[0];

            CreateChildNodes(root, fv);

            return root;
        }

        private void NormalizeFeatures(List<FeatureVector> fv, float mag) 
        {
            for (int i = 0; i < fv.Count; i++) 
            {
                for (int j = 0; j < fv[i].m_FeatureVector.Length; j++) 
                {
                    fv[i].m_FeatureVector[j] /= mag;
                }
            }
        }

        private void FindMaxMagnitude(List<FeatureVector> fv, ref float mag)
        {
            foreach (var f in fv) 
            {
                if (f.m_CurrentRFootVel.magnitude > mag)
                    mag = f.m_CurrentRFootVel.magnitude;
                if (f.m_CurrentLFootVel.magnitude > mag)
                    mag = f.m_CurrentLFootVel.magnitude;
                if (f.m_CurrentRFootPos.magnitude > mag)
                    mag = f.m_CurrentRFootPos.magnitude;
                if (f.m_CurrentLFootPos.magnitude > mag)
                    mag = f.m_CurrentLFootPos.magnitude;

                if (f.m_CurrentHipPos.magnitude > mag)
                    mag = f.m_CurrentHipPos.magnitude;

                for (int i = 0; i < f.trajectories.Length; i++) 
                {
                    if (f.trajectories[i].m_FuturePos.magnitude > mag)
                        mag = f.trajectories[i].m_FuturePos.magnitude;
                }
            }
        }

        private void SetTrajectories(List<FeatureVector> fv, List<MMSystem.Pose> poses, int frame1, int frame2, int frame3) 
        {
            for (int i = 0; i < fv.Count; i++) 
            {
                int count = 0;
                int tCount = 0;
                Vector3 fullDelta = new Vector3();
                FeatureVector f = new FeatureVector();
                f = fv[i];

                f.trajectories = new Trajectory[3];
                for (int j = 0; j < f.trajectories.Length; j++) { f.trajectories[j].m_FuturePos = Vector3.positiveInfinity; }

                for (int j = fv[i].poseIndex; j < poses.Count; j++) 
                {
                    fullDelta += poses[j].deltaPos;

                    if (frame1 == count)
                    {
                        f.trajectories[tCount].m_FuturePos = new Vector3(fullDelta.x, 0f, fullDelta.z);
                        tCount++;
                    }
                    if (frame2 == count)
                    {
                        f.trajectories[tCount].m_FuturePos = new Vector3(fullDelta.x, 0f, fullDelta.z);
                        tCount++;
                    }
                    if (frame3 == count)
                    {
                        f.trajectories[tCount].m_FuturePos = new Vector3(fullDelta.x, 0f, fullDelta.z);
                        tCount++;
                        break;
                    }
                    count++;
                }

                if (tCount == 0) 
                {
                    f.trajectories[tCount].m_FuturePos = new Vector3(f.m_CurrentHipPos.x, 0f, f.m_CurrentHipPos.z);
                    tCount++;
                }
                if (tCount == 1)
                {
                    f.trajectories[tCount].m_FuturePos = f.trajectories[0].m_FuturePos;
                    tCount++;
                }
                if (tCount == 2)
                {
                    f.trajectories[tCount].m_FuturePos = f.trajectories[1].m_FuturePos;
                    tCount++;
                }

                fv[i] = f;

            }
        }

        private void SetVelocity(List<MMSystem.Pose> poses, List<FeatureVector> fv) 
        {
            for (int i = 0; i < fv.Count; i++) 
            {
                FeatureVector f = new FeatureVector();
                f = fv[i];
                if (i >= fv.Count - 1) 
                {
                    f.m_CurrentLFootVel = fv[i - 1].m_CurrentLFootVel;
                    f.m_CurrentRFootVel = fv[i - 1].m_CurrentRFootVel;
                    f.m_CurrentHipVel = fv[i - 1].m_CurrentHipVel;

                    fv[i] = f;
                    continue;
                }

                f.m_CurrentLFootVel = fv[i + 1].m_CurrentLFootPos - fv[i].m_CurrentLFootPos;
                f.m_CurrentRFootVel = fv[i + 1].m_CurrentRFootPos - fv[i].m_CurrentRFootPos;
                f.m_CurrentHipVel = fv[i + 1].m_CurrentHipPos - fv[i].m_CurrentHipPos;
                fv[i] = f;
            }
        }

        private void SetPositions(MMSystem.Pose pose, ref FeatureVector fv) 
        {
            foreach (var bone in pose.m_Bones)
            {
                if (bone.m_Name == m_LFoot.name)
                {
                    fv.m_CurrentLFootPos = bone.m_BoneWorldPos;
                    continue;
                }
                if (bone.m_Name == m_RFoot.name)
                {
                    fv.m_CurrentRFootPos = bone.m_BoneWorldPos;
                    continue;
                }
                if (bone.m_Name == m_Root.name)
                {
                    fv.m_CurrentHipPos = bone.m_BoneWorldPos;
                    continue;
                }
            }
        }

        private void AddArrayOfMotionTypesClips(MotionType type, List<AnimationClip> clips) 
        {
            foreach (var m in type.m_Motions) 
            {
                for (int i = 0; i < m.m_Clips.Length; i++) 
                {
                    clips.Add(m.m_Clips[i]);
                }
            }
        }

        private void AddArrayOfMotionClips(Motion type, List<AnimationClip> clips)
        {
            clips.AddRange(type.m_Clips);
        }

        private void AddVector3ToFeatureVector(ref float[] fv, Vector3 v, ref int i) 
        {
            fv[i++] = v.x;
            fv[i++] = v.y;
            fv[i++] = v.z;
        }

        private FeatureNode CreatNode(FeatureVector fv) 
        {
            FeatureNode node = null;
            if (node != null)
            {
                node.m_LeftNode = null;
                node.m_RightNode = null;
                node.m_Feature = fv;
            }
            return node;
        }

        private void FindBiggestMagnitude(FeatureVector fv, ref float mag)
        {
            if (mag < fv.m_CurrentLFootVel.magnitude)
                mag = fv.m_CurrentLFootVel.magnitude;
            if (mag < fv.m_CurrentRFootVel.magnitude)
                mag = fv.m_CurrentRFootVel.magnitude;
            if (mag < fv.m_CurrentLFootPos.magnitude)
                mag = fv.m_CurrentLFootPos.magnitude;
            if (mag < fv.m_CurrentRFootPos.magnitude)
                mag = fv.m_CurrentRFootPos.magnitude;
            if (mag < fv.m_CurrentHipVel.magnitude)
                mag = fv.m_CurrentHipVel.magnitude;
            for (int i = 0; i < fv.trajectories.Length; i++)
            {
                if (mag < fv.trajectories[i].m_FuturePos.magnitude)
                    mag = fv.trajectories[i].m_FuturePos.magnitude;
            }
        }

        private void CalcRootToFoot(List<FeatureVector> fv)
        {
            for (int i = 0; i < fv.Count; i++)
            {
                FeatureVector f = fv[i];
                f.m_CurrentLFootPos = fv[i].m_CurrentLFootPos - fv[i].m_CurrentHipPos;
                f.m_CurrentRFootPos = fv[i].m_CurrentRFootPos - fv[i].m_CurrentHipPos;
                fv[i] = f;
            }
        }
    }
}