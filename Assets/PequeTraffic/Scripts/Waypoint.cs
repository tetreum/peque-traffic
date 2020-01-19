using System.Collections.Generic;
using UnityEngine;

namespace Peque.Traffic
{ 
    public class Waypoint : MonoBehaviour
    {
        public Waypoint previousWaypoint;
        public Waypoint nextWaypoint;

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
        public float width = 1f;
        
        [Range(0f, 1f)]
        public float branchRatio = 0.5f;
        public List<Waypoint> branches = new List<Waypoint>();

        [Range(1, 200)]
        public int minSpeed = 10;

        [Range(1, 200)]
        public int maxSpeed = 50;

        [HideInInspector]
        public Semaphore relatedSemaphore;
        [HideInInspector]
        public int semaphorePath;

        public Vector3 GetCenterPosition() {
            return transform.position;
        }

        public Vector3 GetPosition() {
            Vector3 minBound = transform.position + transform.right * width / 2f;
            Vector3 maxBound = transform.position - transform.right * width / 2f;

            return Vector3.Lerp(minBound, maxBound, Random.Range(0f, 1f));
        }

        public void Awake() {
            findAdditiveLinks();
        }

        public void findAdditiveLinks() {
            if (previousAdditiveWaypoint != null && previousWaypoint == null) {
                GameObject additiveWaypoint = GameObject.Find(previousAdditiveWaypoint);

                if (additiveWaypoint) {
                    previousWaypoint = additiveWaypoint.GetComponent<Waypoint>();
                    previousWaypoint.findAdditiveLinks();
                }
            }
            if (nextAdditiveWaypoint != null && nextWaypoint == null) {
                GameObject additiveWaypoint = GameObject.Find(nextAdditiveWaypoint);

                if (additiveWaypoint) {
                    nextWaypoint = additiveWaypoint.GetComponent<Waypoint>();
                    nextWaypoint.findAdditiveLinks();
                }
            }
        }
    }
}
