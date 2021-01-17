FROM mcr.microsoft.com/dotnet/runtime:5.0
COPY src/Sestek.ManagerAutomation/bin/Release/net5.0/publish/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "Sestek.ManagerAutomation.dll"]