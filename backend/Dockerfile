# ---------- build stage: has the full .NET SDK ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (only project files copied) so Docker can cache NuGet packages
# and skip this slow step when only source code changes.
COPY global.json SchoolAdmission.sln ./
COPY src/SchoolAdmission.Api/SchoolAdmission.Api.csproj src/SchoolAdmission.Api/
COPY tests/SchoolAdmission.Api.Tests/SchoolAdmission.Api.Tests.csproj tests/SchoolAdmission.Api.Tests/
RUN dotnet restore SchoolAdmission.sln

COPY . .
RUN dotnet publish src/SchoolAdmission.Api/SchoolAdmission.Api.csproj -c Release -o /app/publish --no-restore

# ---------- test stage: used by `docker compose --profile test run --rm backend-tests` ----------
FROM build AS test
ENTRYPOINT ["dotnet", "test", "SchoolAdmission.sln", "--no-restore", "--logger", "console;verbosity=normal"]

# ---------- runtime stage: small image, no SDK ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Run as the non-root user that ships with the .NET images.
USER app
ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
