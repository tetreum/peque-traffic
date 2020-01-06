using UnityEngine;

namespace Peque.Traffic
{
    public class Semaphore : MonoBehaviour
    {
        public enum Status
        {
            Green = 1,
            Yellow = 2,
            Red = 3,
        }
        public int secondsInGreen = 10;
        public int secondsInYellow = 10;
        public int secondsInRed = 60;
        public int startingPath = 1;
        private Status _path1Status = Status.Green;
        public Status path1Status {
            get {
                return _path1Status;
            }
            set {
                _path1Status = value;

                foreach (MeshRenderer renderer in greensPath1) {
                    renderer.enabled = false;
                }
                foreach (MeshRenderer renderer in yellowsPath1) {
                    renderer.enabled = false;
                }
                foreach (MeshRenderer renderer in redsPath1) {
                    renderer.enabled = false;
                }
                foreach (MeshRenderer renderer in greensPath2) {
                    renderer.enabled = false;
                }
                foreach (MeshRenderer renderer in yellowsPath2) {
                    renderer.enabled = false;
                }
                foreach (MeshRenderer renderer in redsPath2) {
                    renderer.enabled = false;
                }

                switch (value) {
                    case Status.Green:
                        remainingSeconds = secondsInGreen;

                        foreach (MeshRenderer renderer in greensPath1) {
                            renderer.enabled = true;
                        }
                        foreach (MeshRenderer renderer in redsPath2) {
                            renderer.enabled = true;
                        }
                        break;
                    case Status.Yellow:
                        remainingSeconds = secondsInYellow;

                        foreach (MeshRenderer renderer in yellowsPath1) {
                            renderer.enabled = true;
                        }
                        foreach (MeshRenderer renderer in redsPath2) {
                            renderer.enabled = true;
                        }

                        break;
                    case Status.Red:
                        remainingSeconds = secondsInRed;

                        foreach (MeshRenderer renderer in redsPath1) {
                            renderer.enabled = true;
                        }
                        foreach (MeshRenderer renderer in greensPath2) {
                            renderer.enabled = true;
                        }

                        break;
                }
            }
        }

        public Status path2Status {
            get {
                switch (path1Status) {
                    case Status.Green:
                    case Status.Yellow:
                        return Status.Red;
                    case Status.Red:
                        if (remainingSeconds < secondsInYellow) {
                            return Status.Yellow;
                        }
                        return Status.Green;
                }

                return Status.Red;
            }
        }

        public MeshRenderer[] greensPath1;
        public MeshRenderer[] yellowsPath1;
        public MeshRenderer[] redsPath1;

        public MeshRenderer[] greensPath2;
        public MeshRenderer[] yellowsPath2;
        public MeshRenderer[] redsPath2;

        public Waypoint[] path1StopWaypoints;
        public Waypoint[] path2StopWaypoints;

        public Semaphore copyThisSemaphore;

        
        private int _remainingSeconds = 0;
        public int remainingSeconds {
            get {
                return _remainingSeconds;
            }
            set {
                _remainingSeconds = value;

                if (path1Status == Status.Red && _remainingSeconds < secondsInYellow) {
                    foreach (MeshRenderer renderer in greensPath2) {
                        renderer.enabled = false;
                    }
                    foreach (MeshRenderer renderer in yellowsPath2) {
                        renderer.enabled = true;
                    }
                }
            }
        }

        [HideInInspector]
        public Semaphore slave;

        private void Awake() {
            if (copyThisSemaphore) {
                copyThisSemaphore.slave = this;
                path1Status = copyThisSemaphore.path1Status;
                return;
            }

            // to trigger setter methods
            path1Status = path1Status;

            if (path1StopWaypoints != null) {
                foreach (var waypoint in path1StopWaypoints) {
                    waypoint.relatedSemaphore = this;
                    waypoint.semaphorePath = 1;
                }
            }
            if (path2StopWaypoints != null) {
                foreach (var waypoint in path2StopWaypoints) {
                    waypoint.relatedSemaphore = this;
                    waypoint.semaphorePath = 2;
                }
            }
        }

        private void Start() {
            SemaphoreManager.Instance.semaphores.Add(this);
        }

        public Status getStatus (Waypoint waypoint) {
            if (waypoint.semaphorePath == 1) {
                return path1Status;
            }
            return path2Status;
        }

        public void changeStatus () {
            switch(path1Status) {
                case Status.Green:
                    path1Status = Status.Yellow;
                    break;
                case Status.Yellow:
                    path1Status = Status.Red;
                    break;
                case Status.Red:
                    path1Status = Status.Green;
                    break;
            }

            if (slave) {
                slave.path1Status = path1Status;
                slave.remainingSeconds = remainingSeconds;
            }
        }
    }
}