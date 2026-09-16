rem Developer Command Prompt for VS 2022 
msbuild /t:Clean,Restore,Build /p:VsTargetVersion=VS2022 /p:Configuration=Release_VS2022 /p:Platform="x64"
msbuild /t:Clean,Restore,Build /p:VsTargetVersion=VS2019 /p:Configuration=Release_VS2019 /p:Platform="x86" 
msbuild /t:Clean,Restore,Build /p:VsTargetVersion=VS2017 /p:Configuration=Release_VS2017 /p:Platform="x86" 
 