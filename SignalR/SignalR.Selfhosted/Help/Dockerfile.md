#BUILD STAGE START
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

#SET WORKING DIR
WORKDIR /app

#COPY
COPY ["SignalR.Common/SignalR.Common.csproj", "SignalR.Common/"]
COPY ["SignalR.Selfhosted/SignalR.Selfhosted.csproj", "SignalR.Selfhosted/"]

#RESTORE
RUN dotnet restore "SignalR.Selfhosted/SignalR.Selfhosted.csproj"

WORKDIR /app/SignalR.Selfhosted

COPY . .

RUN dotnet publish "SignalR.Selfhosted/SignalR.Selfhosted.csproj" -c Release -o /app/publish/
#BUILD STAGE ENDED

#RUNTIME STAGE START
FROM mcr.microsoft.com/dotnet/aspnet:6.0 as runtime

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Development

RUN apt-get update &&\
   apt-get install -y curl

EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SignalR.Selfhosted.dll"]
#BUILD STAGE ENDED