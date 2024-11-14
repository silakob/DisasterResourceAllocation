FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DisasterResourceAllocation/DisasterResourceAllocation.csproj", "DisasterResourceAllocation/"]
RUN dotnet restore "DisasterResourceAllocation/DisasterResourceAllocation.csproj"
COPY . .
WORKDIR "/src/DisasterResourceAllocation"
RUN dotnet build "DisasterResourceAllocation.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DisasterResourceAllocation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DisasterResourceAllocation.dll"]


#docker build . -f DisasterResourceAllocation/Dockerfile -t disasterresourceallocation
#docker run --name disasterresourceallocation -p 4000:8080 -d disasterresourceallocation
#docker run --name redis -p 5002:6379 -d redis