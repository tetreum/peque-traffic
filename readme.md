![Preview](https://cldup.com/dTxpPi9lDf.thumb.png)

# PequeTraffic

Yet Another attempt to build a traffic system in Unity.
Fork/Core from [Game Dev Guide - Building a Traffic System in Unity](https://www.youtube.com/watch?v=MXCZ-n5VyJc)
Uses waypoint system rather than NavMesh.

### Download

You've got 3 options:
1. (Recommended for development) Clone this repo by using `git clone `
1. Download the entire repository as [zip file]() and uncompress it somewhere else
2. Download the unitypackage. (quite stupid as it's not production ready for any game)

### Usage

Please check the included demo scene.

### Recommendations

- Disable 3G gizmos for better editing

### ToDo

- Make pedestrians & vehicles respect other entities space
- Make vehicles not jump between lines
- Go multithread


### Changelog
- Vehicles traffic
- Semaphores
- Root autoselection when opening WaypointManager
- Multiple pedestrian prefabs
- Character Navigation Controller
- When creating new waypoints reuse previous created waypoints data (so you can go faster)