FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["HomeAssignmentPFTC.csproj", "./"]
RUN dotnet restore "./HomeAssignmentPFTC.csproj"

COPY . .
RUN dotnet publish "HomeAssignmentPFTC.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HomeAssignmentPFTC.dll"]