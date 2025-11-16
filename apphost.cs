#:sdk Aspire.AppHost.Sdk@13.0.0

#:project src/Aspire.ElevatorMusic/Aspire.ElevatorMusic.csproj

using Aspire.ElevatorMusic;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddElevatorMusic();

builder.Build().Run();