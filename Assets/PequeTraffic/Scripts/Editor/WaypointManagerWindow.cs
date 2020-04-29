using UnityEngine;
using UnityEditor;

namespace Peque.Traffic
{ 
    public class WaypointManagerWindow : EditorWindow
    {
        public Transform waypointRoot;
        private Waypoint previousWaypoint;

        [MenuItem("Window/PequeTraffic/Waypoint Editor")]
        public static void Open () {
            GetWindow<WaypointManagerWindow>().onOpen();
        }

        public void onOpen () {
            // attempt to autoselect the root
            if (waypointRoot != null || Selection.activeGameObject == null) {
                return;
            }

            if (Selection.activeGameObject.name.Contains("Waypoints")) {
                waypointRoot = Selection.activeGameObject.transform;
            } else if (Selection.activeGameObject.name.Contains("waypoint ")) {
                waypointRoot = Selection.activeGameObject.transform.parent;
            }
        }

        private void OnGUI() {
            SerializedObject obj = new SerializedObject(this);

            EditorGUILayout.PropertyField(obj.FindProperty("waypointRoot"));

            if (waypointRoot == null) {
                EditorGUILayout.HelpBox("Root transform must be selected. Please assign a root transform", MessageType.Warning);
            } else {
                EditorGUILayout.BeginVertical("box");
                DrawButtons();
                EditorGUILayout.EndVertical();
            }

            obj.ApplyModifiedProperties();
        }

        void DrawButtons () {
            if (GUILayout.Button("Create waypoint")) {
                CreateWaypoint();
            }

            if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Waypoint>()) {
                if (GUILayout.Button("Add branch")) {
                    CreateBranch();
                }
                if (GUILayout.Button("Create waypoint before")) {
                    CreateWaypointBefore();
                }
                if (GUILayout.Button("Create waypoint after")) {
                    CreateWaypointAfter();
                }
                if (GUILayout.Button("Remove waypoint")) {
                    RemoveWaypoint();
                }
            }
        }

        private Waypoint spawnWaypoint () {
            GameObject waypointObject = new GameObject("waypoint " + waypointRoot.childCount, typeof(Waypoint));
            waypointObject.transform.SetParent(waypointRoot, false);

            Waypoint waypoint = waypointObject.GetComponent<Waypoint>();

            // use previous waypoint data
            if (previousWaypoint != null) {
                waypoint.data.maxSpeed = previousWaypoint.data.maxSpeed;
                waypoint.data.minSpeed = previousWaypoint.data.minSpeed;
                waypoint.data.width = previousWaypoint.data.width;
                waypoint.data.singleDirection = previousWaypoint.data.singleDirection;
            } else if (waypointRoot.name.Contains("Vehicles")) {
                waypoint.data.singleDirection = true;
            }

            previousWaypoint = waypoint;
            return waypoint;
        }

        void CreateWaypoint() {
            Waypoint waypoint = spawnWaypoint();

            // use previous waypoint width
            if (previousWaypoint != null) {
                waypoint.data.maxSpeed = previousWaypoint.data.maxSpeed;
                waypoint.data.minSpeed = previousWaypoint.data.minSpeed;
                waypoint.data.width = previousWaypoint.data.width;
            }

            if (waypointRoot.childCount > 1) {
                waypoint.previousWaypoint = waypointRoot.GetChild(waypointRoot.childCount - 2).GetComponent<Waypoint>();
                waypoint.previousWaypoint.nextWaypoint = waypoint;

                // Place the waypoint at the last position
                waypoint.transform.position = waypoint.previousWaypoint.transform.position;
                waypoint.transform.forward = waypoint.previousWaypoint.transform.forward;
            }

            Selection.activeGameObject = waypoint.gameObject;
        }

        void CreateWaypointBefore() {
            Waypoint waypoint = spawnWaypoint();

            Waypoint selectedWaypoint = Selection.activeGameObject.GetComponent<Waypoint>();

            waypoint.transform.position = selectedWaypoint.transform.position;
            waypoint.transform.forward = selectedWaypoint.transform.forward;
            waypoint.data.width = selectedWaypoint.data.width;
            waypoint.data.maxSpeed = selectedWaypoint.data.maxSpeed;
            waypoint.data.minSpeed = selectedWaypoint.data.minSpeed;

            if (selectedWaypoint.previousWaypoint != null) {
                waypoint.previousWaypoint = selectedWaypoint.previousWaypoint;
                selectedWaypoint.previousWaypoint.nextWaypoint = waypoint;
            }

            waypoint.nextWaypoint = selectedWaypoint;
            selectedWaypoint.previousWaypoint = waypoint;
            waypoint.transform.SetSiblingIndex(selectedWaypoint.transform.GetSiblingIndex());
            Selection.activeGameObject = waypoint.gameObject;
        }

        void CreateWaypointAfter() {
            Waypoint waypoint = spawnWaypoint();

            Waypoint selectedWaypoint = Selection.activeGameObject.GetComponent<Waypoint>();

            waypoint.transform.position = selectedWaypoint.transform.position;
            waypoint.transform.forward = selectedWaypoint.transform.forward;
            waypoint.data.width = selectedWaypoint.data.width;
            waypoint.data.maxSpeed = selectedWaypoint.data.maxSpeed;
            waypoint.data.minSpeed = selectedWaypoint.data.minSpeed;

            waypoint.previousWaypoint = selectedWaypoint;

            if (selectedWaypoint.nextWaypoint != null) {
                selectedWaypoint.nextWaypoint.previousWaypoint = waypoint;
                waypoint.nextWaypoint = selectedWaypoint.nextWaypoint;
            }

            selectedWaypoint.nextWaypoint = waypoint;
            waypoint.transform.SetSiblingIndex(selectedWaypoint.transform.GetSiblingIndex());
            Selection.activeGameObject = waypoint.gameObject;
        }

        void RemoveWaypoint() {
            Waypoint selectedWaypoint = Selection.activeGameObject.GetComponent<Waypoint>();

            if (selectedWaypoint.nextWaypoint != null) {
                selectedWaypoint.nextWaypoint.previousWaypoint = selectedWaypoint.previousWaypoint;
            }

            if (selectedWaypoint.previousWaypoint != null) {
                selectedWaypoint.previousWaypoint.nextWaypoint = selectedWaypoint.nextWaypoint;
                Selection.activeGameObject = selectedWaypoint.previousWaypoint.gameObject;
            }

            DestroyImmediate(selectedWaypoint.gameObject);
        }

        void CreateBranch () {
            Waypoint waypoint = spawnWaypoint();

            Waypoint branchedFrom = Selection.activeGameObject.GetComponent<Waypoint>();
            branchedFrom.branches.Add(waypoint);

            waypoint.transform.position = branchedFrom.transform.position;
            waypoint.transform.forward = branchedFrom.transform.forward;

            Selection.activeGameObject = waypoint.gameObject;
        }
    }
}