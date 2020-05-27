@echo off
setlocal enabledelayedexpansion

:INIT
	set "rc=-15"
	set "solutionDir=%~1*"
	set "targetDir=%~2*"
	set "configurationName=%~3"
	set "projectDir=%~4*"
	
	set "solutionDir=%solutionDir:\*=%"
	set "targetDir=%targetDir:\*=%"
	set "projectDir=%projectDir:\*=%"

:PROCESS
	echo.
	echo.
	echo ===== POST-BUILD =======================================================================================================
		
	if exist "%solutionDir%\Output" rd "%solutionDir%\Output" /s /q >nul
	if exist "%solutionDir%\Output" goto :END_ERROR
	md "%solutionDir%\Output" >nul	
	
	echo robocopy "%targetDir%" "%solutionDir%\Output" *.exe *.dll *.json /XF *vshost* 
	robocopy "%targetDir%"      "%solutionDir%\Output" *.exe *.dll *.json /XF *vshost* >nul
	if %errorlevel% GEQ 8 goto :END_ERROR
	
	if /i not "%configurationName%"=="RELEASE" goto :END_OK
	
	:: Obfuscation	
	call :Obfuscate
	
	echo robocopy "%solutionDir%\Output" "%solutionDir%\Output\Obfuscated" *.json
	robocopy "%solutionDir%\Output" "%solutionDir%\Output\Obfuscated" *.json >nul
	if %errorlevel% GEQ 8 goto :END_ERROR

:END_OK
	set "rc=0"
	goto :END
	
:END_ERROR
	set "rc=999"
	goto :END
	
:END
	echo ========================================================================================================================
	echo.
	echo.
	exit /b %rc%
	

:: ------------------------------------------------------------------------------------------------------------------------------
:Obfuscate
	if /i not "%configurationName%"=="Release" goto :eof
	
	set "filesToSkip="
	
	set "obfuscateScript=d:\HWI and VSS\DNBE\ATM Installer\Code\Obfuscator\Skater\obfuscate.cmd"
	
	if not exist "%obfuscateScript%" echo File %obfuscateScript% not found& goto :END_ERROR
	
	echo calling "%obfuscateScript%" "%solutionDir%\Output" "%filesToSkip%"
	call "%obfuscateScript%" "%solutionDir%\Output" "%filesToSkip%"
	if not %errorlevel%==0 goto :END_ERROR
		
	goto :eof