FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["SecurePass.csproj", "./"]
RUN dotnet restore "SecurePass.csproj"
COPY . .
RUN dotnet publish "SecurePass.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY SecurePass.db /app/SecurePass.db
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SecurePass.dll"]
