# Build image
FROM microsoft/dotnet:2.1.301-sdk AS builder

ARG BUILD_NUMBER
ENV BUILD_NUMBER ${BUILD_NUMBER:-0.0.0}

ARG NUGET_API_KEY
ENV NUGET_API_KEY $NUGET_API_KEY


RUN echo $BUILD_NUMBER

WORKDIR /sln

COPY ./build.sh ./build.cake   ./

# Install Cake, and compile the Cake build script
RUN ./build.sh --Target="Clean" --buildVersion="$BUILD_NUMBER"


COPY ./Hammock.sln ./  
COPY ./src/Hammock/Hammock.csproj  ./src/Hammock/Hammock.csproj  
COPY ./tests/Hammock.Tests/Hammock.Tests.csproj  ./tests/Hammock.Tests/Hammock.Tests.csproj 

RUN ./build.sh --Target="Restore" --buildVersion="$BUILD_NUMBER"

COPY ./tests ./tests
COPY ./src ./src


# Build, Test, and Publish
RUN /bin/bash ./build.sh --Target="CI" --buildVersion="$BUILD_NUMBER"

