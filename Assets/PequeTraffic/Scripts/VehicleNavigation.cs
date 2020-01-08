using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

namespace Peque.Traffic
{
    public class VehicleNavigation : WaypointNavigator
    {
        public enum Status
        {
            Moving = 1,
            Stopped = 2
        }
        public enum Sense
        {
            Forward = 0,
            Right = 1,
            Left = -1
        }

        [HideInInspector]
        public Status status;
        public Sense sense {
            get {
                return _sense;
            }
            set {
                if (value != _sense) {
                    _sense = value;
                    updateSensorsStatus();
                    updateSignalsStatus();
                }
            }
        }
        
        public float movementSpeed = 1;
        public float movementRotation = 1;
        public float frontSecurityDistance = 5f;

        [HideInInspector]
        public bool braking = false;
        [HideInInspector]
        public int stopperId;
        public Sensor.Element? stoppedReason;

        public MeshRenderer[] stopSignals;
        public MeshRenderer[] leftSignals;
        public MeshRenderer[] rightSignals;
        public Sensor frontSensor;
        public Sensor rightSensor;
        public Sensor leftSensor;

        private Sense _sense;
        private WaypointNavigator waypointNavigator;
        new private Rigidbody rigidbody;
        private CarController carController;

        void Awake() {
            // teak a little vehicle settings to not make them look equal
            /* disabled while developing, to ease debugging
            frontSecurityDistance += Random.Range(-1f, 1f);
            movementSpeed += Random.Range(-0.5f, 1f);
            */
            rightSensor.enabled = false;
            leftSensor.enabled = false;

            waypointNavigator = GetComponent<WaypointNavigator>();
            rigidbody = GetComponent<Rigidbody>();
            carController = GetComponent<CarController>();

            showStopSignals(false);
            updateSignalsStatus(true);
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

            if (!gotCollisions()) {
                moveToWaypoint(movementSpeed);
            }
        }

        bool gotCollisions() {
            Sensor sensor = detectCollisions();

            if (sensor == null) {
                stoppedReason = null;
                stopperId = 0;
                return false;
            }

            try {
                stoppedReason = sensor.detectedElementType;
                stopperId = sensor.detectedElement.GetInstanceID();
            } catch (InvalidOperationException) {
                return false; // it seems that there is no longer a collision
            }
            

            return true;
        }

        Sensor detectCollisions () {
            if (detectCollisions(frontSensor)) return frontSensor;
            if (rightSensor.enabled && detectCollisions(rightSensor)) return rightSensor;
            if (leftSensor.enabled && detectCollisions(leftSensor)) return leftSensor;

            return null;
        }

        bool detectCollisions(Sensor sensor) {
            // if while moving we detect a person, stop
            switch (sensor.detectedElementType) {
                case Sensor.Element.Person:
                    hardBrake();
                    return true;
                case Sensor.Element.Vehicle:
                    VehicleNavigation infrontVehicle = sensor.detectedElement.GetComponent<VehicleNavigation>();

                    // seems stopped or too near
                    if (infrontVehicle.status == Status.Stopped ||
                        infrontVehicle.braking == true ||
                        (infrontVehicle.transform.position - transform.position).magnitude < frontSecurityDistance) {

                        // oops they're trying to reach the same waypoint
                        // the nearest one will continue
                        if (infrontVehicle.currentWaypoint == currentWaypoint &&
                            (destination - infrontVehicle.transform.position).magnitude > (destination - transform.position).magnitude
                            ) {
                            return false;
                        } else if (infrontVehicle.currentWaypoint != currentWaypoint &&
                            infrontVehicle.stoppedReason == Sensor.Element.Vehicle &&
                            infrontVehicle.stopperId == transform.GetInstanceID() && // they're colliding with each other
                            (destination - infrontVehicle.transform.position).magnitude > (destination - transform.position).magnitude
                            ) {
                            return false;
                        }

                        hardBrake();
                        return true;
                    }
                    // adjust speed to not collide
                    moveToWaypoint(infrontVehicle.movementSpeed - 1);
                    return true;
            }
            return false;
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

                sense = getSense(direction);
            }
            status = Status.Moving;
        }

        // only enable lateral sensors when vehicle is turning
        void updateSensorsStatus () {
            switch (sense) {
                case Sense.Forward:
                    rightSensor.enabled = false;
                    leftSensor.enabled = false;
                    break;
                case Sense.Right:
                    rightSensor.enabled = true;
                    leftSensor.enabled = false;
                    break;
                case Sense.Left:
                    rightSensor.enabled = false;
                    leftSensor.enabled = true;
                    break;
            }
        }

        void updateSignalsStatus(bool forceOff = false) {
            foreach (MeshRenderer mesh in leftSignals) {
                mesh.enabled = (sense == Sense.Left && !forceOff);
            }
            foreach (MeshRenderer mesh in rightSignals) {
                mesh.enabled = (sense == Sense.Right && !forceOff);
            }
        }

        void hardBrake () {
            if (braking) {
                return;
            }
            showStopSignals(true);

            if (rigidbody) {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.drag = 0;
            }

            status = Status.Stopped;
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
            if (rigidbody && rigidbody.velocity.y < -100) {
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

        /**
         * From https://forum.unity.com/threads/left-right-test-function.31420/
        */
        Sense getSense(Vector3 direction) {
            Vector3 right = Vector3.Cross(transform.up, transform.forward);        // right vector
            float dir = Vector3.Dot(right, direction);
            
            if (dir > 1f) {
                return Sense.Right;
            } else if (dir < -1f) {
                return Sense.Left;
            } else {
                return Sense.Forward; // it could also be backward
            }
        }
    }
}