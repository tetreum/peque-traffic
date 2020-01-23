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

        [HideInInspector]
        public int currentSpeed = 0;
        public int maxSpeed = 5;
        public float movementRotation = 1;
        public float frontSecurityDistance = 5f;

        [HideInInspector]
        public bool braking = false;
        [HideInInspector]
        public int stopperId;
        [HideInInspector]
        public Sensor.Element? stoppedReason;

        public MeshRenderer[] stopSignals;
        public MeshRenderer[] leftSignals;
        public MeshRenderer[] rightSignals;
        public Sensor frontSensor;
        public Sensor rightSensor;
        public Sensor leftSensor;
        public Transform frontCast;
        public Transform rightCast;
        public Transform leftCast;

        [SerializeField] [Range(0, 1)] private float m_CautiousSpeedFactor = 0.05f;               // percentage of max speed to use when being maximally cautious
        [SerializeField] [Range(0, 180)] private float m_CautiousMaxAngle = 50f;                  // angle of approaching corner to treat as warranting maximum caution
        [SerializeField] private float m_CautiousMaxDistance = 100f;                              // distance at which distance-based cautiousness begins
        [SerializeField] private float m_CautiousAngularVelocityFactor = 30f;                     // how cautious the AI should be when considering its own current angular velocity (i.e. easing off acceleration if spinning!)
        [SerializeField] private float m_SteerSensitivity = 0.05f;                                // how sensitively the AI uses steering input to turn to the desired direction
        [SerializeField] private float m_AccelSensitivity = 0.04f;                                // How sensitively the AI uses the accelerator to reach the current desired speed
        [SerializeField] private float m_BrakeSensitivity = 1f;                                   // How sensitively the AI uses the brake to reach the current desired speed
        [SerializeField] private float m_LateralWanderDistance = 3f;                              // how far the car will wander laterally towards its target
        [SerializeField] private float m_LateralWanderSpeed = 0.1f;                               // how fast the lateral wandering will fluctuate
        [SerializeField] [Range(0, 1)] private float m_AccelWanderAmount = 0.1f;                  // how much the cars acceleration will wander
        [SerializeField] private float m_AccelWanderSpeed = 0.1f;
		private float m_RandomPerlin;

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
            m_RandomPerlin = UnityEngine.Random.value * 100;

            rightSensor.enabled = false;
            leftSensor.enabled = false;

            waypointNavigator = GetComponent<WaypointNavigator>();
            rigidbody = GetComponent<Rigidbody>();
            carController = GetComponent<CarController>();

            showStopSignals(false);
            updateSignalsStatus(true);

            TrafficManager.Instance.add(this);
        }

        private void OnDestroy() {
            TrafficManager.Instance.delete(this);
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
                int speed = currentWaypoint.minSpeed + UnityEngine.Random.Range(0, 20);

                if (speed > currentWaypoint.maxSpeed) {
                    speed = currentWaypoint.maxSpeed;
                }
                if (speed > maxSpeed) {
                    speed = maxSpeed;
                }
                currentSpeed = speed;
                moveToWaypoint(speed);
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
                            infrontVehicle.stopperId == transform.GetInstanceID() && // they're colliding with each other, nearest one to its destination will continue
                            (infrontVehicle.destination - infrontVehicle.transform.position).magnitude > (destination - transform.position).magnitude
                            ) {
                            return false;
                        }

                        hardBrake();
                        return true;
                    }
                    // adjust speed to not collide
                    moveToWaypoint(infrontVehicle.currentSpeed - 1);
                    return true;
            }
            return false;
        }

        void moveToWaypoint (int speed) {
            if (braking) {
                StopCoroutine(AddDrag());
            }
            status = Status.Moving;

            Vector3 direction = destination - transform.position;

            destination.y = transform.position.y;

            /**
             * To improve performance, near vehicles should be using rigidbodies
             * while far ones no.
             * But right now, i cant make rigid moving feel good
            */
            if (rigidbody && false) {
                Vector3 fwd = transform.forward;
                if (rigidbody.velocity.magnitude > carController.MaxSpeed * 0.1f) {
                    fwd = rigidbody.velocity;
                }

                float desiredSpeed = speed;

                float approachingCornerAngle = Vector3.Angle(currentWaypoint.transform.forward, fwd);

                // also consider the current amount we're turning, multiplied up and then compared in the same way as an upcoming corner angle
                float spinningAngle = rigidbody.angularVelocity.magnitude * m_CautiousAngularVelocityFactor;

                // if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount
                float cautiousnessRequired = Mathf.InverseLerp(0, m_CautiousMaxAngle,
                                                               Mathf.Max(spinningAngle,
                                                                         approachingCornerAngle));
                desiredSpeed = Mathf.Lerp(carController.MaxSpeed, carController.MaxSpeed * m_CautiousSpeedFactor,
                                          cautiousnessRequired);

                // use different sensitivity depending on whether accelerating or braking:
                float accelBrakeSensitivity = (desiredSpeed < carController.CurrentSpeed)
                                                  ? m_BrakeSensitivity
                                                  : m_AccelSensitivity;

                // decide the actual amount of accel/brake input to achieve desired speed.
                float accel = Mathf.Clamp((desiredSpeed - carController.CurrentSpeed) * accelBrakeSensitivity, -1, 1);

                // add acceleration 'wander', which also prevents AI from seeming too uniform and robotic in their driving
                // i.e. increasing the accel wander amount can introduce jostling and bumps between AI cars in a race
                accel *= (1 - m_AccelWanderAmount) +
                         (Mathf.PerlinNoise(Time.time * m_AccelWanderSpeed, m_RandomPerlin) * m_AccelWanderAmount);

                // calculate the local-relative position of the target, to steer towards
                Vector3 localTarget = transform.InverseTransformPoint(destination);

                // work out the local angle towards the target
                float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

                // get the amount of steering needed to aim the car towards the target
                float steer = Mathf.Clamp(targetAngle * m_SteerSensitivity, -1, 1) * Mathf.Sign(carController.CurrentSpeed);

                // feed input to the car controller.
                carController.Move(steer, accel, accel, 0f);
            } else {
                transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * speed);

                if (direction != Vector3.zero) {
                    Quaternion frontRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, frontRotation, Time.deltaTime * (speed / 2));

                    sense = getSense(direction);
                    rotateWheels(frontRotation);
                } else {
                    rotateWheels(Quaternion.identity);
                }
            }
        }

        void rotateWheels (Quaternion frontRotation) {
            GameObject[] meshes = carController.getWheelMeshes();
            float rotation = Time.deltaTime * currentSpeed * 360;
            int i = 0;

            frontRotation.eulerAngles = new Vector3(rotation, frontRotation.eulerAngles.y, frontRotation.eulerAngles.z);

            foreach (GameObject mesh in meshes) {
                if (i < 2) {
                    mesh.transform.rotation = frontRotation;
                } else {
                    mesh.transform.Rotate(rotation, 0, 0);
                }
                
                i++;
            }
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

            currentSpeed = 0;
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