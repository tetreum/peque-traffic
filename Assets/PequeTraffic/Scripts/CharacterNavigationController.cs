using UnityEngine;

namespace Peque.Traffic
{ 
    public class CharacterNavigationController : WaypointNavigator
    {
        public float movementSpeed = 1;
        public float movementRotation = 1;
        public float stopDistance = 1f;
        public float stoppingThreshold = 1.5f;

        private Animator animator;
        private WaypointNavigator waypointNavigator;

        private void Awake() {
            animator = GetComponent<Animator>();
            waypointNavigator = GetComponent<WaypointNavigator>();
        }

        private void Update() {
            if (reachedDestination) {
                waypointNavigator.getNextWaypoint();

                // if after requesting a new waypoint we didnt get one, stop moving animation
                if (reachedDestination) {
                    animator.SetFloat("Speed", 0);
                }
                return;
            }

            Vector3 direction = destination - transform.position;
            float distance = direction.magnitude;

            float sD = !reachedDestination ? stopDistance : stopDistance * stoppingThreshold;

            if (distance > sD) {
                animator.SetFloat("Speed", movementSpeed);
                transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * movementSpeed);

                if (direction != Vector3.zero) {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 4);
                }
            } else {
                reachedDestination = true;
            }
        }
    }
}
