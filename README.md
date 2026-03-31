# An Integrated Framework for Network Emulation and Multi-vehicle Algorithm Testing

[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)]()
[![License](https://img.shields.io/badge/License-Apache%202.0-green.svg)]()
[![Engine](https://img.shields.io/badge/Engine-Unity-black.svg)]()
[![Network](https://img.shields.io/badge/Network-Mininet--WiFi-orange.svg)]()
[![Languages](https://img.shields.io/badge/Languages-C%23%20%7C%20Python-purple.svg)]()

As drones and autonomous vehicles become integral to smart city infrastructures, there is a growing need for tools that can accurately evaluate their behavior under realistic communication and mobility conditions. Existing frameworks often lack support for scalable scenarios, realistic wireless emulation, or the integration of heterogeneous vehicle types. This demonstration presents UNetyEmu as a novel framework that combines real-time network emulation with high-fidelity mobility simulation, enabling realistic experimentation with both aerial and non-aerial autonomous vehicles. This integration allows researchers to evaluate vehicle coordination under dynamic communication conditions typical of smart city scenarios

<p align="center">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/gifVehiclesScenario.gif?raw=true" height="300">
</p>
<p align="center">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/gif4.gif?raw=true" height="400">
</p>



## Repository structure

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
│   │   ├── Network
│   │   │   └── mininet  
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

Rodriguez, M.; Góes de Castro, A.; Fontes, R.; Rodriguez, F.; Rothenberg, C. (2025). An Integrated Framework for Network Emulation and Multi-vehicle Algorithm Testing. In Proceedings of the ACM SIGCOMM 2025 Posters and Demos (SIGCOMM ’25). DOI: [https://doi.org/10.1145/3744969.3748436](https://doi.org/10.1145/3744969.3748436)

```bibtex
@inproceedings{sigcomm25,
    author = {Rodriguez, Mauricio and de Castro, Ariel Goes and Fontes, Ramon and Rodriguez, Fabricio and Rothenberg, Christian},
    title = {An Integrated Framework for Network Emulation and Multi-vehicle Algorithm Testing},
    year = {2025},
    isbn = {9798400720260},
    publisher = {Association for Computing Machinery},
    address = {New York, NY, USA},
    url = {https://doi.org/10.1145/3744969.3748436},
    doi = {10.1145/3744969.3748436},
    booktitle = {Proceedings of the ACM SIGCOMM 2025 Posters and Demos},
    pages = {167–169},
    numpages = {3},
    keywords = {Algorithm Testing, Autonomous Vehicles, Drones, Network Emulation, Realistic scenarios, UAV, UNetyEmu},
    location = {Coimbra, Portugal},
    series = {ACM SIGCOMM Posters and Demos '25},
    repo = {The repository for the simulator used in this work is available at the following link in the branch corresponding to the event: https://github.com/intrig-unicamp/UNetyEmu}
}
```


## License

Apache License
Version 2.0, January 2004
[http://www.apache.org/licenses/](http://www.apache.org/licenses/)
