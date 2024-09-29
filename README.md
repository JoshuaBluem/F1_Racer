# F1_Racer
-------------**Description:**--------------

This Unity bachelor project focuses on Formula 1 Racing, utilizing ML-Agents for reinforcement learning to compare OpenAI's trainers: Proximal Policy Optimization (PPO) and Soft Actor-Critic (SAC).

---------**Starting the Project:**----------

To access the project, clone it to your local machine and add the project path in Unity Hub. To initiate the game, navigate to Assets/Game/Scenes/GameScene.unity and press play in the Unity Editor.

----------**Gameplay Modes:**-----------

The project supports 3 gameplay modes:
 - Play Game: Engage in gameplay using mouse and keyboard controls.
 - Watch Algorithm: Observe a programmed algorithm in action.
 - Load AI: Select a Neural Network (.onnx file) to allow AI to play the game.

--------------**Training AI:**--------------

AI training begins automatically when the mlagents-learn command is executed in the activated console environment while the "GameScene" is running. Building the project is recommended for faster training, though training can also be conducted within the Unity Editor. If ML-Agents is correctly installed and configured, an example command in the console would be:
<br/>mlagents-learn C:<your_path>\YourConfigFile.yaml --env=C:<your_path>\Builds\F1_Racer.exe --no-graphics
<br/>The trained AI will be saved as an .onnx file. For additional guidance on ML-Agents, refer to introductory tutorials available on [YouTube](https://www.youtube.com/watch?v=RANRz9oyzko)

-------------**Code Insight:**-------------

To gain a better understanding of the code structure related to driving mechanics, please consult the simplified UML diagram located in [Assets/Game/Docs](https://github.com/JoshuaBluem/F1_Racer/tree/main/Assets/Game/Docs/CarDrive_UML.drawio.pdf)
