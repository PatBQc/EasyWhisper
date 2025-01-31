del .\Release\*.* /S /Q
del .\EasyWhisper\bin\Release\*.* /S /Q


REM .Net Version
dotnet publish .\EasyWhisper.sln -c Release
powershell -command "Compress-Archive -Path .\EasyWhisper\bin\Release\net8.0-windows\publish -DestinationPath .\Release\EasyWhisper-v01.01-DotNet.zip"
del .\EasyWhisper\bin\Release\*.* /S /Q

REM Windows x64
dotnet publish .\EasyWhisper.sln -c Release -r win-x64 --self-contained
powershell -command "Compress-Archive -Path .\EasyWhisper\bin\Release\net8.0-windows\win-x64\publish\ -DestinationPath .\Release\EasyWhisper-v01.01-x64-Self-Contained.zip"
del .\EasyWhisper\bin\Release\*.* /S /Q

REM Windows x86
dotnet publish .\EasyWhisper.sln -c Release -r win-x86 --self-contained
powershell -command "Compress-Archive -Path .\EasyWhisper\bin\Release\net8.0-windows\win-x86\publish\ -DestinationPath .\Release\EasyWhisper-v01.01-x86-Self-Contained.zip"
del .\EasyWhisper\bin\Release\*.* /S /Q

REM Windows ARM 64 
dotnet publish .\EasyWhisper.sln -c Release -r win-arm64 --self-contained
powershell -command "Compress-Archive -Path .\EasyWhisper\bin\Release\net8.0-windows\win-arm64\publish\ -DestinationPath .\Release\EasyWhisper-v01.01-ARM64-Self-Contained.zip"
del .\EasyWhisper\bin\Release\*.* /S /Q
