REM @echo off
echo Starting batch file execution...

REM Change directory to the specified path
cd /d C:\Unity\Uni\F1_Racer

REM Activate the virtual environment using 'call' to keep the command prompt open
call .\venv\Scripts\activate

REM Loop through each configuration file from 1 to 100 and run mlagents-learn
for /l %%i in (1, 1, 10) do (
    echo Executing mlagents-learn with Config%%i.yaml
    REM mlagents-learn C:\Unity\Uni\F1_Racer\Assets\TrainingFiles\ConfigurationFiles\Data\SAC_Config%%i.yaml --env=C:\Unity\Uni\F1_Racer\Builds\F1_Racer.exe --no-graphics
	mlagents-learn C:\Unity\Uni\F1_Racer\Assets\TrainingFiles\ConfigurationFiles\Data\configuration85.hp.learning%%i.yaml --env=C:\Unity\Uni\F1_Racer\Builds\F1_Racer.exe --no-graphics
)

REM @echo on
pause
