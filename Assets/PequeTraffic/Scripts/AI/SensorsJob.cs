using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Peque.Traffic;

public class SensorsJob : MonoBehaviour
{
    public bool ready = true;
    public void start() {
        ready = false;

        int count = TrafficManager.Instance.vehicles.Count;
        var results = new NativeArray<RaycastHit>(count, Allocator.TempJob);
        var commands = new NativeArray<RaycastCommand>(count, Allocator.TempJob);
        var vehicleIndex = new NativeArray<int>(count, Allocator.TempJob);
        Vector3 from;
        Vector3 direction;
        int i = 0;

        foreach (VehicleNavigation vehicle in TrafficManager.Instance.vehicles.Values) {
            from = new Vector3(vehicle.transform.position.x + (vehicle.transform.GetComponent<BoxCollider>().size.z / 2), vehicle.transform.position.y, vehicle.transform.position.z + (vehicle.transform.forward.z * vehicle.transform.GetComponent<BoxCollider>().size.z / 2 + 0.1f));
            //from = new Vector3(vehicle.transform.position.x + (transform.forward.x * vehicle.transform.GetComponent<BoxCollider>().size.z), vehicle.transform.position.y, vehicle.transform.position.z + (vehicle.transform.forward.z * vehicle.transform.GetComponent<BoxCollider>().size.z / 2 + 0.1f));
            direction = vehicle.destination;
            direction.y = from.y;

            //commands[i] = new RaycastCommand(from, direction, 4);
            //Debug.DrawLine(from, direction, Color.green);

            switch (vehicle.sense) {
                case VehicleNavigation.Sense.Forward:
                    from = vehicle.frontCast.transform.position;
                    break;
                case VehicleNavigation.Sense.Right:
                    from = vehicle.rightCast.transform.position;
                    break;
                case VehicleNavigation.Sense.Left:
                    from = vehicle.leftCast.transform.position;
                    break;
            }

            commands[i] = new RaycastCommand(from, direction, 4);
            Debug.DrawLine(from, direction, Color.magenta);
            vehicleIndex[i] = vehicle.GetInstanceID();
            i++;
        }

        // Schedule the batch of raycasts
        JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 1, default(JobHandle));
        JobHandle.ScheduleBatchedJobs();

        // Wait for the batch processing job to complete
        handle.Complete();
        
        i = 0;
        while (i < count) {
            // vehicle got deleted while raycasting
            if (!TrafficManager.Instance.vehicles.ContainsKey(vehicleIndex[i]) || (results[i].collider == null && results[i].point == Vector3.zero && false)) {
                i++;
                continue;
            }
            VehicleNavigation vehicle = TrafficManager.Instance.vehicles[vehicleIndex[i]];

            RaycastHit hit = results[i];

            // no hit or colliding with itself
            if (hit.collider == null || hit.collider.transform.root.GetInstanceID() == vehicle.transform.root.GetInstanceID()) {
                Debug.Log("No hit");
                vehicle.frontSensor.detectedElement = null;
            } else {
                Debug.Log("Hit " + hit.collider.transform.root.name);
                vehicle.frontSensor.detectedElement = hit.collider.transform.root;
            }
            i++;
        }

        // Dispose the buffers
        results.Dispose();
        commands.Dispose();
        vehicleIndex.Dispose();
        
        ready = true;
    }
}