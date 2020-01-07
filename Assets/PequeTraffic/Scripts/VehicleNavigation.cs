using System.Collections;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

namespace Peque.Traffic
{
    public class VehicleNavigation : WaypointNavigator
    {
        public float movementSpeed = 1;
        public float movementRotation = 1;
        public bool braking = false;

        public MeshRenderer[] stopSignals;
        public FrontSensor frontSensor;
        
        private WaypointNavigator waypointNavigator;
        new private Rigidbody rigidbody;
        private CarController carController;

        void Awake() {
            waypointNavigator = GetComponent<WaypointNavigator>();
            rigidbody = GetComponent<Rigidbody>();
            carController = GetComponent<CarController>();

            showStopSignals(false);
        }

        private void Update() {
            detectFreeFalling();

            if (reachedDestination) {
                waypointNavigator.getNextWaypoint();

                // if after requesting a new waypoint we didnt get one, show stop signals
                showStopSignals(reachedDestination);

                if (reachedDestination) {
                    hardBrake();
                    return;
                }
            }

            // if while moving we detect a person, stop
            switch (frontSensor.detectedElementType) {
                case FrontSensor.Element.Person:
                    hardBrake();
                    return;
                case FrontSensor.Element.Vehicle:
                    Vector3 infrontVehicleSpeed = frontSensor.detectedElement.GetComponent<Rigidbody>().velocity;

                    // seems stopped, better stop too
                    if (infrontVehicleSpeed.x < 0.4f && infrontVehicleSpeed.y < 0.4f && infrontVehicleSpeed.z < 0.4f) {
                        hardBrake();
                        return;
                    }
                    // adjust speed to not collide
                    moveToWaypoint(frontSensor.detectedElement.GetComponent<VehicleNavigation>().movementSpeed - 1);
                    break;
            }

            moveToWaypoint(movementSpeed);
        }

        void moveToWaypoint (float speed) {
            if (braking) {
                StopCoroutine(AddDrag());
            }

            Vector3 direction = destination - transform.position;

            destination.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * speed);

            if (direction != Vector3.zero) {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 4);
            }
        }

        void hardBrake () {
            if (braking) {
                return;
            }
            showStopSignals(true);

            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.drag = 0;

            //StartCoroutine(AddDrag());
        }

        IEnumerator AddDrag(float multiplier = 1f) {
            braking = true;

            while (rigidbody.drag < 10) {
                rigidbody.drag = Time.deltaTime * multiplier;
                yield return null;
            }

            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.drag = 0;
            braking = false;
        }

        void detectFreeFalling () {
            // looks like went under the map
            if (rigidbody.velocity.y < -100) {
                if (waypointNavigator.currentWaypoint) {
                    waypointNavigator.currentWaypoint.occupied = false; // release the waypoint
                }
                Destroy(gameObject);
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