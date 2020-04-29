using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Peque.Traffic
{ 
    public class NavigatorSpawner : MonoBehaviour
    {
        public enum Direction
        {
            Both = 2,
            Normal = 0,
            Inverse = 1,
        }
        public GameObject[] prefabs;
        public int numberToSpawn = 5;
        [Tooltip("Number of attempts that spawner will try to instantiate the requested amount of prefabs.")]
        public int maxAttempts = 10;
        public Direction allowedDirection = Direction.Both;
        [Tooltip("Waypoint gameobjects will be removed at runtime to improve fps")]
        public bool optimizeOnRuntime = true;
        [Tooltip("Waypoint gameobjects removal at runtime will also be done in Editor")]
        public bool optimizeOnEditorToo = true;

        private List<WaypointData> waypoints;

        void Start() {
            getChildWaypoints();

            if (numberToSpawn > 0 && waypoints.Count > 0) {
                StartCoroutine(spawn());
            }
        }

        private void getChildWaypoints() {
            waypoints = new List<WaypointData>();

            foreach (Waypoint waypoint in transform.GetComponentsInChildren<Waypoint>()) {
                waypoints.Add(waypoint.data);
            }
        }

        IEnumerator spawn () {
            int count = 0;
            int attempts = 0; 

            while (count < numberToSpawn) {
                WaypointData randomWaypoint = getRandomWaypoint();

                // seems like there are no available slots
                if (randomWaypoint == null) {
                    //Debug.Log("No available slots found for " + transform.name + " waiting a second");
                    attempts++;

                    if (attempts == maxAttempts) {
                        //Debug.Log("No available slots found for " + transform.name + ", stopping spawner.");
                        break;
                    }

                    yield return new WaitForSeconds(1);
                    continue;
                }
				// make sure this waypoint isn't reused for spawning more entities
                randomWaypoint.occupied = true;

                GameObject obj = Instantiate(prefabs[Random.Range(0, prefabs.Length)]);

                Vector3 spawnPosition = randomWaypoint.centerPosition;
                spawnPosition.y += 0.5f;

                obj.transform.position = spawnPosition;

				// Point spawned entities looking at their next waypoint
                Vector3 lookPos = spawnPosition;
                if (randomWaypoint.nextWaypoint != null) {
                    lookPos = randomWaypoint.nextWaypoint.centerPosition;
                } else if(randomWaypoint.previousWaypoint != null) {
                    lookPos = randomWaypoint.previousWaypoint.centerPosition;
                }

                lookPos.y = obj.transform.position.y;
                obj.transform.LookAt(lookPos);


                int direction;

                if (allowedDirection == Direction.Both) {
                    direction = Mathf.RoundToInt(Random.Range(0f, 1f));
                } else {
                    direction = (int)allowedDirection;
                }

                obj.GetComponent<WaypointNavigator>().init(direction, randomWaypoint);

                yield return new WaitForEndOfFrame();
                count++;
            }

            if (optimizeOnRuntime && (!Application.isEditor || (Application.isEditor && optimizeOnEditorToo))) {
                removeWaypointsGameobjects();
            }
        }

        void removeWaypointsGameobjects () {
            foreach (Transform child in transform) {
                Destroy(child.gameObject);
            }
        }

        WaypointData getRandomWaypoint (int attempt = 0) {
            WaypointData waypoint = waypoints[Random.Range(0, waypoints.Count - 1)];

            // to avoid overlapping on spawn, check if current or nearest waypoints are already occupied
            if (waypoint.occupied || (waypoint.nextWaypoint != null && waypoint.nextWaypoint.occupied) || (waypoint.previousWaypoint != null && waypoint.previousWaypoint.occupied)) {
                attempt++;

                if (attempt == maxAttempts) {
                    return null;
                }

                return getRandomWaypoint(attempt);
            }

            return waypoint;
        }
    }
}