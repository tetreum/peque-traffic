using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Peque.Traffic
{
    [System.Serializable]
    public class WaypointData
    {
        public WaypointData previousWaypoint;
        public WaypointData nextWaypoint;

        [Tooltip("If the previous waypoint is in a different scene, write down it's name here")]
        public string previousAdditiveWaypoint;
        [Tooltip("If the next waypoint is in a different scene, write down it's name here")]
        public string nextAdditiveWaypoint;

        public bool singleDirection = false;

        [HideInInspector]
        public bool reserved = false;
        [HideInInspector]
        public bool occupied = false;

        [Range(0f, 5f)]
        public float width = 2.44f;

        [Range(0f, 1f)]
        public float branchRatio = 0.5f;

        [System.NonSerialized]
        public List<WaypointData> branches = new List<WaypointData>();

        [Range(1, 200)]
        public int minSpeed = 5;

        [Range(1, 200)]
        public int maxSpeed = 10;

        [HideInInspector]
        public Semaphore relatedSemaphore;
        [HideInInspector]
        public int semaphorePath;
        [HideInInspector]
        public int pathId;
        [HideInInspector]
        public Vector3 centerPosition;
        [HideInInspector]
        public Vector3 position;
        [HideInInspector]
        public string name;

        public void findAdditiveLinks() {
            if (previousAdditiveWaypoint != null || nextAdditiveWaypoint != null) {
                AdditiveWaypointsManager.Instance.waypoints.Add(this);
            }

            if (previousAdditiveWaypoint != null && previousWaypoint == null) {
                WaypointData additiveWaypoint = AdditiveWaypointsManager.Instance.getByName(previousAdditiveWaypoint);

                if (additiveWaypoint != null) {
                    previousWaypoint = additiveWaypoint;
                    previousWaypoint.findAdditiveLinks();
                }
            }
            if (nextAdditiveWaypoint != null && nextWaypoint == null) {
                WaypointData additiveWaypoint = AdditiveWaypointsManager.Instance.getByName(nextAdditiveWaypoint);

                if (additiveWaypoint != null) {
                    nextWaypoint = additiveWaypoint;
                    nextWaypoint.findAdditiveLinks();
                }
            }
        }
    }
}
