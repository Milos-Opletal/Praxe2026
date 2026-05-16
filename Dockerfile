# Stage 1: Build the C# Application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /source

# Copy the APP folder containing the C# project
COPY APP/ ./APP/

# Publish the application as a single executable for win-x64
WORKDIR /source/APP/Praxe2026
RUN dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o /app/publish

# Stage 2: Setup the Python Server
FROM python:3.11-slim
WORKDIR /app

# Copy python requirements and install them
COPY SERVER/requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

# Copy the server source code
COPY SERVER/ ./

# Create build directory for the executable
RUN mkdir -p /app/build
# Copy the compiled executable from Stage 1 into the build directory
COPY --from=build-env /app/publish/Praxe2026.exe /app/build/Praxe2026.exe

# Expose port 4331 for Flask
EXPOSE 4331

# Start the Flask server with gunicorn for better production performance
CMD ["gunicorn", "--workers", "1", "--threads", "1", "--timeout", "60", "-b", "0.0.0.0:4331", "server:app"]
