using Lib.DataFlow;
using UnityEditor;
using UnityEngine;

namespace Unknown
{
    internal class GizmosHolder : MonoBehaviour
    {
#if UNITY_EDITOR
        [CustomEditor(typeof(GizmosHolder))]
        class Ed : Editor
        {
            void OnSceneGUI()
            {
                var runner = (GizmosHolder) target;
                Handles.BeginGUI();
                runner._handles?.Next();
                Handles.EndGUI();
            }
        }
#endif
        IPub _handles;
        IPub _gizmo;

        public void Init(IPub pubGizmo, IPub pubHandles)
        {
            _gizmo = pubGizmo;
            _handles = pubHandles;
        }

        void OnDrawGizmos()
        {
            _gizmo?.Next();
        }
    }
}