using UnityEngine;
using System.Collections;
using System;

namespace Peque.Traffic
{
    /**
     * Base from https://github.com/angelotadres/AICar/blob/master/Assets/Challenges/Challenge2/AICar.cs
     */
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

        public Transform COM;
        // index 0 & 1 are expected to be front wheels (0 left, 1 right)
        public WheelCollider[] wheelColliders;
        public GameObject[] wheelMeshes;

        public MeshRenderer[] stopSignals;
        public MeshRenderer[] leftSignals;
        public MeshRenderer[] rightSignals;
        public Sensor frontSensor;
        public Sensor rightSensor;
        public Sensor leftSensor;

        [HideInInspector]
        public int stopperId;
        [HideInInspector]
        public Sensor.Element? stoppedReason;
        [SerializeField]
        private float frontSecurityDistance = 5f;

        [SerializeField]
        private float steeringSharpness = 12.0f;

        // These variables are for the gears, the array is the list of ratios. The script uses the defined gear ratios to determine how much torque to apply to the wheels.
        [SerializeField]
        private float[] GearRatio;
        [SerializeField]
        private int CurrentGear = 0;
        public int maxSpeed = 5;

        // These variables are just for applying torque to the wheels and shifting gears.
        // using the defined Max and Min Engine RPM, the script can determine what gear the
        // car needs to be in.
        [SerializeField]
        private float EngineTorque = 90.0f;
        [SerializeField]
        private float BrakePower = 0f;
        [SerializeField]
        private float MaxEngineRPM = 3000.0f;
        [SerializeField]
        private float MinEngineRPM = 1000.0f;
        [SerializeField]
        public float AntiRoll = 5000.0f;

        [HideInInspector]
        public bool braking = false;

        private float EngineRPM = 0.0f;

        // input steer and input torque are the values substituted out for the player input. The 
        // "NavigateTowardsWaypoint" function determines values to use for these variables to move the car
        // in the desired direction.
        private float inputSteer = 0.0f;
        private float inputTorque = 0.0f;
        private WaypointNavigator waypointNavigator;
        private Rigidbody rigid;
        private AudioSource audioPlayer;
        private Sense _sense;

        private void Awake() {

            rightSensor.enabled = false;
            leftSensor.enabled = false;

            audioPlayer = GetComponent<AudioSource>();
            rigid = GetComponent<Rigidbody>();
            waypointNavigator = GetComponent<WaypointNavigator>();

            showStopSignals(false);
            updateSignalsStatus(true);
        }

        private void Start() {
            wheelColliders[0].attachedRigidbody.centerOfMass = COM.localPosition;
            rigid.centerOfMass = COM.localPosition;

            sense = getSense(destination - transform.position);
        }

        private void FixedUpdate() {

            detectFreeFalling();

            // this just checks if the car's position is near enough to a waypoint to count as passing it, if it is, then change the target waypoint to the
            // next in the list.
            if (reachedDestination) {
                waypointNavigator.getNextWaypoint();

                // if after requesting a new waypoint we didnt get one, show stop signals
                showStopSignals(reachedDestination);

                if (reachedDestination) {
                    hardBrake();
                    return;
                }
            }

            //float mph = rigid.velocity.magnitude * 2.237f;

            // This is to limit the maximum speed of the car, adjusting the drag probably isn't the best way of doing it,
            // but it's easy, and it doesn't interfere with the physics processing.
            rigid.drag = rigid.velocity.magnitude / 250f;

            if (!gotCollisions()) {
                int speed = currentWaypoint.minSpeed + UnityEngine.Random.Range(0, 20);

                if (speed > currentWaypoint.maxSpeed) {
                    speed = currentWaypoint.maxSpeed;
                }
                if (speed > maxSpeed) {
                    speed = maxSpeed;
                }
                moveToWaypoint(speed);

                sense = getSense(destination - transform.position);
            } else {
                status = Status.Stopped;
            }
        }

        void moveToWaypoint(int speed) {
            if (rigid && false) {
                rigidMove();
            } else {
                vectorMove(speed);
            }
            status = Status.Moving;
        }

        void vectorMove(int speed) {
            destination.y = transform.position.y;
            Vector3 direction = destination - transform.position;
            //float speed = EngineTorque / 20;
            transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * speed);

            if (direction != Vector3.zero) {
                Quaternion frontRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, frontRotation, Time.deltaTime * (speed / 2));

                //rotateWheels(frontRotation);
            } else {
                //rotateWheels(Quaternion.identity);
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

        Sensor detectCollisions() {
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
                            // their path doesn't intersect and they aren't in the same path, just it's just an opposite side vehicle
                        } else if (infrontVehicle.currentWaypoint != currentWaypoint &&
                            !lineSegmentsIntersect(infrontVehicle.transform.position, infrontVehicle.destination, transform.position, destination) &&
                            infrontVehicle.currentWaypoint.pathId != currentWaypoint.pathId
                            ) {
                            return false;
                            // they're colliding with each other, nearest one to its destination will continue
                        } else if (infrontVehicle.currentWaypoint != currentWaypoint &&
                            infrontVehicle.stoppedReason == Sensor.Element.Vehicle &&
                            infrontVehicle.stopperId == transform.GetInstanceID() &&
                            (infrontVehicle.destination - infrontVehicle.transform.position).magnitude > (destination - transform.position).magnitude
                            ) {
                            return false;
                        }

                        hardBrake();
                        return true;
                    }
                    // adjust speed to not collide
                    //moveToWaypoint(infrontVehicle.currentSpeed - 1);
                    return true;
            }
            return false;
        }

        // From https://www.edy.es/dev/2011/10/the-stabilizer-bars-creating-physically-realistic-stable-vehicles/
        void applyAntiRoll() {
            WheelHit hit;
            float travelL = 1.0f;
            float travelR = 1.0f;

            bool groundedL = wheelColliders[0].GetGroundHit(out hit);
            if (groundedL) {
                travelL = (-wheelColliders[0].transform.InverseTransformPoint(hit.point).y - wheelColliders[0].radius) / wheelColliders[0].suspensionDistance;
            }

            bool groundedR = wheelColliders[1].GetGroundHit(out hit);
            if (groundedR) {
                travelR = (-wheelColliders[1].transform.InverseTransformPoint(hit.point).y - wheelColliders[1].radius) / wheelColliders[1].suspensionDistance;
            }

            float antiRollForce = (travelL - travelR) * AntiRoll;

            if (groundedL) {
                rigid.AddForceAtPosition(wheelColliders[0].transform.up * -antiRollForce,
                wheelColliders[0].transform.position);
            }

            if (groundedR) {
                rigid.AddForceAtPosition(wheelColliders[1].transform.up * antiRollForce,
                wheelColliders[1].transform.position);
            }
        }

        void rotateWheelMeshes() {
            for (int i = 0; i < 4; i++) {
                Quaternion quat;
                Vector3 position;
                wheelColliders[i].GetWorldPose(out position, out quat);
                wheelMeshes[i].transform.position = position;
                wheelMeshes[i].transform.rotation = quat;
            }
        }

        private void ShiftGears() {
            // this funciton shifts the gears of the vehicle, it loops through all the gears, checking which will make
            // the engine RPM fall within the desired range. The gear is then set to this "appropriate" value.
            int AppropriateGear;
            if (EngineRPM >= MaxEngineRPM) {
                AppropriateGear = CurrentGear;

                for (int i = 0; i < GearRatio.Length; i++) {
                    if (wheelColliders[0].rpm * GearRatio[i] < MaxEngineRPM) {
                        AppropriateGear = i;
                        break;
                    }
                }

                CurrentGear = AppropriateGear;
            }

            if (EngineRPM <= MinEngineRPM) {
                AppropriateGear = CurrentGear;

                for (int j = GearRatio.Length - 1; j >= 0; j--) {
                    if (wheelColliders[0].rpm * GearRatio[j] > MinEngineRPM) {
                        AppropriateGear = j;
                        break;
                    }
                }

                CurrentGear = AppropriateGear;
            }
        }

        private void rigidMove() {
            if (braking) {
                StopCoroutine(AddDrag());
            }
            // now we just find the relative position of the waypoint from the car transform,
            // that way we can determine how far to the left and right the waypoint is.

            Vector3 RelativeWaypointPosition = transform.InverseTransformPoint(new Vector3(destination.x, transform.position.y, destination.z));

            // by dividing the horizontal position by the magnitude, we get a decimal percentage of the turn angle that we can use to drive the wheels
            inputSteer = RelativeWaypointPosition.x / RelativeWaypointPosition.magnitude;

            // now we do the same for torque, but make sure that it doesn't apply any engine torque when going around a sharp turn...
            if (Mathf.Abs(inputSteer) < 0.5) {
                inputTorque = RelativeWaypointPosition.z / RelativeWaypointPosition.magnitude - Mathf.Abs(inputSteer);
            } else {
                inputTorque = 0.0f;
            }

            // Compute the engine RPM based on the average RPM of the two wheels, then call the shift gear function
            EngineRPM = (wheelColliders[0].rpm + wheelColliders[1].rpm) / 2f * GearRatio[CurrentGear];
            ShiftGears();

            // set the audio pitch to the percentage of RPM to the maximum RPM plus one, this makes the sound play
            // up to twice it's pitch, where it will suddenly drop when it switches gears.
            audioPlayer.pitch = Mathf.Abs(EngineRPM / MaxEngineRPM) + 1.0f;
            // this line is just to ensure that the pitch does not reach a value higher than is desired.
            if (audioPlayer.pitch > 2.0f) {
                audioPlayer.pitch = 2.0f;
            }

            // finally, apply the values to the wheels.	The torque applied is divided by the current gear, and
            // multiplied by the calculated AI input variable.
            wheelColliders[0].motorTorque = EngineTorque / GearRatio[CurrentGear] * inputTorque;
            wheelColliders[1].motorTorque = EngineTorque / GearRatio[CurrentGear] * inputTorque;

            wheelColliders[0].brakeTorque = BrakePower;
            wheelColliders[1].brakeTorque = BrakePower;

            // the steer angle is an arbitrary value multiplied by the calculated AI input.
            wheelColliders[0].steerAngle = (steeringSharpness) * inputSteer;
            wheelColliders[1].steerAngle = (steeringSharpness) * inputSteer;

            applyAntiRoll();
            rotateWheelMeshes();
        }

        // only enable lateral sensors when vehicle is turning
        void updateSensorsStatus() {
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

        void hardBrake() {
            if (braking) {
                return;
            }
            showStopSignals(true);

            if (rigid) {
                rigid.velocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
                rigid.drag = 0;
            }

            status = Status.Stopped;
            //StartCoroutine(AddDrag());
        }

        IEnumerator AddDrag(float multiplier = 1f) {
            braking = true;

            while (rigid.drag < 10) {
                rigid.drag = Time.deltaTime * multiplier;
                yield return null;
            }

            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
            rigid.drag = 0;
            braking = false;
        }

        void detectFreeFalling() {
            // looks like went under the map
            if (rigid && rigid.velocity.y < -100) {
                if (waypointNavigator.currentWaypoint != null) {
                    waypointNavigator.currentWaypoint.occupied = false; // release the waypoint
                }
                Destroy(gameObject);
            }
        }

        void showStopSignals(bool show) {
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

        /**
         * From https://www.reddit.com/r/gamedev/comments/7ww4yx/whats_the_easiest_way_to_check_if_two_line/
         */
        public static bool lineSegmentsIntersect(Vector2 lineOneA, Vector2 lineOneB, Vector2 lineTwoA, Vector2 lineTwoB) {
            return (((lineTwoB.y - lineOneA.y) * (lineTwoA.x - lineOneA.x) > (lineTwoA.y - lineOneA.y) * (lineTwoB.x - lineOneA.x)) != ((lineTwoB.y - lineOneB.y) * (lineTwoA.x - lineOneB.x) > (lineTwoA.y - lineOneB.y) * (lineTwoB.x - lineOneB.x)) && ((lineTwoA.y - lineOneA.y) * (lineOneB.x - lineOneA.x) > (lineOneB.y - lineOneA.y) * (lineTwoA.x - lineOneA.x)) != ((lineTwoB.y - lineOneA.y) * (lineOneB.x - lineOneA.x) > (lineOneB.y - lineOneA.y) * (lineTwoB.x - lineOneA.x)));
        }
    }
}