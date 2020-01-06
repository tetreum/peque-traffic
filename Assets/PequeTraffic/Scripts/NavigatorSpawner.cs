using System.Collections;
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

        void Start() {
            StartCoroutine(spawn());
        }

        IEnumerator spawn () {
            int count = 0;
            int attempts = 0;

            while (count < numberToSpawn) {
                Waypoint randomWaypoint = getRandomWaypoint();

                // seems like there are no available slots
                if (randomWaypoint == null) {
                    Debug.Log("No available slots found for " + transform.name + " waiting a second");
                    attempts++;

                    if (attempts == maxAttempts) {
                        Debug.Log("No available slots found for " + transform.name + ", stopping spawner.");
                        break;
                    }

                    yield return new WaitForSeconds(1);
                    continue;
                }

                GameObject obj = Instantiate(prefabs[Random.Range(0, prefabs.Length)]);

                Vector3 spawnPosition = randomWaypoint.transform.position;
                spawnPosition.y += 0.5f;

                obj.transform.position = spawnPosition;

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
        }

        Waypoint getRandomWaypoint (int attempt = 0) {
            Transform child = transform.GetChild(Random.Range(0, transform.childCount - 1));
            Waypoint waypoint = child.GetComponent<Waypoint>();

            // to avoid overlapping check if current or nearest waypoints are already occupied
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