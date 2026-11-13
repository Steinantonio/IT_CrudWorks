# syntax=docker/dockerfile:1

# Render builds with the repository root as the build context, so all paths
# below are relative to that root.

# Stage 1: build the React SPA.
FROM node:24-alpine AS frontend
WORKDIR /src/frontend
COPY project/frontend/package.json project/frontend/package-lock.json ./
RUN npm ci
COPY project/frontend/ ./
RUN npm run build

# Stage 2: publish the ASP.NET Core API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY . .
RUN dotnet publish project/src/CrudWorks.Api/CrudWorks.Api.csproj \
        -c Release \
        -o /app/publish \
        /p:UseAppHost=false

# Stage 3: runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=backend /app/publish ./
COPY --from=frontend /src/frontend/dist ./wwwroot
# Render's default expected port is 10000; binding to 0.0.0.0 is implicit via this var.
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "CrudWorks.Api.dll"]
