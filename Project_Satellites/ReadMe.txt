// Build
A build of the simulator can be found in the Build folder. Run the Project Satellites.exe in the folder.
New satellites can be placed by right-clicking in space. Operations can be triggered on satellites by right-clicking on them. Time can be scaled in the upper left corner.

// Algorithms + Simulator
The Unity project can be found in the Project_Satellites folder. The code for the simulator as well as the algorithms can be found in the Assets/Scripts folder inside that folder.
Simulator code can be found in the Assets/Scripts folder while algorithms code can be found in Assets/Scripts/Algorithm.

// UPPAAL
In all models, under System Declarations, switch between synchronous and asynchronous
versions of the RingNode Templates, as well as the different Setup Templates.
NOTE: Some queries are dependent on the declared RingNode and/or Setup Templates.

The number of nodes, or any other relevant parameters can be changed in the Global Declaration.

The main model file are: Discovery, FailureDetection, and PlanGeneration.
NOTE: Some model include other RingNode templates than RingNodeSync and RingNodeAsync.
These are merely for demostrating past work and may not be up to date with queries.