# F1_Racer
-----------**Description:**-----------

Unity bachelor project of 'formula 1 racing' with ml-agents (reinforcement learning) for comparing open-ai's trainers ppo and sac

----------**Start Project:**-----------

This project is made for the unity editor. To open it, clone it to local and then add the project path in the unity hub.
To start the actual game, head to Assets/Game/Scenes/GameScene and press play in the unity editor.

-----------**Play Modes:**------------

The project supports 3 playstyles within the game.
 - First ("Play Game") is playing yourself with MnK.
 - The Second option ("Watch Algortihm") uses some programmed algorithm.
 - With the third option ("Load AI ...") you can select a Neural-Network (.onnx file) to let an ai play the game.

-----------**Training AI:**------------

Training will start automatically when "mlagents-learn" is called (in the activated console-enviroment) and the "GameScene"-Scene is running.
Since building the project makes training faster, i recommend building the scene. But still you can also train in the Unity-Editor.
If ml-agents is correctly installed and setup, an example training command in the console could be: "mlagents-learn C:\<your path>\YourConfigFile.yaml --env=C:\<your path>\Builds\F1_Racer.exe --no-graphics"
<br/>Within the result you will find the trained ai as .onnx file
<br/>To get started with ml-agents you can look this start-tutorial on ![youtube](https://www.youtube.com/watch?v=RANRz9oyzko)

-----------**Code Insight:**-----------

For understanding the structure of the code around the driving you can get a rough insight by looking at following simplefied UML-diagram ![Assets/Game/Docs](https://github.com/JoshuaBluem/F1_Racer/tree/main/Assets/Game/Docs/CarDrive_UML.drawio.pdf)
