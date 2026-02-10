using System.Numerics;
using UnityEngine;

namespace MMSystem
{
    public class PoseSearch
    {
        /*public FeatureVector SearchFeatureTree(FeatureNode rootNode, FeatureVector fv)
        {
            bool isSearching = true;
            int dimensionIndex = 0;
            float bestCost = 10000000f;
            FeatureNode bestNode = new FeatureNode();
            FeatureNode currentNode = rootNode;

            while (isSearching)
            {
                int currentIndex = fv.poseIndex;
                int testingIndex = currentNode.m_Feature.poseIndex;
                float diff = testingIndex - currentIndex;

                Debug.Log($"CI: {currentIndex} TI: {testingIndex} Diff: {diff}");

                if (diff <= 0 && diff >= -15)
                {
                    diff = 10f / 8;
                }
                else if (diff > 5)
                {
                    diff = 8f / 8;
                }
                else
                {
                    diff = 0f;
                }

                if (currentNode.m_Feature.m_FeatureVector[dimensionIndex] < fv.m_FeatureVector[dimensionIndex])
                {
                    
                    if (CalculatePoseCost(fv, currentNode.m_Feature) + diff < bestCost)
                    {
                        if (currentNode.m_Feature.poseIndex != fv.poseIndex)
                        {
                            Debug.Log($"Right Side - Pose Index: {currentNode.m_Feature.poseIndex} Cost: {CalculatePoseCost(fv, currentNode.m_Feature)} Diff: {diff}");
                            bestNode = currentNode;
                            bestCost = CalculatePoseCost(fv, currentNode.m_Feature);
                        }
                    }

                    if (currentNode.m_RightNode == null)
                    {
                        if (currentNode.m_LeftNode != null)
                            currentNode = currentNode.m_LeftNode;
                    }
                    else
                    {
                        currentNode = currentNode.m_RightNode;
                    }

                    //Debug.Log($"Right Side - Pose Index: {currentNode.m_Feature.poseIndex} Diff: {diff}");
                }
                else
                {
                    if (CalculatePoseCost(fv, currentNode.m_Feature) + diff < bestCost)
                    {
                        //Debug.Log(CalculatePoseCost(fv, currentNode.m_Feature) + diff);
                        if (currentNode.m_Feature.poseIndex != fv.poseIndex)
                        {
                            Debug.Log($"Left Side - Pose Index: {currentNode.m_Feature.poseIndex} Cost: {CalculatePoseCost(fv, currentNode.m_Feature)} Diff: {diff}");

                            bestNode = currentNode;
                            bestCost = CalculatePoseCost(fv, currentNode.m_Feature);
                        }
                    }

                    if (currentNode.m_LeftNode == null)
                    {
                        if (currentNode.m_RightNode != null)
                            currentNode = currentNode.m_RightNode;
                    }
                    else 
                    {
                        currentNode = currentNode.m_LeftNode;
                    }


                }
                dimensionIndex++;

                if (dimensionIndex >= fv.m_FeatureVector.Length)
                    dimensionIndex = 0;

                if (currentNode.m_LeftNode == null && currentNode.m_RightNode == null)
                {
                    isSearching = false;
                }
            }


            return bestNode.m_Feature;
        }*/

        public FeatureVector SearchFeatureArray(FeatureVector[] features, FeatureVector fv, Weightings weights, float mag)
        {
            float total = 100000000.0f;
            FeatureVector chosenFeature = new FeatureVector();

            for (int i = 0; i < features.Length; i++)
            {
                int currentIndex = fv.poseIndex;
                int testingIndex = features[i].poseIndex;
                float diff = testingIndex - currentIndex;

                float time = Mathf.Abs(diff - 2) * 2f;

                if (diff <= 0 && diff >= -15)
                {
                    diff = 10f / weights.PoseFavourWeight;
                }
                else if (diff > 5)
                {
                    diff = 5f / weights.PoseFavourWeight;
                }
                else
                {
                    diff = 0f;
                }

                float cost = CalculatePoseCost(fv, features[i], weights, mag);
                cost += diff;

                //Debug.Log($"Current: {fv.poseIndex} Index: {features[i].poseIndex} Cost: {cost}");

                if (cost <= total)
                {
                    chosenFeature = features[i];
                    total = cost; 
                }
            }

            Debug.Log($"Index: {chosenFeature.poseIndex} Cost: {total}");

            return chosenFeature;
        }

        private float CalculatePoseCost(FeatureVector fv, FeatureVector tester, Weightings weights, float magnitude)
        {
            float total = 0.0f;

            if (fv.poseIndex == tester.poseIndex)
                return 1000000f;
            
            AddVector3Cost(weights.PosWeight, fv.m_CurrentLFootPos, tester.m_CurrentLFootPos, ref total, magnitude);
            AddVector3Cost(weights.PosWeight, fv.m_CurrentRFootPos, tester.m_CurrentRFootPos, ref total, magnitude);
            AddVector3Cost(weights.VelWeight, fv.m_CurrentLFootVel, tester.m_CurrentLFootVel, ref total, magnitude);
            AddVector3Cost(weights.VelWeight, fv.m_CurrentRFootVel, tester.m_CurrentRFootVel, ref total, magnitude);
            AddVector3Cost(1f, fv.m_CurrentHipPos, tester.m_CurrentHipPos, ref total, magnitude);
            AddVector3Cost(1f, fv.m_CurrentHipVel, tester.m_CurrentHipVel, ref total, magnitude);

            for (int i = 0; i < fv.trajectories.Length; i++)
            {
                AddVector3Cost(weights.TrajectoryWeighting, fv.trajectories[i].m_FuturePos, tester.trajectories[i].m_FuturePos, ref total, magnitude);
            }

            //Debug.Log($"Current Index: {fv.poseIndex} Test Index: {tester.poseIndex} Total: {total}");

            return total;
        }

        private void AddVector3Cost(float weighting, UnityEngine.Vector3 c, UnityEngine.Vector3 t, ref float total, float mag)
        {
            c /= mag;
            t /= mag;
            total += weighting * Mathf.Sqrt((c.x - t.x) * (c.x - t.x));
            total += weighting * Mathf.Sqrt((c.y - t.y) * (c.y - t.y));
            total += weighting * Mathf.Sqrt((c.z - t.z) * (c.z - t.z));
        }
    }
}
