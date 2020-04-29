using System.Collections.Generic;
using UnityEngine;

namespace Peque.Traffic
{
    public class Waypoint : MonoBehaviour
    {
        public Waypoint previousWaypoint;
        public Waypoint nextWaypoint;
        public WaypointData data;

        public List<Waypoint> branches = new List<Waypoint>();

        /**
         * To improve FPS migrate all waypoints data to a class that doesn't extend from Monobehaviour and
         * later delete all waypoints from scene
        */
        private void Awake() {
            data.name = name;
            data.centerPosition = GetCenterPosition();
            data.position = GetPosition();
            data.pathId = transform.root.GetInstanceID();

            if (previousWaypoint) {
                data.previousWaypoint = previousWaypoint.data;
            }

            if (nextWaypoint) {
                data.nextWaypoint = nextWaypoint.data;
            }

            foreach (Waypoint branch in branches) {
                data.branches.Add(branch.data);
            }
        }

        public void Start() {
            // run this on start as AdditiveWaypointsManager instances in Awake
            if (AdditiveWaypointsManager.Instance != null) {
                data.findAdditiveLinks();
            }
        }

        public Vector3 GetCenterPosition() {
            return transform.position;
        }

        public Vector3 GetPosition() {
            Vector3 minBound = transform.position + transform.right * data.width / 2f;
            Vector3 maxBound = transform.position - transform.right * data.width / 2f;

            return Vector3.Lerp(minBound, maxBound, Random.Range(0f, 1f));
        }
    }
}
