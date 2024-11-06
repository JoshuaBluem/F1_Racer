REM This batch script loops through multiple config files and trains all after another
REM So you do not have to start training manually




REM @echo off
echo Starting batch file execution...

REM Change directory to the specified path
cd /d C:\Unity\Uni\F1_Racer

REM Activate the virtual environment using 'call' to keep the command prompt open
call .\venv\Scripts\activate

REM Loop through each configuration file from 1 to 100 and run mlagents-learn
for /l %%i in (1, 1, 100) do (
    REM echo Executing mlagents-learn with PPO_Config%%i.yaml
  	REM mlagents-learn C:\Unity\Uni\F1_Racer\Assets\TrainingFiles\Configs\Data\ppo_best%%i.yaml --env=C:\Unity\Uni\F1_Racer\Builds\F1_Racer.exe --no-graphics
 	echo Executing mlagents-learn with SAC_Config%%i.yaml
 	mlagents-learn C:\Unity\Uni\F1_Racer\results\SAC_Config%%i\configuration.yaml --env=C:\Unity\Uni\F1_Racer\Builds\F1_Racer.exe --no-graphics
)

REM @echo on
pause
