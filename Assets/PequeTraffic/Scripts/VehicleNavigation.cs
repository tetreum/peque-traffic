using UnityEngine;

namespace Peque.Traffic
{
    public class VehicleNavigation : WaypointNavigator
    {
        public float movementSpeed = 1;
        public float movementRotation = 1;
        public float stopDistance = 1f;
        public float stoppingThreshold = 1.5f;

        public MeshRenderer[] stopSignals;
        
        private WaypointNavigator waypointNavigator;

        void Awake() {
            waypointNavigator = GetComponent<WaypointNavigator>();

            showStopSignals(false);
        }

        private void Update() {
            if (reachedDestination) {
                waypointNavigator.getNextWaypoint();

                // if after requesting a new waypoint we didnt get one, show stop signals
                showStopSignals(reachedDestination);
                return;
            }

            Vector3 direction = destination - transform.position;
            float distance = direction.magnitude;

            float sD = !reachedDestination ? stopDistance : stopDistance * stoppingThreshold;

            if (distance > sD) {
                transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * movementSpeed);

                if (direction != Vector3.zero) {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 4);
                }
            } else {
                reachedDestination = true;
            }
        }

        void showStopSignals (bool show) {
            if (stopSignals != null) {
                foreach (MeshRenderer mesh in stopSignals) {
                    mesh.enabled = show;
                }
            }
        }
    }
}