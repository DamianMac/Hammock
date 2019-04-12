# Build image
FROM microsoft/dotnet:2.1.301-sdk AS builder

ARG buildVersion=0.0.0.0

WORKDIR /sln



COPY ./build.sh ./build.cake   ./

# Install Cake, and compile the Cake build script
RUN ./build.sh --Target="Clean"

# Copy all the csproj files and restore to cache the layer for faster builds
# The dotnet_build.sh script does this anyway, so superfluous, but docker can 
# cache the intermediate images so _much_ faster
COPY ./Hammock.sln ./  
COPY ./src/Hammock/Hammock.csproj  ./src/Hammock/Hammock.csproj  
COPY ./tests/Hammock.Tests/Hammock.Tests.csproj  ./tests/Hammock.Tests/Hammock.Tests.csproj 

RUN ./build.sh --Target="Restore"

COPY ./tests ./tests
COPY ./src ./src


# Build, Test, and Publish
RUN /bin/bash ./build.sh --Target="CI" --buildVersion="$buildVersion" --octopusurl="$octopusurl" --octopusapikey="$octopusapikey"

