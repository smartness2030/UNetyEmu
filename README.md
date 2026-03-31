# Multi UAVs Preflight Planning in a Shared and Dynamic Airspace

[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux-blue.svg)]()
[![Engine](https://img.shields.io/badge/Engine-Unity-black.svg)]()
[![Language](https://img.shields.io/badge/Language-C%23-purple.svg)]()

Preflight planning for large-scale Unmanned Aerial Vehicle (UAV) fleets in dynamic, shared airspace presents significant challenges, including temporal No-Fly Zones (NFZs), heterogeneous vehicle profiles, and strict delivery deadlines. While Multi-Agent Path Finding (MAPF) provides a formal framework, existing methods often lack the scalability and flexibility required for real-world Unmanned Traffic Management (UTM). We propose DTAPP-IICR: a Delivery-Time Aware Prioritized Planning method with Incremental and Iterative Conflict Resolution. Our framework first generates an initial solution by prioritizing missions based on urgency. Secondly, it computes roundtrip trajectories using SFIPP-ST, a novel 4D single-agent planner (Safe Flight Interval Path Planning with Soft and Temporal Constraints). SFIPP-ST handles heterogeneous UAVs, strictly enforces temporal NFZs, and models inter-agent conflicts as soft constraints. Subsequently, an iterative Large Neighborhood Search, guided by a geometric conflict graph, efficiently resolves any residual conflicts. A completeness-preserving directional pruning technique further accelerates the 3D search. On benchmarks with temporal NFZs, DTAPP-IICR achieves near-100% success with fleets of up to 1,000 UAVs and gains up to 50% runtime reduction from pruning, outperforming batch Enhanced Conflict-Based Search in the UTM context. Scaling successfully in realistic city-scale operations where other priority-based methods fail even at moderate deployments, DTAPP-IICR is positioned as a practical and scalable solution for preflight planning in dense, dynamic urban airspace.


<p align="center">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/gifRoutePlanning.gif?raw=true" height="350">
</p>



## Repository for the Proposed Algorithm

https://github.com/amathsow/4DPlanning



## Structure of this repository for the UNetyEmu simulator

```
├── Assets  
│   ├── DepthCameraImages  
│   ├── MissionsLogs  
│   ├── Models  
│   ├── Resources  
│   ├── Scenes  
│   ├── Scripts
│   │   ├── Algorithms  
│   │   ├── CameraSetting  
│   │   ├── Controllers  
│   │   ├── GeneralManagementScripts  
│   │   ├── GeneralSettings  
│   │   ├── GetFeatures  
│   │   ├── PlayersDynamics  
│   │   └── Sensors  
│   └── TextMesh Pro  
├── ImagesDoc  
├── Packages  
├── ProjectSettings  
├── Assembly-CSharp-Editor.csproj  
├── Assembly-CSharp.csproj  
├── LICENSE  
└── README  
```


## Wiki documentation

- [a. Repository structure](https://github.com/intrig-unicamp/UNetyEmu/wiki/a.-Repository-structure)
- [b. Basic information](https://github.com/intrig-unicamp/UNetyEmu/wiki/b.-Basic-information)
- [c. Dependencies](https://github.com/intrig-unicamp/UNetyEmu/wiki/c.-Dependencies)
- [d. Full Installation](https://github.com/intrig-unicamp/UNetyEmu/wiki/d.-Installation)
    - [Getting started with Unity](https://github.com/intrig-unicamp/UNetyEmu/wiki/d.-Installation#getting-started-with-unity)
    - [UNetyEmu Basic Setup using Unity](https://github.com/intrig-unicamp/UNetyEmu/wiki/d.-Installation#unetyemu-basic-setup-using-unity)
    - [Getting started with Mininet-WiFi](https://github.com/intrig-unicamp/UNetyEmu/wiki/d.-Installation#getting-started-with-mininet-wifi)
    - [UNetyEmu Basic Setup using Mininet-WiFi](https://github.com/intrig-unicamp/UNetyEmu/wiki/d.-Installation#unetyemu-basic-setup-using-mininet-wifi)
- [e. Minimum test](https://github.com/intrig-unicamp/UNetyEmu/wiki/e.-Minimum-test)
    - [First Scenario Demo (only in Unity)](https://github.com/intrig-unicamp/UNetyEmu/wiki/e.-Minimum-test#first-scenario-demo-only-in-unity)
- [f. Experiments](https://github.com/intrig-unicamp/UNetyEmu/wiki/f.-Experiments)
    - [First Scenario Demo (Continued from section g. Minimum Tests)](https://github.com/intrig-unicamp/UNetyEmu/wiki/f.-Experiments#first-scenario-demo-continued-from-page-g-minimum-tests)
    - [Second Scenario Demo (also only in Unity)](https://github.com/intrig-unicamp/UNetyEmu/wiki/f.-Experiments#second-scenario-demo-also-only-in-unity)
    - [Third Scenario Demo (Unity + Mininet-WiFi)](https://github.com/intrig-unicamp/UNetyEmu/wiki/f.-Experiments#third-scenario-demo-unity--mininet-wifi)
- [Videos and Tutorials](https://github.com/intrig-unicamp/UNetyEmu/wiki/Videos-and-Tutorials)



## Citation

Sow, A.; Rodriguez, M.; de Oliveira, F.; Wzorek, M.; de Leng, D.; Tiger, M.; Heintz, F.; Rothenberg, C. (2026). Multi UAVs Preflight Planning in a Shared and Dynamic Airspace. In Proceedings of the 25th International Conference on Autonomous Agents and Multiagent Systems (AAMAS 2026). Accepted for publication. Pre-print version: [https://doi.org/10.48550/arXiv.2602.12055](https://doi.org/10.48550/arXiv.2602.12055)

```bibtex
@inproceedings{arxiv_aamas26,
    author        = {Sow, Amath and Rodriguez, Mauricio and de Oliveira, Fabiola and Wzorek, Mariusz and de Leng, Daniel and Tiger, Mattias and Heintz, Fredrik and Rothenberg, Christian},
    title         = {Multi UAVs Preflight Planning in a Shared and Dynamic Airspace},
    booktitle     = {Proceedings of the 25th International Conference on Autonomous Agents and Multiagent Systems (AAMAS 2026)},
    year          = {2026},
    note          = {Accepted for publication},
    eprint        = {2602.12055},
    archivePrefix = {arXiv},
    primaryClass  = {cs.MA},
    repo = {Algorithm source code: https://github.com/amathsow/4DPlanning. The repository for the simulator used in this work is available at the following link in the branch corresponding to the event: https://github.com/intrig-unicamp/UNetyEmu}
}
```


## License

Apache License
Version 2.0, January 2004
[http://www.apache.org/licenses/](http://www.apache.org/licenses/)
