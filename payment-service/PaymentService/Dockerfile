FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src


COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8082
EXPOSE 8082

ENTRYPOINT ["dotnet", "PaymentService.dll"]