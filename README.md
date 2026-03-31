# UNetyEmu: Unity-based simulator for aerial and non-aerial vehicles with integrated network emulation

[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)]()
[![Engine](https://img.shields.io/badge/Engine-Unity-black.svg)]()
[![Network](https://img.shields.io/badge/Network-Mininet--WiFi-orange.svg)]()
[![Languages](https://img.shields.io/badge/Languages-C%23%20%7C%20Python-purple.svg)]()

<img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/Selos_SBRC25_new.png" height="120">

**UNetyEmu** is a novel framework that combines real-time network emulation with high-fidelity mobility simulation, enabling realistic experimentation with both aerial and non-aerial autonomous vehicles. This integration allows researchers to evaluate vehicle coordination under dynamic communication conditions typical of smart city scenarios using 5G (and beyond) networks. Unlike existing experimental platforms, UNetyEmu provides online and offline control connectivity states, supports network emulation, and allows evaluating algorithms related to multiple drones, such as obstacle avoidance, path planning, logistics, and coordination, among others.

<p align="center">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/gif2.gif?raw=true" height="200">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/gif3.gif?raw=true" height="200">
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/intrig-unicamp/UNetyEmu/refs/heads/main/ImagesDoc/gif5.gif?raw=true" height="400">
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

Rodriguez Cesen, M.; Góes de Castro, A.; Santana, I.; Fontes, R. R.; R. Cesen, F.; Esteve Rothenberg, C. (2025). UNetyEmu: Unity-based simulator for aerial and non-aerial vehicles with integrated network emulation.In 43rd Brazilian Symposium on Computer Networks and Distributed Systems (SBRC 2025), Natal/RN, Brazil. DOI: [https://doi.org/10.5753/sbrc_estendido.2025.7122](https://doi.org/10.5753/sbrc_estendido.2025.7122)

```bibtex
@inproceedings{sbrc25,
    author = {Rodriguez, Mauricio and de Castro, Ariel and Santana, Ibini and Fontes, Ramon and Rodriguez, Fabricio and Rothenberg, Christian},
    title = {UNetyEmu: Unity-based simulator for aerial and non-aerial vehicles with integrated network emulation},
    booktitle = {Companion Proceedings of the 43rd Brazilian Symposium on Computer Networks and Distributed Systems},
    location = {Natal/RN},
    year = {2025},
    keywords = {},
    issn = {2177-9384},
    pages = {100--111},
    publisher = {SBC},
    address = {Porto Alegre, RS, Brasil},
    doi = {10.5753/sbrc_estendido.2025.7122},
    url = {https://sol.sbc.org.br/index.php/sbrc_estendido/article/view/35864},
    repo = {The repository for the simulator used in this work is available at the following link in the branch corresponding to the event: https://github.com/intrig-unicamp/UNetyEmu}
}
```


## License

Apache License
Version 2.0, January 2004
[http://www.apache.org/licenses/](http://www.apache.org/licenses/)
