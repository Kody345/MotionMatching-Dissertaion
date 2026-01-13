using System.Numerics;
using UnityEngine;

namespace MMSystem
{

    public class PoseSearch
    {
        private Vector<Pose> m_Poses;
        public void Init(Vector<Pose> poses) 
        {
            m_Poses = poses;
        }

        public void TestSearch(Goal goal, Pose currentPose) 
        {

        }

        //Compute how far each joint and their vector and trajectory
        //
        private void ComputeCost() { }
    }
}
