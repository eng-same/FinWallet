FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

# Copy csproj files and restore dependencies
COPY FinWallet.slnx ./
COPY FinWallet.Api/FinWallet.Api.csproj FinWallet.Api/
COPY FinWallet.Application/FinWallet.Application.csproj FinWallet.Application/
COPY FinWallet.Domain/FinWallet.Domain.csproj FinWallet.Domain/
COPY FinWallet.Infrastructure/FinWallet.Infrastructure.csproj FinWallet.Infrastructure/
COPY FinWallet.Contracts/FinWallet.Contracts.csproj FinWallet.Contracts/
COPY FinWallet.MockBankCore.Worker/FinWallet.MockBankCore.Worker.csproj FinWallet.MockBankCore.Worker/
COPY FinWallet.Tests/FinWallet.Tests.csproj FinWallet.Tests/
COPY docker-compose.dcproj ./

RUN dotnet restore FinWallet.slnx

# Copy everything else and build release
COPY . ./
RUN dotnet publish FinWallet.Api/FinWallet.Api.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/out ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "FinWallet.Api.dll"]
