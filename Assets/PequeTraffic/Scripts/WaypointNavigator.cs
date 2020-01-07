using UnityEngine;

namespace Peque.Traffic
{
    public class WaypointNavigator : MonoBehaviour
    {
        public float stopDistance = 1f;
        public float stoppingThreshold = 1.5f;

        [HideInInspector]
        public bool reachedDestination {
            get {
                Vector3 direction = destination - transform.position;

                return (direction.magnitude <= stopDistance);
            }
        }
        
        public Waypoint currentWaypoint;

        protected Vector3 destination;

        int direction;

        public void init (int direction, Waypoint startPoint) {
            this.direction = direction;
            currentWaypoint = startPoint;
            SetDestination(currentWaypoint.GetPosition());
        }

        public void getNextWaypoint() {
            bool shouldBranch = false;

            // dont give a new waypoint if current one has a stopper and it's not an exit point (100% ratio)
            if (currentWaypoint.branchRatio < 1f && currentWaypoint.relatedSemaphore != null && currentWaypoint.relatedSemaphore.getStatus(currentWaypoint) == Semaphore.Status.Red) {
                return;
            }

            currentWaypoint.occupied = false;

            if (currentWaypoint.branches != null && currentWaypoint.branches.Count > 0) {
                // handle situations when developer left a path with a branch and no direct continuity
                if ((direction == 0 && currentWaypoint.nextWaypoint == null) || (direction == 1 && currentWaypoint.previousWaypoint == null)) {
                    shouldBranch = true;
                } else {
                    shouldBranch = Random.Range(0f, 1f) <= currentWaypoint.branchRatio ? true : false;
                }
            }

            if (shouldBranch) {
                currentWaypoint = currentWaypoint.branches[Random.Range(0, currentWaypoint.branches.Count - 1)];
            } else {
                if (direction == 0) {
                    if (currentWaypoint.nextWaypoint != null) {
                        currentWaypoint = currentWaypoint.nextWaypoint;
                    } else {
                        currentWaypoint = currentWaypoint.previousWaypoint;
                        direction = 1;
                    }
                } else {
                    if (currentWaypoint.previousWaypoint != null) {
                        currentWaypoint = currentWaypoint.previousWaypoint;
                    } else {
                        currentWaypoint = currentWaypoint.nextWaypoint;
                        direction = 0;
                    }
                }
            }

            currentWaypoint.occupied = true;

            SetDestination(currentWaypoint.singleDirection ? currentWaypoint.GetCenterPosition() : currentWaypoint.GetPosition());
        }
        protected void SetDestination(Vector3 destination) {
            this.destination = destination;
        }
    }
}
